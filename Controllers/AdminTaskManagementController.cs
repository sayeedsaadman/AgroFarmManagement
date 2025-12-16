using AgroManagement.Data;
using AgroManagement.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroManagement.Controllers
{
    public class AdminTaskManagementController : Controller
    {
        private readonly AgroContext _context;

        public AdminTaskManagementController(AgroContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Optional Admin session check (enable if you want)
            // if (HttpContext.Session.GetString("UserType") != "Admin")
            //     return RedirectToAction("Login", "Auth");

            var list = await _context.Employees
                .Select(e => new AdminEmployeeTaskManagementVM
                {
                    EmployeeCode = e.EmployeeCode,
                    EmployeeName = e.EmployeeName,
                    TaskCount = _context.EmployeeTasks.Count(t => t.EmployeeCode == e.EmployeeCode)
                })
                .OrderBy(x => x.EmployeeName)
                .ToListAsync();

            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkDone(string employeeCode)
        {
            var tasks = await _context.EmployeeTasks
                .Where(t => t.EmployeeCode == employeeCode)
                .ToListAsync();

            if (tasks.Any())
            {
                _context.EmployeeTasks.RemoveRange(tasks);
                await _context.SaveChangesAsync();
                TempData["success"] = "All tasks cleared for the employee.";
            }
            else
            {
                TempData["success"] = "Employee already has no assigned tasks.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
