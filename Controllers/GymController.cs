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
            .Include(g => g.Trainers)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (gym == null)
            return NotFound();

        return View(gym);
    }

    [Authorize(Roles = Roles.RoleAdmin)]
    public async Task<IActionResult> Create()
    {
        var features = await _context.GymFeatures.ToListAsync();

        ViewBag.AvailableFeatures = features;

        return View();
    }

    [HttpPost]
    [Authorize(Roles = Roles.RoleAdmin)]
    public async Task<IActionResult> Create(Gym gym, List<int> selectedFeatureIds, string? customFeatures)
    {
        if (ModelState.IsValid)
        {
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

            if (!string.IsNullOrWhiteSpace(customFeatures))
            {
                var customNames = customFeatures.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var name in customNames)
                {
                    var existing = await _context.GymFeatures.FirstOrDefaultAsync(f => f.Name.ToLower() == name.ToLower());
                    if (existing != null) gym.Features.Add(existing);
                    else gym.Features.Add(new GymFeature { Name = name });
                }
            }

            _context.Gyms.Add(gym);
            await _context.SaveChangesAsync();
            return RedirectToAction("List");
        }

        ViewBag.AvailableFeatures = await _context.GymFeatures.ToListAsync();
        return View(gym);
    }

    [Authorize(Roles = Roles.RoleAdmin)]
    public async Task<IActionResult> Edit(int id)
    {
        var gym = await _context.Gyms
            .Include(g => g.Features)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (gym == null)
            return NotFound();

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
            var gymToUpdate = await _context.Gyms
                .Include(g => g.Features)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (gymToUpdate == null) return NotFound();

            gymToUpdate.Name = gym.Name;
            gymToUpdate.Address = gym.Address;
            gymToUpdate.MonthlyPrice = gym.MonthlyPrice;
            gymToUpdate.AnnuallyPrice = gym.AnnuallyPrice;

            gymToUpdate.OpeningTime = gym.OpeningTime;
            gymToUpdate.ClosingTime = gym.ClosingTime;

            gymToUpdate.Features.Clear();

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

            if (!string.IsNullOrWhiteSpace(customFeatures))
            {
                var customNames = customFeatures.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var name in customNames)
                {
                    var existingFeature = await _context.GymFeatures.FirstOrDefaultAsync(f => f.Name.ToLower() == name.ToLower());

                    if (existingFeature != null)
                    {
                        if (!gymToUpdate.Features.Contains(existingFeature))
                            gymToUpdate.Features.Add(existingFeature);
                    }
                    else
                    {
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

        ViewBag.AvailableFeatures = await _context.GymFeatures.ToListAsync();
        return View(gym);
    }

    [Authorize(Roles = Roles.RoleAdmin)]
    [HttpGet]
    public async Task<IActionResult> CheckDeleteStatus(int id)
    {
        var gym = await _context.Gyms
            .Include(g => g.Trainers)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (gym == null)
        {
            return Json(new { success = false, message = "Salon bulunamadı." });
        }

        bool hasTrainers = gym.Trainers.Count != 0;
        int trainerCount = gym.Trainers.Count;

        return Json(new
        {
            success = true,
            hasTrainers,
            trainerCount,
            gymName = gym.Name
        });
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