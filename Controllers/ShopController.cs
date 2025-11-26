using Microsoft.AspNetCore.Mvc;

namespace AgroManagement.Controllers
{
    public class ShopController : Controller
    {
        // GET: /Shop
        public IActionResult Index()
        {
            return View();
        }
    }
}
