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

        // GET: /AdminHealth/HealthTracker?date=2025-12-16
        public async Task<IActionResult> HealthTracker(string? date)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            var selectedDate = string.IsNullOrWhiteSpace(date)
                ? DateTime.Today.ToString("yyyy-MM-dd")
                : date;

            // Load animals from DB
            var animals = await _context.Animals
                .OrderBy(a => a.TagNumber)
                .Select(a => new { a.Id, a.TagNumber, a.Breed, a.Weight })
                .ToListAsync();

            // Load existing logs for date from JSON
            var contentRoot = _env.ContentRootPath;
            HealthLogHelper.EnsureInitialized(contentRoot);
            var logs = HealthLogHelper.GetByDate(contentRoot, selectedDate)
                .ToDictionary(x => x.AnimalId);

            var rows = new List<HealthTrackerRowVM>();
            foreach (var a in animals)
            {
                logs.TryGetValue(a.Id, out var log);

                rows.Add(new HealthTrackerRowVM
                {
                    AnimalId = a.Id,
                    TagNumber = a.TagNumber ?? "",
                    Breed = a.Breed,
                    WeightKg = (decimal)a.Weight,

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
        public IActionResult SaveHealthTracker(HealthTrackerPageVM vm)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            var contentRoot = _env.ContentRootPath;
            HealthLogHelper.EnsureInitialized(contentRoot);

            var entries = vm.Rows.Select(r => new HealthLogEntry
            {
                Date = vm.Date,
                AnimalId = r.AnimalId,
                TagNumber = r.TagNumber,

                MilkLiters = r.MilkLiters,

                HealthChecked = r.HealthChecked,

                MedicineGiven = r.MedicineGiven,
                MedicineName = r.MedicineGiven ? (r.MedicineName ?? "None") : "None",
                Dose = r.MedicineGiven ? (r.Dose ?? "") : "",

                Notes = r.Notes ?? ""
            }).ToList();

            HealthLogHelper.UpsertMany(contentRoot, entries);

            TempData["success"] = "Health Tracker saved successfully.";
            return RedirectToAction(nameof(HealthTracker), new { date = vm.Date });
        }

        // GET: /AdminHealth/HealthDashboard?year=2025&month=12
        public async Task<IActionResult> HealthDashboard(int? year, int? month)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            var now = DateTime.Today;
            var y = year ?? now.Year;
            var m = month ?? now.Month;

            var contentRoot = _env.ContentRootPath;
            HealthLogHelper.EnsureInitialized(contentRoot);

            var monthLogs = HealthLogHelper.GetByMonth(contentRoot, y, m);

            // Milk totals
            var totalMilk = monthLogs.Sum(x => x.MilkLiters);
            var animalsWithMilk = monthLogs.Where(x => x.MilkLiters > 0)
                                           .Select(x => x.AnimalId)
                                           .Distinct()
                                           .Count();

            // Milk by animal (TagNumber-based)
            var milkByAnimal = monthLogs
                .GroupBy(x => x.TagNumber)
                .Select(g => (TagNumber: g.Key, TotalMilk: g.Sum(x => x.MilkLiters)))
                .OrderByDescending(x => x.TotalMilk)
                .ToList();

            // Medicine entries for month
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

            // Optional: show total animals from DB, if you want later
            // var totalAnimals = await _context.Animals.CountAsync();

            return View(vm);
        }
    }
}
