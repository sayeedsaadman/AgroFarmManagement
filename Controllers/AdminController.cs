using Microsoft.AspNetCore.Mvc;
using AgroManagement.Data;
using AgroManagement.Models;
using System.Linq;
using AgroManagement.Models.ViewModels;
using Microsoft.EntityFrameworkCore;


public class AdminController : Controller
{
    private readonly AgroContext _context;

    public AdminController(AgroContext context)
    {
        _context = context;
    }

    // Dashboard
    public async Task<IActionResult> Dashboard()
    {
        const int TASKS_PER_ANIMAL = 8;

        var totalEmployees = await _context.Employees.CountAsync();
        var totalAnimals = await _context.Animals.CountAsync();
        var totalUsers = await _context.Users.CountAsync();

        var totalTasksAssigned = await _context.EmployeeTasks.CountAsync();

        var totalTasksPossible = totalAnimals * TASKS_PER_ANIMAL;
        var totalTasksUnassigned = totalTasksPossible - totalTasksAssigned;
        if (totalTasksUnassigned < 0) totalTasksUnassigned = 0;

        var tasksByEmployee = await _context.EmployeeTasks
            .GroupBy(t => t.EmployeeCode)
            .Select(g => new { EmployeeCode = g.Key, Count = g.Count() })
            .ToListAsync();

        var empLookup = await _context.Employees
            .ToDictionaryAsync(e => e.EmployeeCode, e => e.EmployeeName);

        var employeeNames = new List<string>();
        var tasksPerEmployee = new List<int>();

        foreach (var row in tasksByEmployee.OrderByDescending(x => x.Count))
        {
            employeeNames.Add(empLookup.ContainsKey(row.EmployeeCode)
                ? empLookup[row.EmployeeCode]
                : row.EmployeeCode);

            tasksPerEmployee.Add(row.Count);
        }

        var vm = new DashboardVM
        {
            TotalEmployees = totalEmployees,
            TotalAnimals = totalAnimals,
            TotalUsers = totalUsers,

            TotalTasksAssigned = totalTasksAssigned,
            TasksRemaining = totalTasksUnassigned,

            TotalTasksPossible = totalTasksPossible,
            TotalTasksUnassigned = totalTasksUnassigned,

            EmployeeNames = employeeNames,
            TasksPerEmployee = tasksPerEmployee
        };

        return View(vm);
    }

    // -----------------------------
    // MANAGE USERS (LIST)
    // -----------------------------
    public IActionResult ManageUsers()
    {
        var users = _context.Users.ToList();
        return View(users);
    }

    // -----------------------------
    // USER DETAILS
    // -----------------------------
    public IActionResult UserDetails(int id)
    {
        var user = _context.Users.FirstOrDefault(u => u.Id == id);
        if (user == null) return NotFound();

        return View(user);
    }

    // -----------------------------
    // EDIT USER (GET)
    // -----------------------------
    public IActionResult EditUser(int id)
    {
        var user = _context.Users.Find(id);
        if (user == null) return NotFound();

        return View(user);
    }

    // -----------------------------
    // EDIT USER (POST)
    // -----------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditUser(User user)
    {
        if (!ModelState.IsValid)
        {
            return View(user);
        }

        _context.Users.Update(user);
        _context.SaveChanges();
        return RedirectToAction("ManageUsers");
    }

    // -----------------------------
    // DELETE USER (GET)
    // -----------------------------
    public IActionResult DeleteUser(int id)
    {
        var user = _context.Users.Find(id);
        if (user == null) return NotFound();

        return View(user);
    }

    // -----------------------------
    // DELETE USER (POST)
    // -----------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteUserConfirmed(int id)
    {
        var user = _context.Users.Find(id);
        if (user != null)
        {
            _context.Users.Remove(user);
            _context.SaveChanges();
        }

        return Json(new { ok = true });

    }

    // -----------------------------
    // FETCH PASSWORD FROM DATABASE
    // -----------------------------
    [HttpGet]
    public IActionResult GetUserPassword(int id)
    {
        var user = _context.Users.FirstOrDefault(u => u.Id == id);
        if (user == null)
            return Json(new { success = false, password = "" });

        // returning PasswordHash (because that's what you store)
        return Json(new { success = true, password = user.PasswordHash });
    }

}
