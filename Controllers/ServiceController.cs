using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerUp.Data;
using PowerUp.Models;
using PowerUp.Utility;

namespace PowerUp.Controllers;

public class ServiceController(ApplicationDbContext context) : Controller
{
    private readonly ApplicationDbContext _context = context;

    public async Task<IActionResult> Index()
    {
        var services = await _context.Services.ToListAsync();
        return View(services);
    }

    public async Task<IActionResult> Details(int id)
    {
        var service = await _context.Services
            .Include(s => s.TrainerServices)
            .ThenInclude(ts => ts.Trainer)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (service == null)
            return NotFound();

        return View(service);
    }

    [Authorize(Roles = Roles.RoleAdmin)]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [Authorize(Roles = Roles.RoleAdmin)]
    public async Task<IActionResult> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError("name", "Lütfen hizmet adını giriniz.");
            return View();
        }

        var service = new Service { Name = name };
        _context.Services.Add(service);
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    [Authorize(Roles = Roles.RoleAdmin)]
    public async Task<IActionResult> Edit(int id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null)
            return NotFound();

        return View(service);
    }

    [HttpPost]
    [Authorize(Roles = Roles.RoleAdmin)]
    public async Task<IActionResult> Edit(int id, string name)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError("name", "Lütfen hizmet adını giriniz.");
            return View(service);
        }

        service.Name = name;
        _context.Services.Update(service);
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    [Authorize(Roles = Roles.RoleAdmin)]
    public async Task<IActionResult> Delete(int id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null)
            return NotFound();

        return View(service);
    }

    [HttpPost]
    [ActionName("Delete")]
    [Authorize(Roles = Roles.RoleAdmin)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null)
            return NotFound();

        _context.Services.Remove(service);
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    [Authorize(Roles = Roles.RoleTrainer)]
    public async Task<IActionResult> GetAvailableServices(int trainerId)
    {
        var trainer = await _context.Trainers
            .Include(t => t.TrainerServices)
            .FirstOrDefaultAsync(t => t.Id == trainerId);

        if (trainer == null)
            return NotFound();

        var assignedServiceIds = trainer.TrainerServices.Select(ts => ts.ServiceId).ToList();
        var availableServices = await _context.Services
            .Where(s => !assignedServiceIds.Contains(s.Id))
            .Select(s => new { id = s.Id, name = s.Name })
            .ToListAsync();

        return Json(availableServices);
    }
}
