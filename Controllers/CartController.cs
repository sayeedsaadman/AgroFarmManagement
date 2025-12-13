using System.Linq;
using Microsoft.AspNetCore.Mvc;
using AgroManagement.Helper;
using AgroManagement.Models;

namespace AgroManagement.Controllers
{
    public class CartController : Controller
    {
        // Cart Page
        public IActionResult Index(string? msg = null)
        {
            var cart = CartSessionHelper.GetCart(HttpContext.Session);
            var vm = new CartVM { Items = cart };

            ViewBag.Msg = msg;
            return View(vm);
        }

        // Called from Shop "Buy Now"
        public IActionResult Add(string key)
        {
            var product = ProductCatalog.All.FirstOrDefault(p => p.Key == key);
            if (product == null)
                return RedirectToAction("Index", "Shop");

            // Ensure stock JSON exists
            StockHelper.EnsureInitialized(HttpContext.RequestServices
                .GetService(typeof(Microsoft.AspNetCore.Hosting.IWebHostEnvironment)) is Microsoft.AspNetCore.Hosting.IWebHostEnvironment env
                    ? env.ContentRootPath
                    : Directory.GetCurrentDirectory());

            var contentRoot = GetContentRoot();

            var cart = CartSessionHelper.GetCart(HttpContext.Session);
            var existing = cart.FirstOrDefault(x => x.ProductKey == key);

            var currentQtyInCart = existing?.Quantity ?? 0;
            var availableStock = StockHelper.GetStock(contentRoot, key);

            if (currentQtyInCart + 1 > availableStock)
                return RedirectToAction(nameof(Index), new { msg = $"Not enough stock. Only {availableStock} left." });

            if (existing == null)
            {
                cart.Add(new CartItem
                {
                    ProductKey = product.Key,
                    Name = product.Name,
                    UnitLabel = product.UnitLabel,
                    Price = product.Price,
                    Quantity = 1
                });
            }
            else
            {
                existing.Quantity += 1;
            }

            CartSessionHelper.SaveCart(HttpContext.Session, cart);
            return RedirectToAction(nameof(Index));
        }

        // Qty +
        public IActionResult Increase(string key)
        {
            var contentRoot = GetContentRoot();
            var cart = CartSessionHelper.GetCart(HttpContext.Session);
            var item = cart.FirstOrDefault(x => x.ProductKey == key);

            if (item == null) return RedirectToAction(nameof(Index));

            var availableStock = StockHelper.GetStock(contentRoot, key);
            if (item.Quantity + 1 > availableStock)
                return RedirectToAction(nameof(Index), new { msg = $"Not enough stock. Only {availableStock} left." });

            item.Quantity += 1;
            CartSessionHelper.SaveCart(HttpContext.Session, cart);

            return RedirectToAction(nameof(Index));
        }

        // Qty -
        public IActionResult Decrease(string key)
        {
            var cart = CartSessionHelper.GetCart(HttpContext.Session);
            var item = cart.FirstOrDefault(x => x.ProductKey == key);

            if (item == null) return RedirectToAction(nameof(Index));

            item.Quantity -= 1;
            if (item.Quantity <= 0)
                cart.Remove(item);

            CartSessionHelper.SaveCart(HttpContext.Session, cart);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(string key)
        {
            var cart = CartSessionHelper.GetCart(HttpContext.Session);
            var item = cart.FirstOrDefault(x => x.ProductKey == key);
            if (item != null) cart.Remove(item);

            CartSessionHelper.SaveCart(HttpContext.Session, cart);
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        public IActionResult ConfirmPurchase()
        {
            // Require login
            var userType = HttpContext.Session.GetString("UserType");
            if (string.IsNullOrEmpty(userType))
                return RedirectToAction("Login", "Auth");

            var username = HttpContext.Session.GetString("UserName") ?? "Unknown";

            var contentRoot = GetContentRoot();
            StockHelper.EnsureInitialized(contentRoot, defaultStockPerProduct: 10);
            SalesHelper.EnsureInitialized(contentRoot);

            var cart = CartSessionHelper.GetCart(HttpContext.Session);
            if (cart == null || cart.Count == 0)
                return RedirectToAction(nameof(Index), new { msg = "Cart is empty." });

            // Build request quantities
            var req = cart.ToDictionary(x => x.ProductKey, x => x.Quantity);

            // Atomic stock cut
            if (!StockHelper.TryDecreaseStockBulk(contentRoot, req, out var error))
            {
                return RedirectToAction(nameof(Index), new { msg = error });
            }

            // Record sale
            SalesHelper.RecordSale(contentRoot, username, cart);

            // Clear cart
            CartSessionHelper.ClearCart(HttpContext.Session);

            return RedirectToAction(nameof(Index), new { msg = "✅ Purchase successful!" });
        }

        // Checkout page (later we will confirm purchase)
        public IActionResult Checkout()
        {
            // Require login (user must be logged in)
            var userType = HttpContext.Session.GetString("UserType");
            if (string.IsNullOrEmpty(userType))
                return RedirectToAction("Login", "Auth");

            var cart = CartSessionHelper.GetCart(HttpContext.Session);
            var vm = new CartVM { Items = cart };

            return View(vm);
        }

        private string GetContentRoot()
        {
            var env = HttpContext.RequestServices.GetService(typeof(Microsoft.AspNetCore.Hosting.IWebHostEnvironment))
                    as Microsoft.AspNetCore.Hosting.IWebHostEnvironment;

            return env?.ContentRootPath ?? Directory.GetCurrentDirectory();
        }
    }
}
