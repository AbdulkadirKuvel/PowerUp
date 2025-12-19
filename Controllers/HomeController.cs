using System.Diagnostics;
using PowerUp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PowerUp.Controllers;

public class HomeController(ILogger<HomeController> logger, PowerUp.Data.ApplicationDbContext context) : Controller
{
    private readonly ILogger<HomeController> _logger = logger;
    private readonly PowerUp.Data.ApplicationDbContext _context = context;

    public async Task<IActionResult> Index()
    {
        var featured = await _context.FeaturedItems
            .Where(f => f.IsActive)
            .OrderBy(f => f.Order)
            .Include(f => f.Trainer)
            .ThenInclude(t => t.Gym)
            .Include(f => f.Gym)
            .ToListAsync();

        ViewBag.Featured = featured;
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
