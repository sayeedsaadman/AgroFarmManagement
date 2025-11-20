using Microsoft.AspNetCore.Mvc;
using AgroManagement.Data;
using AgroManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace AgroManagement.Controllers
{
    public class AuthController : Controller
    {
        private readonly AgroContext _context;

        public AuthController(AgroContext context)
        {
            _context = context;
        }

        // ============================
        // LOGIN (GET)
        // ============================
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // ============================
        // LOGIN (POST)
        // ============================
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password, string loginType)
        {
            // -------------------------
            // ADMIN LOGIN
            // -------------------------
            if (loginType == "admin")
            {
                if (username == "admin" && password == "12345")
                {
                    // store admin session
                    HttpContext.Session.SetString("UserType", "Admin");
                    HttpContext.Session.SetString("UserName", "Admin");

                    return RedirectToAction("Dashboard", "Admin");

                }

                ViewBag.Error = "Invalid admin credentials!";
                return View();
            }

            // -------------------------
            // USER LOGIN
            // -------------------------
            string hashedPassword = HashPassword(password);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.PasswordHash == hashedPassword);

            if (user != null)
            {
                // store user session
                HttpContext.Session.SetString("UserType", "User");
                HttpContext.Session.SetString("UserName", user.Username);

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Invalid username or password!";
            return View();
        }

        // ============================
        // REGISTER (GET)
        // ============================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // ============================
        // REGISTER (POST)
        // ============================
        [HttpPost]
        public async Task<IActionResult> Register(User user, string Password)
        {
            if (await _context.Users.AnyAsync(u => u.Username == user.Username))
            {
                ModelState.AddModelError("Username", "Username already exists.");
                return View(user);
            }

            user.PasswordHash = HashPassword(Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Account created successfully! Please login.";
            return RedirectToAction("Login");
        }

        // ============================
        // LOGOUT
        // ============================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // ============================
        // PASSWORD HASHING
        // ============================
        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return BitConverter.ToString(bytes).Replace("-", "");
        }
    }
}
