using Microsoft.AspNetCore.Mvc;
using AgroManagement.Helper;
using System.Linq;

namespace AgroManagement.Controllers
{
    public class ShopController : Controller
    {

        // Example existing action
        public IActionResult Index()
        {
            return View();
        }

        // Existing bull details
        private bool IsLoggedIn()
        {
            // Adjust if your session key is different
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("UserType"));
        }

        public IActionResult BullDetails(string key)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Auth"); // or "UserLogin" page
            var product = ProductCatalog.All.FirstOrDefault(p => p.Key == key);
            if (product == null) return NotFound();

            return View("BullDetails", product);
        }

        // MilkyCow details action
        // URL: /Shop/MilkyCow?key=milky_cow
        public IActionResult MilkyCow(string key)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Auth");
            if (string.IsNullOrEmpty(key))
            {
                key = "milky_cow";
            }

            var product = ProductCatalog.All.FirstOrDefault(p => p.Key == key);
            if (product == null)
            {
                return NotFound();
            }

            return View("MilkyCow", product);
        }

        // 🔹 NEW: SmallCalf details action
        // URL: /Shop/SmallCalf?key=small_calf
        public IActionResult SmallCalf(string key)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Auth");
            if (string.IsNullOrEmpty(key))
            {
                key = "small_calf";
            }

            var product = ProductCatalog.All.FirstOrDefault(p => p.Key == key);
            if (product == null)
            {
                return NotFound();
            }

            // Looks for Views/Shop/SmallCalf.cshtml
            return View("SmallCalf", product);
        }
    }
}
