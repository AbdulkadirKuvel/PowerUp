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
        var gym = await _context.Gyms
            .Include(g => g.Features)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (gym == null)
            return NotFound();

        return View(gym);
    }

    [Authorize(Roles = Roles.RoleAdmin)]
    public async Task<IActionResult> Create()
    {
        // Veritabanından özellikleri çekip ViewBag'e atıyoruz
        var features = await _context.GymFeatures.ToListAsync();

        // Eğer veritabanı boşsa bile features boş bir liste olarak gelir, null gelmez.
        ViewBag.AvailableFeatures = features;

        return View();
    }

    [HttpPost]
[Authorize(Roles = Roles.RoleAdmin)]
public async Task<IActionResult> Create(Gym gym, List<int> selectedFeatureIds, string? customFeatures) // <-- Bu parametreler VAR MI?
{
    if (ModelState.IsValid)
    {
        // 1. Checkbox'tan seçilenleri ekleyen kısım BURASI
        if (selectedFeatureIds != null)
        {
            var featuresToAdd = await _context.GymFeatures
                .Where(f => selectedFeatureIds.Contains(f.Id))
                .ToListAsync();
            
            foreach (var feature in featuresToAdd)
            {
                gym.Features.Add(feature);
            }
        }

        // 2. Özel yazılanları ekleyen kısım
        if (!string.IsNullOrWhiteSpace(customFeatures))
        {
            // ... (önceki cevaptaki kodlar)
             var customNames = customFeatures.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
             foreach (var name in customNames)
             {
                 var existing = await _context.GymFeatures.FirstOrDefaultAsync(f => f.Name.ToLower() == name.ToLower());
                 if(existing != null) gym.Features.Add(existing);
                 else gym.Features.Add(new GymFeature { Name = name });
             }
        }

        _context.Gyms.Add(gym);
        await _context.SaveChangesAsync();
        return RedirectToAction("List");
    }
    
    // Hata varsa listeyi tekrar doldur
    ViewBag.AvailableFeatures = await _context.GymFeatures.ToListAsync();
    return View(gym);
}

[Authorize(Roles = Roles.RoleAdmin)]
    public async Task<IActionResult> Edit(int id)
    {
        // 1. Gym'i özellikleri (Features) ile birlikte çekiyoruz
        var gym = await _context.Gyms
            .Include(g => g.Features) 
            .FirstOrDefaultAsync(g => g.Id == id);

        if (gym == null)
            return NotFound();

        // 2. Tüm özellikleri checkbox listesi için ViewBag'e atıyoruz
        ViewBag.AvailableFeatures = await _context.GymFeatures.ToListAsync();

        return View(gym);
    }

    [HttpPost]
    [Authorize(Roles = Roles.RoleAdmin)]
    public async Task<IActionResult> Edit(int id, Gym gym, List<int> selectedFeatureIds, string? customFeatures)
    {
        if (id != gym.Id)
            return NotFound();

        if (ModelState.IsValid)
        {
            // 1. Veritabanındaki asıl kaydı (ilişkileriyle beraber) çekiyoruz
            var gymToUpdate = await _context.Gyms
                .Include(g => g.Features)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (gymToUpdate == null) return NotFound();

            // 2. Temel bilgileri güncelliyoruz
            gymToUpdate.Name = gym.Name;
            gymToUpdate.Address = gym.Address;
            gymToUpdate.MonthlyPrice = gym.MonthlyPrice;
            gymToUpdate.AnnuallyPrice = gym.AnnuallyPrice;

            // 3. Mevcut özellikleri temizliyoruz (Sıfırdan set edeceğiz)
            gymToUpdate.Features.Clear();

            // 4. Checkbox'tan seçilenleri ekle
            if (selectedFeatureIds != null)
            {
                var featuresToAdd = await _context.GymFeatures
                    .Where(f => selectedFeatureIds.Contains(f.Id))
                    .ToListAsync();

                foreach (var feature in featuresToAdd)
                {
                    gymToUpdate.Features.Add(feature);
                }
            }

            // 5. Yeni (Custom) yazılanları ekle
            if (!string.IsNullOrWhiteSpace(customFeatures))
            {
                var customNames = customFeatures.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var name in customNames)
                {
                    var existingFeature = await _context.GymFeatures.FirstOrDefaultAsync(f => f.Name.ToLower() == name.ToLower());
                    
                    if (existingFeature != null)
                    {
                        // Zaten varsa ve listede seçili değilse ekle
                        if (!gymToUpdate.Features.Contains(existingFeature))
                            gymToUpdate.Features.Add(existingFeature);
                    }
                    else
                    {
                        // Yoksa yeni oluştur
                        gymToUpdate.Features.Add(new GymFeature { Name = name });
                    }
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Gyms.Any(e => e.Id == id)) return NotFound();
                else throw;
            }
            return RedirectToAction("List");
        }

        // Hata varsa listeyi tekrar doldur
        ViewBag.AvailableFeatures = await _context.GymFeatures.ToListAsync();
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