using AgroManagement.Data;
using AgroManagement.Helper;
using AgroManagement.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroManagement.Controllers
{
    public class AdminExpenseController : Controller
    {
        private readonly AgroContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminExpenseController(AgroContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private bool IsAdmin()
            => HttpContext.Session.GetString("UserType") == "Admin";

        // =========================
        // MAIN EXPENSE REPORT PAGE
        // =========================
        public async Task<IActionResult> Index(int? year, int? month)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            var now = DateTime.Today;
            int y = year ?? now.Year;
            int m = month ?? now.Month;

            var vm = await BuildExpenseReportVM(y, m);
            return View(vm);
        }

        // =========================
        // PDF EXPORT
        // =========================
        public async Task<IActionResult> ExportMonthlyExpensePdf(int year, int month)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            var vm = await BuildExpenseReportVM(year, month);

            var pdfBytes = MonthlyExpensePdfHelper.Generate(year, month, vm);

            var fileName = $"ExpenseReport_{year:D4}_{month:D2}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        // =========================
        // CORE CALCULATION METHOD
        // =========================
        private async Task<ExpenseReportVM> BuildExpenseReportVM(int year, int month)
        {
            var vm = new ExpenseReportVM
            {
                Year = year,
                Month = month
            };

            // Load animals
            var animals = await _context.Animals
                .OrderBy(a => a.TagNumber)
                .ToListAsync();

            // Load employees
            var employees = await _context.Employees
                .OrderBy(e => e.EmployeeName)
                .ToListAsync();

            // Load health logs JSON for the month
            var contentRoot = _env.ContentRootPath;
            HealthLogHelper.EnsureInitialized(contentRoot);
            var monthLogs = HealthLogHelper.GetByMonth(contentRoot, year, month);

            // Animal expenses
            foreach (var animal in animals)
            {
                decimal medicalCost = 0;

                var animalLogs = monthLogs.Where(x => x.AnimalId == animal.Id && x.MedicineGiven);

                foreach (var log in animalLogs)
                {
                    if (!string.IsNullOrWhiteSpace(log.MedicineName) &&
                        MedicineCostCatalog.Cost.TryGetValue(log.MedicineName, out var cost))
                    {
                        medicalCost += cost;
                    }
                }

                vm.AnimalExpenses.Add(new AnimalExpenseVM
                {
                    AnimalId = animal.Id,
                    TagNumber = animal.TagNumber ?? "",
                    FoodExpense = AnimalExpenseCatalog.FoodPerMonth,
                    MaintenanceExpense = AnimalExpenseCatalog.MaintenancePerMonth,
                    MedicalExpense = medicalCost
                });
            }

            // Employee salary expenses
            foreach (var emp in employees)
            {
                vm.EmployeeSalaries.Add(new EmployeeSalaryVM
                {
                    EmployeeName = emp.EmployeeName,
                    Salary = emp.Salary
                });
            }

            return vm;
        }
    }
}
