using Microsoft.AspNetCore.Mvc;
using AgroManagement.Helper;
using System;

namespace AgroManagement.Controllers
{
    public class AdminSalesController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            var userType = HttpContext.Session.GetString("UserType");
            if (userType != "Admin") return RedirectToAction("Login", "Auth");

            var root = GetContentRoot();
            var vm = SalesHelper.GetSalesAnalytics(root);
            return View(vm);
        }

        [HttpGet]
        public IActionResult MonthlyPdf(int year, int month)
        {
            var userType = HttpContext.Session.GetString("UserType");
            if (userType != "Admin") return RedirectToAction("Login", "Auth");

            var root = GetContentRoot();
            var pdfBytes = MonthlySalesPdfHelper.BuildMonthlySalesReportPdf(root, year, month);

            return File(pdfBytes, "application/pdf", $"SalesReport-{year}-{month:00}.pdf");
        }

        private string GetContentRoot()
        {
            var env = HttpContext.RequestServices.GetService(typeof(Microsoft.AspNetCore.Hosting.IWebHostEnvironment))
                    as Microsoft.AspNetCore.Hosting.IWebHostEnvironment;

            return env?.ContentRootPath ?? Directory.GetCurrentDirectory();
        }
    }
}
