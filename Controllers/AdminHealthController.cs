using AgroManagement.Data;
using AgroManagement.Helper;
using AgroManagement.Models;
using AgroManagement.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroManagement.Controllers
{
    public class AdminHealthController : Controller
    {
        private readonly AgroContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminHealthController(AgroContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private bool IsAdmin()
            => HttpContext.Session.GetString("UserType") == "Admin";

        // GET: /AdminHealth/HealthTracker?date=yyyy-MM-dd
        public async Task<IActionResult> HealthTracker(string? date)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            var selectedDate = string.IsNullOrWhiteSpace(date)
                ? DateTime.Today.ToString("yyyy-MM-dd")
                : date;

            // Animals from DB (Weight field is in DB as "Weight")
            var animals = await _context.Animals
                .OrderBy(a => a.TagNumber)
                .Select(a => new { a.Id, a.TagNumber, a.Breed, a.Weight })
                .ToListAsync();

            // Logs from JSON for selected date
            var contentRoot = _env.ContentRootPath;
            HealthLogHelper.EnsureInitialized(contentRoot);

            var dayLogs = HealthLogHelper.GetByDate(contentRoot, selectedDate)
                .ToDictionary(x => x.AnimalId, x => x);

            var rows = new List<HealthTrackerRowVM>();

            foreach (var a in animals)
            {
                dayLogs.TryGetValue(a.Id, out var log);

                // If JSON has weight for this date, show it; else show DB weight
                var displayWeight = (log != null && log.WeightKg > 0)
                    ? log.WeightKg
                    : Convert.ToDecimal(a.Weight);

                rows.Add(new HealthTrackerRowVM
                {
                    AnimalId = a.Id,
                    TagNumber = a.TagNumber ?? "",
                    Breed = a.Breed,

                    WeightKg = displayWeight, // ✅ editable

                    MilkLiters = log?.MilkLiters ?? 0,
                    HealthChecked = log?.HealthChecked ?? false,

                    MedicineGiven = log?.MedicineGiven ?? false,
                    MedicineName = log?.MedicineName ?? "None",
                    Dose = log?.Dose ?? "",
                    Notes = log?.Notes ?? ""
                });
            }

            var vm = new HealthTrackerPageVM
            {
                Date = selectedDate,
                Medicines = MedicineCatalog.All,
                Rows = rows
            };

            return View(vm);
        }

        // POST: /AdminHealth/SaveHealthTracker
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveHealthTracker(HealthTrackerPageVM vm)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            var contentRoot = _env.ContentRootPath;
            HealthLogHelper.EnsureInitialized(contentRoot);

            // Save logs to JSON (including WeightKg history)
            var entries = vm.Rows.Select(r => new HealthLogEntry
            {
                Date = vm.Date,
                AnimalId = r.AnimalId,
                TagNumber = r.TagNumber,

                WeightKg = r.WeightKg, // ✅ NEW field

                MilkLiters = r.MilkLiters,
                HealthChecked = r.HealthChecked,

                MedicineGiven = r.MedicineGiven,
                MedicineName = r.MedicineGiven ? (r.MedicineName ?? "None") : "None",
                Dose = r.MedicineGiven ? (r.Dose ?? "") : "",

                Notes = r.Notes ?? ""
            }).ToList();

            HealthLogHelper.UpsertMany(contentRoot, entries);

            // Update DB weight (latest)
            var ids = vm.Rows.Select(x => x.AnimalId).ToList();

            var dbAnimals = await _context.Animals
                .Where(a => ids.Contains(a.Id))
                .ToListAsync();

            foreach (var a in dbAnimals)
            {
                var row = vm.Rows.FirstOrDefault(x => x.AnimalId == a.Id);
                if (row != null && row.WeightKg > 0)
                {
                    a.Weight = (double)row.WeightKg;  // DB field is Weight
                }
            }

            await _context.SaveChangesAsync();

            TempData["success"] = "Health Tracker saved successfully .";
            return RedirectToAction(nameof(HealthTracker), new { date = vm.Date });
        }

        // GET: /AdminHealth/HealthDashboard?year=YYYY&month=MM
        public async Task<IActionResult> HealthDashboard(int? year, int? month)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            var now = DateTime.Today;
            var y = year ?? now.Year;
            var m = month ?? now.Month;

            var contentRoot = _env.ContentRootPath;
            HealthLogHelper.EnsureInitialized(contentRoot);

            // Month logs (for milk + medicine)
            var monthLogs = HealthLogHelper.GetByMonth(contentRoot, y, m);

            // ----- Milk totals -----
            var totalMilk = monthLogs.Sum(x => x.MilkLiters);

            var animalsWithMilk = monthLogs
                .Where(x => x.MilkLiters > 0)
                .Select(x => x.AnimalId)
                .Distinct()
                .Count();

            var milkByAnimal = monthLogs
                .GroupBy(x => x.TagNumber)
                .Select(g => (TagNumber: g.Key, TotalMilk: g.Sum(x => x.MilkLiters)))
                .OrderByDescending(x => x.TotalMilk)
                .ToList();

            // ----- Medicine logs for month -----
            var medicineEntries = monthLogs
                .Where(x => x.MedicineGiven && !string.IsNullOrWhiteSpace(x.MedicineName) && x.MedicineName != "None")
                .OrderByDescending(x => x.Date)
                .ToList();

            var vm = new HealthDashboardVM
            {
                Year = y,
                Month = m,

                TotalMilkMonth = totalMilk,
                AnimalsWithMilkMonth = animalsWithMilk,
                MedicineLogsMonth = medicineEntries.Count,

                MilkByAnimal = milkByAnimal,
                MedicineEntries = medicineEntries
            };

            // ===========================
            // ✅ Weight Alerts (Last 7 Days)
            // ===========================
            var anchorDate = (y == DateTime.Today.Year && m == DateTime.Today.Month)
                ? DateTime.Today
                : new DateTime(y, m, DateTime.DaysInMonth(y, m));

            var targetDate = anchorDate.AddDays(-7);

            // Need all logs for weight history, not only month logs
            var allLogs = HealthLogHelper.GetAll(contentRoot);

            // Get animals list for tags
            var animalList = await _context.Animals
                .Select(a => new { a.Id, a.TagNumber })
                .ToListAsync();

            decimal GetClosestWeight(List<HealthLogEntry> logs, DateTime date)
            {
                // Prefer latest on/before date
                var before = logs
                    .Select(l => new { Log = l, Dt = DateTime.Parse(l.Date) })
                    .Where(x => x.Dt <= date && x.Log.WeightKg > 0)
                    .OrderByDescending(x => x.Dt)
                    .FirstOrDefault();

                if (before != null) return before.Log.WeightKg;

                // Otherwise earliest after date
                var after = logs
                    .Select(l => new { Log = l, Dt = DateTime.Parse(l.Date) })
                    .Where(x => x.Dt > date && x.Log.WeightKg > 0)
                    .OrderBy(x => x.Dt)
                    .FirstOrDefault();

                return after?.Log.WeightKg ?? 0;
            }

            decimal GetLatestWeightInRange(List<HealthLogEntry> logs, DateTime from, DateTime to)
            {
                var latest = logs
                    .Select(l => new { Log = l, Dt = DateTime.Parse(l.Date) })
                    .Where(x => x.Dt >= from && x.Dt <= to && x.Log.WeightKg > 0)
                    .OrderByDescending(x => x.Dt)
                    .FirstOrDefault();

                return latest?.Log.WeightKg ?? 0;
            }

            var weightAlerts = new List<WeightAlertRowVM>();

            foreach (var animal in animalList)
            {
                var logs = allLogs.Where(l => l.AnimalId == animal.Id).ToList();
                if (logs.Count == 0) continue;

                var w7 = GetClosestWeight(logs, targetDate);
                var latest = GetLatestWeightInRange(logs, targetDate, anchorDate);

                if (w7 <= 0 || latest <= 0) continue;

                var percentChange = ((latest - w7) / w7) * 100m;

                // Alert if drop >= 5%
                var isAlert = percentChange <= -5m;

                weightAlerts.Add(new WeightAlertRowVM
                {
                    AnimalId = animal.Id,
                    TagNumber = animal.TagNumber ?? "",
                    Weight7DaysAgo = decimal.Round(w7, 2),
                    LatestWeight = decimal.Round(latest, 2),
                    PercentChange = decimal.Round(percentChange, 2),
                    IsAlert = isAlert
                });
            }

            vm.WeightAlerts = weightAlerts
                .OrderBy(x => x.PercentChange) // biggest drops first
                .ToList();

            return View(vm);
        }
    }
}
