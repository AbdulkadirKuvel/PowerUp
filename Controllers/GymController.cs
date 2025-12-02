using PowerUp.Data;
using PowerUp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using PowerUp.Utility;

namespace PowerUp.Controllers;

public class GymController(ApplicationDbContext context) : Controller
{
    private readonly ApplicationDbContext _context = context;

    public IActionResult Index()
    {
        return RedirectToAction("List");
    }

    public async Task<IActionResult> List()
    {
        var gyms = await _context.Gyms.ToListAsync();
        return View(gyms);
    }

    public async Task<IActionResult> Details(int id)
    {
        var gym = await _context.Gyms.FirstOrDefaultAsync(g => g.Id == id);
        if (gym == null)
            return NotFound();
        return View(gym);
    }

    [Authorize(Roles = Roles.RoleAdmin)]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [Authorize(Roles = Roles.RoleAdmin)]
    public async Task<IActionResult> Create(Gym gym)
    {
        if (ModelState.IsValid)
        {
            _context.Gyms.Add(gym);
            await _context.SaveChangesAsync();
            return RedirectToAction("List");
        }
        return View(gym);
    }

    [Authorize(Roles = Roles.RoleAdmin)]
    public async Task<IActionResult> Edit(int id)
    {
        var gym = await _context.Gyms.FindAsync(id);
        if (gym == null)
            return NotFound();
        return View(gym);
    }

    [HttpPost]
    [Authorize(Roles = Roles.RoleAdmin)]
    public async Task<IActionResult> Edit(int id, Gym gym)
    {
        if (id != gym.Id)
            return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(gym);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return NotFound();
            }
            return RedirectToAction("List");
        }
        return View(gym);
    }

    [Authorize(Roles = Roles.RoleAdmin)]
    public async Task<IActionResult> Delete(int id)
    {
        var gym = await _context.Gyms.FindAsync(id);
        if (gym == null)
            return NotFound();
        return View(gym);
    }

    [HttpPost]
    [Authorize(Roles = Roles.RoleAdmin)]
    public async Task<IActionResult> Delete(int id, Gym gym)
    {
        var gymToDelete = await _context.Gyms.FindAsync(id);
        if (gymToDelete != null)
        {
            _context.Gyms.Remove(gymToDelete);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("List");
    }
}