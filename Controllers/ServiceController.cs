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
        var services = await _context.Services
            .Include(s => s.TrainerServices)
            .ThenInclude(ts => ts.Trainer)
            .ToListAsync();
        return View(services);
    }

    // Detaylarda artık o hizmeti veren hocaların fiyatlarını da görebileceğiz.
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

    // --- MEVCUT METOT ---
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

    // --- YENİ EKLENMESİ GEREKEN METOT ---
    // Antrenör kendine hizmet eklerken artık Fiyat bilgisini de göndermeli.
    [HttpPost]
    [Authorize(Roles = Roles.RoleTrainer)] 
    public async Task<IActionResult> AddServiceToTrainer(int trainerId, int serviceId, decimal price)
    {
        // 1. Antrenör ve Hizmet var mı kontrol et
        var trainer = await _context.Trainers.FindAsync(trainerId);
        var service = await _context.Services.FindAsync(serviceId);

        if (trainer == null || service == null)
        {
            return Json(new { success = false, message = "Eğitmen veya Hizmet bulunamadı." });
        }

        // 2. Bu hizmet daha önce eklenmiş mi?
        var exists = await _context.Set<TrainerService>()
            .AnyAsync(ts => ts.TrainerId == trainerId && ts.ServiceId == serviceId);

        if (exists)
        {
            return Json(new { success = false, message = "Bu hizmet zaten ekli." });
        }

        // 3. İlişkiyi FİYAT bilgisiyle birlikte kaydet
        var trainerService = new TrainerService
        {
            TrainerId = trainerId,
            ServiceId = serviceId,
            Price = price // Fiyat burada set ediliyor
        };

        _context.Add(trainerService);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Hizmet ve fiyat başarıyla eklendi." });
    }
}