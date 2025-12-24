using AgroManagement.Data;
using AgroManagement.Models;
using AgroManagement.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AgroManagement.Controllers
{
    public class EmployeesController : Controller
    {
        private readonly AgroContext _context;
        private bool IsAdmin()
    => HttpContext.Session.GetString("UserType") == "Admin";
        // =======================
        // MODAL / DRAWER ACTIONS
        // =======================

        [HttpGet]
        public async Task<IActionResult> CreateModal()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            ViewBag.Animals = await _context.Animals.OrderBy(a => a.Breed).ToListAsync();
            return View("CreateEditModal", new EmployeeCreateVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateModal(EmployeeCreateVM vm)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            ViewBag.Animals = await _context.Animals.OrderBy(a => a.Breed).ToListAsync();

            if (!ModelState.IsValid)
                return View("CreateEditModal", vm);

            bool exists = await _context.Employees.AnyAsync(e => e.EmployeeCode == vm.EmployeeCode);
            if (exists)
            {
                ModelState.AddModelError("EmployeeCode", "Employee ID already exists. Please change it.");
                return View("CreateEditModal", vm);
            }

            var employee = new Employee
            {
                EmployeeCode = vm.EmployeeCode,
                EmployeeName = vm.EmployeeName,
                EmployeeNumber = vm.EmployeeNumber,
                Salary = vm.Salary,
                Address = vm.Address
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            // ✅ TASK ASSIGNMENT (same logic as your Create)
            if (vm.SelectedAnimalId.HasValue &&
                vm.SelectedTaskNames != null &&
                vm.SelectedTaskNames.Any())
            {
                int animalId = vm.SelectedAnimalId.Value;

                var empCodesQuery = _context.Employees.Select(e => e.EmployeeCode);

                var alreadyAssignedNormalized = await _context.EmployeeTasks
                    .Where(t => t.AnimalId == animalId && empCodesQuery.Contains(t.EmployeeCode))
                    .Select(t => t.TaskName.Trim().ToLower())
                    .ToListAsync();

                var newTasks = vm.SelectedTaskNames
                    .Select(t => t?.Trim() ?? "")
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Where(t => !alreadyAssignedNormalized.Contains(t.Trim().ToLower()))
                    .DistinctBy(t => t.Trim().ToLower())
                    .ToList();

                foreach (var taskName in newTasks)
                {
                    _context.EmployeeTasks.Add(new EmployeeTask
                    {
                        EmployeeCode = employee.EmployeeCode,
                        AnimalId = animalId,
                        TaskName = taskName.Trim()
                    });
                }

                await _context.SaveChangesAsync();
            }

            return Json(new { ok = true });
        }

        [HttpGet]
        public async Task<IActionResult> EditModal(string id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            if (string.IsNullOrEmpty(id)) return NotFound();

            var employee = await _context.Employees
                .Include(e => e.Tasks)
                .FirstOrDefaultAsync(e => e.EmployeeCode == id);

            if (employee == null) return NotFound();

            ViewBag.Animals = await _context.Animals.OrderBy(a => a.Breed).ToListAsync();

            var firstAnimalId = employee.Tasks.FirstOrDefault()?.AnimalId;

            var vm = new EmployeeCreateVM
            {
                EmployeeCode = employee.EmployeeCode,
                EmployeeName = employee.EmployeeName,
                EmployeeNumber = employee.EmployeeNumber,
                Salary = employee.Salary,
                Address = employee.Address,
                SelectedAnimalId = firstAnimalId
            };

            return View("CreateEditModal", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditModal(string id, EmployeeCreateVM vm)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            if (id != vm.EmployeeCode) return NotFound();

            ViewBag.Animals = await _context.Animals.OrderBy(a => a.Breed).ToListAsync();

            if (!ModelState.IsValid)
                return View("CreateEditModal", vm);

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeCode == id);
            if (employee == null) return NotFound();

            employee.EmployeeName = vm.EmployeeName;
            employee.EmployeeNumber = vm.EmployeeNumber;
            employee.Salary = vm.Salary;
            employee.Address = vm.Address;

            _context.Employees.Update(employee);

            // ✅ keep your task edit logic
            if (vm.SelectedAnimalId.HasValue)
            {
                int animalId = vm.SelectedAnimalId.Value;

                var selectedNormalized = (vm.SelectedTaskNames ?? new List<string>())
                    .Select(t => Norm(t))
                    .ToHashSet();

                var existingRows = await _context.EmployeeTasks
                    .Where(t => t.EmployeeCode == id && t.AnimalId == animalId)
                    .ToListAsync();

                var existingNormalized = existingRows.Select(t => Norm(t.TaskName)).ToHashSet();

                var empCodesQuery = _context.Employees.Select(e => e.EmployeeCode);

                var assignedByOthers = await _context.EmployeeTasks
                    .Where(t => t.AnimalId == animalId
                                && t.EmployeeCode != id
                                && empCodesQuery.Contains(t.EmployeeCode))
                    .Select(t => t.TaskName.Trim().ToLower())
                    .ToListAsync();

                var assignedByOthersSet = assignedByOthers.ToHashSet();

                var toRemove = existingRows
                    .Where(r => !selectedNormalized.Contains(Norm(r.TaskName)))
                    .ToList();

                if (toRemove.Any())
                    _context.EmployeeTasks.RemoveRange(toRemove);

                var toAdd = selectedNormalized
                    .Where(t => !existingNormalized.Contains(t))
                    .Where(t => !assignedByOthersSet.Contains(t))
                    .ToList();

                foreach (var taskNorm in toAdd)
                {
                    var displayTask = MasterTasks.First(x => Norm(x) == taskNorm);

                    _context.EmployeeTasks.Add(new EmployeeTask
                    {
                        EmployeeCode = id,
                        AnimalId = animalId,
                        TaskName = displayTask.Trim()
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { ok = true });
        }

        [HttpGet]
        public async Task<IActionResult> DeleteModal(string id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            if (string.IsNullOrEmpty(id)) return NotFound();

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeCode == id);
            if (employee == null) return NotFound();

            return View("DeleteModal", employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteModalConfirmed(string id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeCode == id);
            if (employee == null) return NotFound();

            var tasks = await _context.EmployeeTasks.Where(t => t.EmployeeCode == id).ToListAsync();
            if (tasks.Any()) _context.EmployeeTasks.RemoveRange(tasks);

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            return Json(new { ok = true });
        }

        [HttpGet]
        public async Task<IActionResult> DetailsModal(string id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            if (string.IsNullOrEmpty(id)) return NotFound();

            var employee = await _context.Employees
                .Include(e => e.Tasks)
                .ThenInclude(t => t.Animal)
                .FirstOrDefaultAsync(e => e.EmployeeCode == id);

            if (employee == null) return NotFound();

            return View("DetailsModal", employee);
        }

        public EmployeesController(AgroContext context)
        {
            _context = context;
        }

        // ✅ Fixed Master Task List (8 tasks)
        private static readonly List<string> MasterTasks = new()
        {
            "Stall Cleaning (Remove waste)",
            "Feed Distribution",
            "Water Refill",
            "Milk Collection",
            "Health Check",
            "Vaccination Support",
            "Grooming / Washing",
            "Barn Sanitization"
        };

        private static string Norm(string s) => (s ?? "").Trim().ToLower();

        // =============================
        // ✅ TABULATOR REMOTE LIST ENDPOINT
        // =============================
        // GET: /Employees/GetEmployeeList?searchquery={...}
        [HttpGet]
        public async Task<IActionResult> GetEmployeeList(string? searchquery)
        {
            var q = _context.Employees
                .Include(e => e.Tasks)
                    .ThenInclude(t => t.Animal)
                .AsQueryable();

            int page = 1;
            int size = 10;

            // Parse Tabulator params
            if (!string.IsNullOrWhiteSpace(searchquery))
            {
                try
                {
                    using var doc = JsonDocument.Parse(searchquery);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("page", out var p) && p.TryGetInt32(out var pInt))
                        page = Math.Max(1, pInt);

                    if (root.TryGetProperty("size", out var s) && s.TryGetInt32(out var sInt))
                        size = Math.Clamp(sInt, 1, 200);

                    // Filters
                    if (root.TryGetProperty("filter", out var filterArr) && filterArr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var f in filterArr.EnumerateArray())
                        {
                            var field = f.TryGetProperty("field", out var ff) ? ff.GetString() : null;
                            var value = f.TryGetProperty("value", out var fv) ? fv.GetString() : null;

                            if (string.IsNullOrWhiteSpace(field) || string.IsNullOrWhiteSpace(value))
                                continue;

                            value = value.Trim();

                            switch (field)
                            {
                                case "employeeName":
                                    q = q.Where(x => x.EmployeeName.Contains(value));
                                    break;

                                case "employeeCode":
                                    q = q.Where(x => x.EmployeeCode.Contains(value));
                                    break;

                                case "employeeNumber":
                                    q = q.Where(x => x.EmployeeNumber.Contains(value));
                                    break;

                                case "address":
                                    q = q.Where(x => x.Address.Contains(value));
                                    break;
                            }
                        }
                    }

                    // Sorting (remote)
                    if (root.TryGetProperty("sort", out var sortArr) && sortArr.ValueKind == JsonValueKind.Array)
                    {
                        var first = sortArr.EnumerateArray().FirstOrDefault();
                        if (first.ValueKind != JsonValueKind.Undefined)
                        {
                            var field = first.TryGetProperty("field", out var sf) ? sf.GetString() : null;
                            var dir = first.TryGetProperty("dir", out var sd) ? sd.GetString() : "asc";
                            bool desc = string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase);

                            q = (field, desc) switch
                            {
                                ("employeeName", false) => q.OrderBy(x => x.EmployeeName),
                                ("employeeName", true) => q.OrderByDescending(x => x.EmployeeName),

                                ("employeeCode", false) => q.OrderBy(x => x.EmployeeCode),
                                ("employeeCode", true) => q.OrderByDescending(x => x.EmployeeCode),

                                ("salary", false) => q.OrderBy(x => x.Salary),
                                ("salary", true) => q.OrderByDescending(x => x.Salary),

                                _ => q.OrderByDescending(x => x.EmployeeCode),
                            };
                        }
                    }
                    else
                    {
                        q = q.OrderByDescending(x => x.EmployeeCode);
                    }
                }
                catch
                {
                    // ignore bad json; fall back to default
                    q = q.OrderByDescending(x => x.EmployeeCode);
                }
            }
            else
            {
                q = q.OrderByDescending(x => x.EmployeeCode);
            }

            var total = await q.CountAsync();

            var data = await q
                .Skip((page - 1) * size)
                .Take(size)
                .Select(e => new
                {
                    employeeName = e.EmployeeName,
                    employeeCode = e.EmployeeCode,
                    employeeNumber = e.EmployeeNumber,
                    salary = e.Salary,
                    address = e.Address,

                    tasksCount = e.Tasks.Count,

                    // show short summary; full list still possible
                    tasksSummary = e.Tasks.Any()
                        ? string.Join(" | ", e.Tasks.Select(t =>
                            t.TaskName + " (" + (t.Animal != null ? t.Animal.TagNumber : "NoTag") + ")"))
                        : "No tasks assigned"
                })
                .ToListAsync();

            var lastPage = (int)Math.Ceiling(total / (double)size);

            return Json(new
            {
                data,
                last_page = lastPage,
                current_page = page,
                recordsTotal = total
            });
        }

        // =============================
        // Employee Management (list)
        // =============================
        public async Task<IActionResult> Index()
        {
            // For Tabulator page, we don't need Model list, but keeping your action same
            // You can return View() only, but we won't break anything here.
            var employees = await _context.Employees
                .Include(e => e.Tasks)
                .ThenInclude(t => t.Animal)
                .OrderByDescending(e => e.EmployeeCode)
                .ToListAsync();

            return View(employees);
        }

        // Add Employee form
        public async Task<IActionResult> Create()
        {
            ViewBag.Animals = await _context.Animals
                .OrderBy(a => a.Breed)
                .ToListAsync();

            return View(new EmployeeCreateVM());
        }

        // ✅ Returns ONLY tasks not already assigned for this animal (by existing employees)
        [HttpGet]
        public async Task<IActionResult> GetAvailableTasks(int animalId)
        {
            var empCodesQuery = _context.Employees.Select(e => e.EmployeeCode);

            var assignedNormalized = await _context.EmployeeTasks
                .Where(t => t.AnimalId == animalId && empCodesQuery.Contains(t.EmployeeCode))
                .Select(t => t.TaskName.Trim().ToLower())
                .ToListAsync();

            var available = MasterTasks
                .Where(t => !assignedNormalized.Contains(t.Trim().ToLower()))
                .ToList();

            return Json(available);
        }

        // ✅ For Edit page: return master tasks with checked/disabled state
        [HttpGet]
        public async Task<IActionResult> GetTasksForAnimal(int animalId, string employeeCode)
        {
            var empCodesQuery = _context.Employees.Select(e => e.EmployeeCode);

            var assignedRows = await _context.EmployeeTasks
                .Include(t => t.Animal)
                .Where(t => t.AnimalId == animalId && empCodesQuery.Contains(t.EmployeeCode))
                .ToListAsync();

            var assignedNormalized = assignedRows
                .Select(t => Norm(t.TaskName))
                .ToHashSet();

            var employeeAssignedNormalized = assignedRows
                .Where(t => t.EmployeeCode == employeeCode)
                .Select(t => Norm(t.TaskName))
                .ToHashSet();

            var result = MasterTasks.Select(task => new
            {
                name = task,
                isChecked = employeeAssignedNormalized.Contains(Norm(task)),
                isDisabled = assignedNormalized.Contains(Norm(task))
                             && !employeeAssignedNormalized.Contains(Norm(task))
            });

            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeCreateVM vm)
        {
            ViewBag.Animals = await _context.Animals.OrderBy(a => a.Breed).ToListAsync();

            if (!ModelState.IsValid)
                return View(vm);

            bool exists = await _context.Employees.AnyAsync(e => e.EmployeeCode == vm.EmployeeCode);
            if (exists)
            {
                ModelState.AddModelError("EmployeeCode", "Employee ID already exists. Please change it.");
                return View(vm);
            }

            var employee = new Employee
            {
                EmployeeCode = vm.EmployeeCode,
                EmployeeName = vm.EmployeeName,
                EmployeeNumber = vm.EmployeeNumber,
                Salary = vm.Salary,
                Address = vm.Address
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            // ✅ TASK ASSIGNMENT
            if (vm.SelectedAnimalId.HasValue &&
                vm.SelectedTaskNames != null &&
                vm.SelectedTaskNames.Any())
            {
                int animalId = vm.SelectedAnimalId.Value;

                var empCodesQuery = _context.Employees.Select(e => e.EmployeeCode);

                var alreadyAssignedNormalized = await _context.EmployeeTasks
                    .Where(t => t.AnimalId == animalId && empCodesQuery.Contains(t.EmployeeCode))
                    .Select(t => t.TaskName.Trim().ToLower())
                    .ToListAsync();

                var newTasks = vm.SelectedTaskNames
                    .Select(t => t?.Trim() ?? "")
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Where(t => !alreadyAssignedNormalized.Contains(t.Trim().ToLower()))
                    .DistinctBy(t => t.Trim().ToLower())
                    .ToList();

                foreach (var taskName in newTasks)
                {
                    _context.EmployeeTasks.Add(new EmployeeTask
                    {
                        EmployeeCode = employee.EmployeeCode,
                        AnimalId = animalId,
                        TaskName = taskName.Trim()
                    });
                }

                await _context.SaveChangesAsync();
            }

            TempData["success"] = "Employee added successfully!";
            return RedirectToAction(nameof(Index));
        }

        // EDIT EMPLOYEE
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var employee = await _context.Employees
                .Include(e => e.Tasks)
                .FirstOrDefaultAsync(e => e.EmployeeCode == id);

            if (employee == null) return NotFound();

            ViewBag.Animals = await _context.Animals
                .OrderBy(a => a.Breed)
                .ToListAsync();

            var firstAnimalId = employee.Tasks.FirstOrDefault()?.AnimalId;

            var vm = new EmployeeCreateVM
            {
                EmployeeCode = employee.EmployeeCode,
                EmployeeName = employee.EmployeeName,
                EmployeeNumber = employee.EmployeeNumber,
                Salary = employee.Salary,
                Address = employee.Address,
                SelectedAnimalId = firstAnimalId
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EmployeeCreateVM vm)
        {
            if (id != vm.EmployeeCode) return NotFound();

            ViewBag.Animals = await _context.Animals
                .OrderBy(a => a.Breed)
                .ToListAsync();

            if (!ModelState.IsValid)
                return View(vm);

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeCode == id);

            if (employee == null) return NotFound();

            employee.EmployeeName = vm.EmployeeName;
            employee.EmployeeNumber = vm.EmployeeNumber;
            employee.Salary = vm.Salary;
            employee.Address = vm.Address;

            _context.Employees.Update(employee);

            if (vm.SelectedAnimalId.HasValue)
            {
                int animalId = vm.SelectedAnimalId.Value;

                var selectedNormalized = (vm.SelectedTaskNames ?? new List<string>())
                    .Select(t => Norm(t))
                    .ToHashSet();

                var existingRows = await _context.EmployeeTasks
                    .Where(t => t.EmployeeCode == id && t.AnimalId == animalId)
                    .ToListAsync();

                var existingNormalized = existingRows
                    .Select(t => Norm(t.TaskName))
                    .ToHashSet();

                var empCodesQuery = _context.Employees.Select(e => e.EmployeeCode);

                var assignedByOthers = await _context.EmployeeTasks
                    .Where(t => t.AnimalId == animalId
                                && t.EmployeeCode != id
                                && empCodesQuery.Contains(t.EmployeeCode))
                    .Select(t => t.TaskName.Trim().ToLower())
                    .ToListAsync();

                var assignedByOthersSet = assignedByOthers.ToHashSet();

                var toRemove = existingRows
                    .Where(r => !selectedNormalized.Contains(Norm(r.TaskName)))
                    .ToList();

                if (toRemove.Any())
                    _context.EmployeeTasks.RemoveRange(toRemove);

                var toAdd = selectedNormalized
                    .Where(t => !existingNormalized.Contains(t))
                    .Where(t => !assignedByOthersSet.Contains(t))
                    .ToList();

                foreach (var taskNorm in toAdd)
                {
                    var displayTask = MasterTasks.First(x => Norm(x) == taskNorm);

                    _context.EmployeeTasks.Add(new EmployeeTask
                    {
                        EmployeeCode = id,
                        AnimalId = animalId,
                        TaskName = displayTask.Trim()
                    });
                }
            }

            await _context.SaveChangesAsync();

            TempData["success"] = "Employee updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // DELETE EMPLOYEE
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeCode == id);

            if (employee == null) return NotFound();

            return View(employee);
        }

        // ✅ ONE-TIME TOOL: Remove tasks whose employee doesn't exist
        public async Task<IActionResult> CleanupOrphanTasks()
        {
            var userType = HttpContext.Session.GetString("UserType");
            if (userType != "Admin") return RedirectToAction("Login", "Auth");

            var empCodes = await _context.Employees
                .Select(e => e.EmployeeCode)
                .ToListAsync();

            var orphan = await _context.EmployeeTasks
                .Where(t => !empCodes.Contains(t.EmployeeCode))
                .ToListAsync();

            if (orphan.Any())
            {
                _context.EmployeeTasks.RemoveRange(orphan);
                await _context.SaveChangesAsync();
            }

            TempData["success"] = $"Removed {orphan.Count} orphan tasks.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeCode == id);

            if (employee == null) return NotFound();

            var tasks = await _context.EmployeeTasks
                .Where(t => t.EmployeeCode == id)
                .ToListAsync();

            if (tasks.Any())
                _context.EmployeeTasks.RemoveRange(tasks);

            _context.Employees.Remove(employee);

            await _context.SaveChangesAsync();

            TempData["success"] = "Employee deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
