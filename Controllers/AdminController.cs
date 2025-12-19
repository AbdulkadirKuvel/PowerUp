using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerUp.Data;
using PowerUp.Utility;

namespace PowerUp.Controllers;

[Authorize(Roles = Roles.RoleAdmin)]
public class AdminController(ApplicationDbContext context) : Controller
{
    private readonly ApplicationDbContext _context = context;

    public async Task<IActionResult> AllAppointments()
    {
        var appointments = await _context.Appointments
            .Include(a => a.User)
            .Include(a => a.Trainer)
            .Include(a => a.ScheduleSlot)
            .ThenInclude(s => s!.ScheduleSlotServices!)
            .ThenInclude(ss => ss.Service)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();

        return View(appointments);
    }

    // Manage featured items for home page (Öne çıkanlar)
    public async Task<IActionResult> Featured()
    {
        var trainers = await _context.Trainers
            .Include(t => t.Gym)
            .ToListAsync();

        var gyms = await _context.Gyms.ToListAsync();

        var trainerStats = trainers.Select(t => new {
            t.Id,
            t.Name,
            GymName = t.Gym != null ? t.Gym.Name : "-",
            AcceptedCount = _context.Appointments.Count(a => a.TrainerId == t.Id && a.Status == 1),
            AwaitingCount = _context.Appointments.Count(a => a.TrainerId == t.Id && a.Status == 0),
            RatedCount = _context.Ratings.Count(r => r.Appointment != null && r.Appointment.TrainerId == t.Id),
            AvgTrainerRating = _context.Ratings.Where(r => r.Appointment != null && r.Appointment.TrainerId == t.Id).Average(r => (double?)r.TrainerRating) ?? 0,
            AvgGymRating = _context.Ratings.Where(r => r.Appointment != null && r.Appointment.TrainerId == t.Id).Average(r => (double?)r.GymRating) ?? 0
        }).ToList();

        var gymStats = gyms.Select(g => new {
            g.Id,
            g.Name,
            AcceptedCount = _context.Appointments.Count(a => a.Trainer != null && a.Trainer.GymId == g.Id && a.Status == 1),
            AwaitingCount = _context.Appointments.Count(a => a.Trainer != null && a.Trainer.GymId == g.Id && a.Status == 0),
            RatedCount = _context.Ratings.Count(r => r.Appointment != null && r.Appointment.Trainer != null && r.Appointment.Trainer.GymId == g.Id),
            AvgGymRating = _context.Ratings.Where(r => r.Appointment != null && r.Appointment.Trainer != null && r.Appointment.Trainer.GymId == g.Id).Average(r => (double?)r.GymRating) ?? 0
        }).ToList();

        var featured = await _context.FeaturedItems
            .Where(f => f.IsActive)
            .OrderBy(f => f.Order)
            .Include(f => f.Trainer)
            .ThenInclude(t => t.Gym)
            .Include(f => f.Gym)
            .ToListAsync();

        ViewBag.Trainers = trainerStats;
        ViewBag.Gyms = gymStats;
        ViewBag.Featured = featured;

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Featured(int[] selectedTrainerIds, int[] selectedGymIds)
    {
        // Merge selections preserving order: trainers first then gyms based on input order
        var combined = new List<(int? trainerId, int? gymId)>();

        foreach (var t in selectedTrainerIds ?? new int[0]) combined.Add((t, null));
        foreach (var g in selectedGymIds ?? new int[0]) combined.Add((null, g));

        // Keep only first 4
        combined = combined.Take(4).ToList();

        // Clear existing featured items
        var existing = _context.FeaturedItems.ToList();
        _context.FeaturedItems.RemoveRange(existing);

        int order = 1;
        foreach (var item in combined)
        {
            _context.FeaturedItems.Add(new Models.FeaturedItem
            {
                TrainerId = item.trainerId,
                GymId = item.gymId,
                IsActive = true,
                Order = order++
            });
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Öne çıkanlar güncellendi.";
        return RedirectToAction("Featured");
    }
}
