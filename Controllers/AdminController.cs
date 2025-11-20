using Microsoft.AspNetCore.Mvc;
using AgroManagement.Data;

public class AdminController : Controller
{
    private readonly AgroContext _context;

    public AdminController(AgroContext context)
    {
        _context = context;
    }

    public IActionResult Dashboard()
    {
        var totalAnimals = _context.Animals.Count();
        var totalUsers = _context.Users.Count();

        ViewBag.TotalAnimals = totalAnimals;
        ViewBag.TotalUsers = totalUsers;

        return View();
    }
}
