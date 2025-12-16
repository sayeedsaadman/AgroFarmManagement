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
            // Require login before adding to cart
            var userType = HttpContext.Session.GetString("UserType");
            if (string.IsNullOrEmpty(userType))
                return RedirectToAction("Login", "Auth");

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
        [HttpGet]
        public IActionResult Invoice()
        {
            var userType = HttpContext.Session.GetString("UserType");
            if (string.IsNullOrEmpty(userType))
                return RedirectToAction("Login", "Auth");
            var dateStr = HttpContext.Session.GetString("LastInvoiceDateUtc");
            var invoiceDateUtc = string.IsNullOrWhiteSpace(dateStr) ? DateTime.UtcNow : DateTime.Parse(dateStr);

            var invoiceNo = HttpContext.Session.GetString("LastInvoiceNo");
            if (string.IsNullOrWhiteSpace(invoiceNo))
                return RedirectToAction(nameof(Index));

            var username = HttpContext.Session.GetString("UserName") ?? "Unknown";

            // ✅ Read items saved during ConfirmPurchase
            var json = HttpContext.Session.GetString("LastInvoiceItems");
            var items = string.IsNullOrWhiteSpace(json)
                ? new List<CartItem>()
                : (System.Text.Json.JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>());

            if (items.Count == 0)
                return RedirectToAction(nameof(Index), new { msg = "Invoice items missing. Please purchase again." });

            // ✅ Build the viewmodel for PDF
            var vm = new CartVM { Items = items };

            // ✅ Use invoice time (optional: store it in session too)
            var pdfBytes = InvoicePdfHelper.BuildInvoicePdf(
                invoiceNo,
                username,
                DateTime.UtcNow,
                vm
            );

            return File(pdfBytes, "application/pdf", $"{invoiceNo}.pdf");
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
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmPurchase()
        {
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

            var req = cart.ToDictionary(x => x.ProductKey, x => x.Quantity);

            if (!StockHelper.TryDecreaseStockBulk(contentRoot, req, out var error))
                return RedirectToAction(nameof(Index), new { msg = error });

            // ✅ make invoice number
            var invoiceNo = "INV-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            // ✅ record sale (we will also store invoiceNo in Session for now)
            SalesHelper.RecordSale(contentRoot, username, cart);

            // ✅ store invoice no temporarily
            HttpContext.Session.SetString("LastInvoiceNo", invoiceNo);
            HttpContext.Session.SetString("LastInvoiceDateUtc", DateTime.UtcNow.ToString("o"));

            // ✅ Save items for invoice BEFORE clearing cart
            HttpContext.Session.SetString(
                "LastInvoiceItems",
                System.Text.Json.JsonSerializer.Serialize(cart)
            );

            // Clear cart AFTER saving invoice items
            CartSessionHelper.ClearCart(HttpContext.Session);

            // redirect to success page (or wherever you go)
            return RedirectToAction("PurchaseSuccess");

        }


        [HttpGet]
        public IActionResult PurchaseSuccess()
        {
            var userType = HttpContext.Session.GetString("UserType");
            if (string.IsNullOrEmpty(userType))
                return RedirectToAction("Login", "Auth");

            var invoiceNo = HttpContext.Session.GetString("LastInvoiceNo");
            if (string.IsNullOrWhiteSpace(invoiceNo))
                return RedirectToAction(nameof(Index));

            ViewBag.InvoiceNo = invoiceNo;
            return View();
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
