using Microsoft.AspNetCore.Mvc;
using AgroManagement.Data;
using AgroManagement.Models;
using System.Linq;

public class AdminController : Controller
{
    private readonly AgroContext _context;

    public AdminController(AgroContext context)
    {
        _context = context;
    }

    // Dashboard
    public IActionResult Dashboard()
    {
        var totalAnimals = _context.Animals.Count();
        var totalUsers = _context.Users.Count();

        ViewBag.TotalAnimals = totalAnimals;
        ViewBag.TotalUsers = totalUsers;

        return View();
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
