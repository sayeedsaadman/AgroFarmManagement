using AgroManagement.Data;
using AgroManagement.Models;
using AgroManagement.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroManagement.Controllers
{
    public class ProfileController : Controller
    {
        private readonly AgroContext _db;

        public ProfileController(AgroContext db)
        {
            _db = db;
        }

        // GET: /Profile
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // ✅ your project stores session as "UserName"
            var username = HttpContext.Session.GetString("UserName");

            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Login", "Auth");

            // Find the user from DB by Username
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Username == username);
            if (user == null)
                return NotFound();

            // Map DB model -> VM
            var vm = new ProfileVM
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.Name,   // ✅ your User model uses Name
                Email = user.Email,
                Phone = user.Phone
            };

            return View(vm);
        }

        // POST: /Profile/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(ProfileVM vm)
        {
            if (!ModelState.IsValid)
                return View("Index", vm);

            var user = await _db.Users.FindAsync(vm.Id);
            if (user == null)
                return NotFound();

            // Update fields
            user.Name = vm.FullName;  // ✅ map back to Name
            user.Email = vm.Email;
            user.Phone = vm.Phone;

            await _db.SaveChangesAsync();

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("Index");
        }

        // POST: /Profile/DeleteAccount
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            _db.Users.Remove(user);  // hard delete
            await _db.SaveChangesAsync();

            // Clear session & logout
            HttpContext.Session.Clear();

            return RedirectToAction("Login", "Auth");
        }
    }
}
