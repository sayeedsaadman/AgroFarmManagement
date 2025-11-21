using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using AgroManagement.Data;
using AgroManagement.Models;
using AgroManagement.UnitOfWork;

namespace AgroManagement.Controllers
{
    public class AnimalsController : Controller
    {
        private readonly AgroContext _context;
        private readonly IUnitOfWork _work;

        public AnimalsController(AgroContext context, IUnitOfWork work)
        {
            _context = context;
            _work = work;
        }

        // ======================================================
        //                 FIXED BREED SUPPORT
        // ======================================================
        private static readonly string[] AllowedBreeds =
        {
            "Angus",
            "Brahman",
            "Holstein Friesian (HF)",
            "Jersey",
            "Sahiwal",
            "Sindhi",
            "Crossbred"
        };

        private static readonly HashSet<string> AllowedBreedsSet =
            new HashSet<string>(AllowedBreeds, StringComparer.OrdinalIgnoreCase);

        private static bool IsValidBreed(string? value) =>
            !string.IsNullOrWhiteSpace(value) && AllowedBreedsSet.Contains(value.Trim());

        private static IEnumerable<SelectListItem> GetBreedSelectList(string? selected = null)
        {
            foreach (var b in AllowedBreeds)
                yield return new SelectListItem
                {
                    Text = b,
                    Value = b,
                    Selected = string.Equals(b, selected, StringComparison.OrdinalIgnoreCase)
                };
        }

        // ================================
        //          FULL PAGE VIEWS
        // ================================

        // GET: Animals
        public async Task<IActionResult> Index()
        {
            // View uses Tabulator remote ajax; returning list is fine.
            return View(await _context.Animals.ToListAsync());
        }

        // GET: Animals/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var animal = await _context.Animals.FirstOrDefaultAsync(m => m.Id == id);
            if (animal == null) return NotFound();

            return View(animal);
        }

        // GET: Animals/Create (full page)
        public IActionResult Create()
        {
            ViewBag.Breeds = GetBreedSelectList();
            return View();
        }

        // POST: Animals/Create (full page)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,TagNumber,Breed,DateOfBirth,Weight,PurchasePrice")] Animal animal)
        {
            if (!IsValidBreed(animal.Breed))
                ModelState.AddModelError(nameof(Animal.Breed), "Please select a valid breed.");

            if (_context.Animals.Any(a => a.TagNumber == animal.TagNumber))
                ModelState.AddModelError(nameof(Animal.TagNumber), "Tag Number already exists!");

            if (!ModelState.IsValid)
            {
                ViewBag.Breeds = GetBreedSelectList(animal.Breed);
                return View(animal);
            }

            _context.Add(animal);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Animals/Edit/5 (full page)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var animal = await _context.Animals.FindAsync(id);
            if (animal == null) return NotFound();

            ViewBag.Breeds = GetBreedSelectList(animal.Breed);
            return View(animal);
        }

        // POST: Animals/Edit/5 (full page)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,TagNumber,Breed,DateOfBirth,Weight,PurchasePrice")] Animal animal)
        {
            if (id != animal.Id) return NotFound();

            if (!IsValidBreed(animal.Breed))
                ModelState.AddModelError(nameof(Animal.Breed), "Please select a valid breed.");

            if (!ModelState.IsValid)
            {
                ViewBag.Breeds = GetBreedSelectList(animal.Breed);
                return View(animal);
            }

            try
            {
                _context.Update(animal);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Animals.Any(e => e.Id == id)) return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Animals/Delete/5 (full page)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var animal = await _context.Animals.FirstOrDefaultAsync(m => m.Id == id);
            if (animal == null) return NotFound();

            return View(animal);
        }

        // POST: Animals/Delete/5 (full page)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var animal = await _context.Animals.FindAsync(id);
            if (animal != null) _context.Animals.Remove(animal);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ============================================
        //          TABULATOR REMOTE DATA SOURCE
        // ============================================
        [HttpGet]
        public async Task<IActionResult> GetAnimalList(int page = 1, decimal size = 5, string searchquery = "")
        {
            try
            {
                var recordsTotal = 0;

                // Parse incoming search filter JSON from Tabulator
                var filterItems = new List<FilterItem>();
                var searchQueryModel = JsonConvert.DeserializeObject<SearchQueryModel>(searchquery);

                if (searchQueryModel != null && searchQueryModel.filter != null)
                {
                    filterItems = searchQueryModel.filter;
                    size = searchQueryModel.size;
                    page = searchQueryModel.page;
                }

                // Build dynamic WHERE (consumed by your repo/SP)
                string filterQuery = "";
                if (filterItems.Count > 0)
                {
                    foreach (var item in filterItems)
                    {
                        if (!string.IsNullOrEmpty(item.field) && !string.IsNullOrEmpty(item.value))
                        {
                            if (!string.IsNullOrEmpty(filterQuery)) filterQuery += " AND ";
                            filterQuery += $"{item.field} LIKE '%{item.value}%'";
                        }
                    }
                }

                var animalList = await _work.Animal.GetAllAnimalsAsync(filterQuery, page, (int)size);

                recordsTotal = animalList.Count > 0 ? animalList.First().TOTALCOUNT : 0;
                var pagecount = Math.Ceiling(recordsTotal / (double)size);

                return Ok(new
                {
                    success = true,
                    data = animalList,
                    last_page = pagecount,
                    current_page = page,
                    recordsTotal = recordsTotal
                });
            }
            catch
            {
                // let global middleware/logging handle details
                throw;
            }
        }

        // ============================================
        //               MODAL ENDPOINTS
        //  (GET -> Partial; POST -> JSON { ok: true })
        // ============================================

        // ----- DETAILS (VIEW) -----
        [HttpGet]
        public async Task<IActionResult> DetailsModal(int id)
        {
            var animal = await _work.Animal.GetByIdAsync(id);
            if (animal == null) return NotFound();
            return PartialView("Modals/_Details", animal);
        }

        // ----- CREATE (MODAL) -----
        [HttpGet]
        public IActionResult CreateModal()
        {
            ViewBag.Breeds = GetBreedSelectList();
            return PartialView("Modals/_CreateOrEdit", new Animal
            {
                DateOfBirth = DateTime.Today
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateModal([Bind("Id,TagNumber,Breed,DateOfBirth,Weight,PurchasePrice")] Animal model)
        {
            if (!IsValidBreed(model.Breed))
                ModelState.AddModelError(nameof(Animal.Breed), "Please select a valid breed.");

            if (_context.Animals.Any(a => a.TagNumber == model.TagNumber))
                ModelState.AddModelError(nameof(Animal.TagNumber), "Tag Number already exists!");

            if (!ModelState.IsValid)
            {
                ViewBag.Breeds = GetBreedSelectList(model.Breed);
                return PartialView("Modals/_CreateOrEdit", model);
            }

            await _work.Animal.AddAsync(model);
            await _work.CompleteAsync();
            return Json(new { ok = true });
        }

        // ----- EDIT (MODAL) -----
        [HttpGet]
        public async Task<IActionResult> EditModal(int id)
        {
            var animal = await _work.Animal.GetByIdAsync(id);
            if (animal == null) return NotFound();

            ViewBag.Breeds = GetBreedSelectList(animal.Breed);
            return PartialView("Modals/_CreateOrEdit", animal);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditModal([Bind("Id,TagNumber,Breed,DateOfBirth,Weight,PurchasePrice")] Animal model)
        {
            if (!IsValidBreed(model.Breed))
                ModelState.AddModelError(nameof(Animal.Breed), "Please select a valid breed.");

            if (!ModelState.IsValid)
            {
                ViewBag.Breeds = GetBreedSelectList(model.Breed);
                return PartialView("Modals/_CreateOrEdit", model);
            }

            _work.Animal.Update(model);
            await _work.CompleteAsync();
            return Json(new { ok = true });
        }

        // ----- DELETE (MODAL) -----
        [HttpGet]
        public async Task<IActionResult> DeleteModal(int id)
        {
            var animal = await _work.Animal.GetByIdAsync(id);
            if (animal == null) return NotFound();
            return PartialView("Modals/_Delete", animal);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteModalConfirmed(int id)
        {
            var animal = await _work.Animal.GetByIdAsync(id);
            if (animal == null) return NotFound();

            _work.Animal.Delete(animal);
            await _work.CompleteAsync();
            return Json(new { ok = true });
        }
    }
}
