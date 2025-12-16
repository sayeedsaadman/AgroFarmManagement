using AgroManagement.Data;
using AgroManagement.Models;
using AgroManagement.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroManagement.Controllers
{
    public class EmployeesController : Controller
    {
        private readonly AgroContext _context;

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

        // Employee Management (list)
        public async Task<IActionResult> Index()
        {
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

            // EF-safe normalization (no custom Norm inside SQL)
            var assignedNormalized = await _context.EmployeeTasks
                .Where(t => t.AnimalId == animalId && empCodesQuery.Contains(t.EmployeeCode))
                .Select(t => t.TaskName.Trim().ToLower())
                .ToListAsync();

            // keep stable order (MasterTasks order)
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

            // only consider tasks assigned by existing employees
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

                // only consider tasks assigned by existing employees
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

            // update employee fields
            employee.EmployeeName = vm.EmployeeName;
            employee.EmployeeNumber = vm.EmployeeNumber;
            employee.Salary = vm.Salary;
            employee.Address = vm.Address;

            _context.Employees.Update(employee);

            // ✅ Task edit logic (only if animal selected)
            if (vm.SelectedAnimalId.HasValue)
            {
                int animalId = vm.SelectedAnimalId.Value;

                var selectedNormalized = (vm.SelectedTaskNames ?? new List<string>())
                    .Select(t => Norm(t))
                    .ToHashSet();

                // tasks this employee already has for this animal
                var existingRows = await _context.EmployeeTasks
                    .Where(t => t.EmployeeCode == id && t.AnimalId == animalId)
                    .ToListAsync();

                var existingNormalized = existingRows
                    .Select(t => Norm(t.TaskName))
                    .ToHashSet();

                // ✅ tasks assigned to other EXISTING employees for this animal
                var empCodesQuery = _context.Employees.Select(e => e.EmployeeCode);

                var assignedByOthers = await _context.EmployeeTasks
                    .Where(t => t.AnimalId == animalId
                                && t.EmployeeCode != id
                                && empCodesQuery.Contains(t.EmployeeCode))
                    .Select(t => t.TaskName.Trim().ToLower())
                    .ToListAsync();

                var assignedByOthersSet = assignedByOthers.ToHashSet();

                // remove unchecked tasks
                var toRemove = existingRows
                    .Where(r => !selectedNormalized.Contains(Norm(r.TaskName)))
                    .ToList();

                if (toRemove.Any())
                    _context.EmployeeTasks.RemoveRange(toRemove);

                // add newly checked tasks, but only if NOT assigned to others
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
        // Call: /Employees/CleanupOrphanTasks (Admin only), then you can delete this method
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

            // ✅ remove tasks first (prevents orphan tasks)
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
