using AgroManagement.Data;
using AgroManagement.Helper;
using AgroManagement.Models;
using AgroManagement.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;


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
        var contentRoot = (HttpContext.RequestServices.GetService(typeof(Microsoft.AspNetCore.Hosting.IWebHostEnvironment))
    as Microsoft.AspNetCore.Hosting.IWebHostEnvironment)?.ContentRootPath
    ?? Directory.GetCurrentDirectory();

        AgroManagement.Helper.SalesHelper.EnsureInitialized(contentRoot);
        ViewBag.TotalIncome = AgroManagement.Helper.SalesHelper.GetTotalIncome(contentRoot);

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
    // MANAGE PRODUCTS (STOCK) - JSON
    // -----------------------------
    public IActionResult Products()
    {
        // Admin only (your session auth)
        var userType = HttpContext.Session.GetString("UserType");
        if (userType != "Admin") return RedirectToAction("Login", "Auth");

        var contentRoot = GetContentRoot();

        StockHelper.EnsureInitialized(contentRoot, 10);

        var stocks = StockHelper.GetAllStocks(contentRoot);

        var list = ProductCatalog.All
            .Select(p => new AdminProductVM
            {
                Key = p.Key,
                Category = p.Category,
                Name = p.Name,
                UnitLabel = p.UnitLabel,
                Price = p.Price,
                Stock = stocks.ContainsKey(p.Key) ? stocks[p.Key] : 0
            })
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Name)
            .ToList();

        return View(list);
    }

    // -----------------------------
    // ADD PRODUCT (INCREASE STOCK) - GET
    // -----------------------------
    [HttpGet]
    public IActionResult AddProduct()
    {
        var userType = HttpContext.Session.GetString("UserType");
        if (userType != "Admin") return RedirectToAction("Login", "Auth");

        return View();
    }

    // -----------------------------
    // ADD PRODUCT (INCREASE STOCK) - POST
    // -----------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddProduct(string key, int qty)
    {
        var userType = HttpContext.Session.GetString("UserType");
        if (userType != "Admin") return RedirectToAction("Login", "Auth");

        if (string.IsNullOrWhiteSpace(key) || qty <= 0)
            return RedirectToAction(nameof(Products));

        var contentRoot = GetContentRoot();
        StockHelper.EnsureInitialized(contentRoot, 10);

        StockHelper.IncreaseStock(contentRoot, key, qty);

        return RedirectToAction(nameof(Products));
    }

    // -----------------------------
    // SET STOCK (DIRECT UPDATE) - POST
    // -----------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetStock(string key, int stock)
    {
        var userType = HttpContext.Session.GetString("UserType");
        if (userType != "Admin") return RedirectToAction("Login", "Auth");

        if (string.IsNullOrWhiteSpace(key))
            return RedirectToAction(nameof(Products));

        var contentRoot = GetContentRoot();
        StockHelper.EnsureInitialized(contentRoot, 10);

        StockHelper.SetStock(contentRoot, key, stock);

        return RedirectToAction(nameof(Products));
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
    private string GetContentRoot()
    {
        var env = (HttpContext.RequestServices.GetService(typeof(Microsoft.AspNetCore.Hosting.IWebHostEnvironment))
            as Microsoft.AspNetCore.Hosting.IWebHostEnvironment);

        return env?.ContentRootPath ?? Directory.GetCurrentDirectory();
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
