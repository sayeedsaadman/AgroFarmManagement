using AgroManagement.Data;
using AgroManagement.Helper;
using AgroManagement.Models;
using AgroManagement.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroManagement.Controllers
{
    public class AdminAnimalLifecycleController : Controller
    {
        private readonly AgroContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminAnimalLifecycleController(AgroContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private bool IsAdmin()
            => HttpContext.Session.GetString("UserType") == "Admin";

        // GET: /AdminAnimalLifecycle/Index
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            var contentRoot = _env.ContentRootPath;
            AnimalLifecycleHelper.EnsureInitialized(contentRoot);

            var statusMap = AnimalLifecycleHelper.GetAll(contentRoot)
                .ToDictionary(x => x.AnimalId, x => x);

            var animals = await _context.Animals
                .OrderBy(a => a.TagNumber)
                .Select(a => new { a.Id, a.TagNumber, a.Breed, a.Weight })
                .ToListAsync();

            var vm = new AnimalLifecyclePageVM
            {
                StatusOptions = new List<string> { "Active", "Pregnant", "On Sell", "Sold", "Dead" }
            };

            foreach (var a in animals)
            {
                statusMap.TryGetValue(a.Id, out var st);

                vm.Rows.Add(new AnimalLifecycleRowVM
                {
                    AnimalId = a.Id,
                    TagNumber = a.TagNumber ?? "",
                    Breed = a.Breed ?? "",
                    Weight = a.Weight,

                    Status = st?.Status ?? "Active",
                    Notes = st?.Notes ?? "",
                    UpdatedOn = st?.UpdatedOn ?? ""
                });
            }

            return View(vm);
        }

        // POST: /AdminAnimalLifecycle/Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save(AnimalLifecyclePageVM vm)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            var contentRoot = _env.ContentRootPath;
            AnimalLifecycleHelper.EnsureInitialized(contentRoot);

            var entries = vm.Rows.Select(r => new AnimalLifecycleStatusEntry
            {
                AnimalId = r.AnimalId,
                TagNumber = r.TagNumber ?? "",
                Status = r.Status ?? "Active",
                Notes = r.Notes ?? "",
                UpdatedOn = DateTime.Today.ToString("yyyy-MM-dd")
            }).ToList();

            AnimalLifecycleHelper.UpsertMany(contentRoot, entries);

            TempData["success"] = "Animal lifecycle statuses saved successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
