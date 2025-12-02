using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerUp.Data;
using PowerUp.Models;
using PowerUp.Utility;

namespace PowerUp.Controllers;

[Authorize]
public class ScheduleController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : Controller
{
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    // Get trainer's schedule slots
    [Authorize(Roles = Roles.RoleTrainer)]
    public async Task<IActionResult> MySchedule()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Index", "Home");

        var trainer = await _context.Trainers
            .FirstOrDefaultAsync(t => t.ApplicationUserId == user.Id);

        if (trainer == null)
            return RedirectToAction("Index", "Home");

        var schedules = await _context.ScheduleSlots
            .Where(s => s.TrainerId == trainer.Id)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ToListAsync();

        return View(schedules);
    }

    // Create new schedule slot
    [Authorize(Roles = Roles.RoleTrainer)]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [Authorize(Roles = Roles.RoleTrainer)]
    public async Task<IActionResult> Create(int dayOfWeek, string startTime, string endTime, int capacity)
    {
        if (dayOfWeek < 0 || dayOfWeek > 6)
        {
            ModelState.AddModelError("", "Lütfen geçerli bir gün seçiniz.");
            return View();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Index", "Home");

        var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.ApplicationUserId == user.Id);

        if (trainer == null)
            return RedirectToAction("Index", "Home");

        if (!TimeOnly.TryParse(startTime, out var start) || !TimeOnly.TryParse(endTime, out var end))
        {
            ModelState.AddModelError("", "Geçersiz saat formatı.");
            return View();
        }

        if (end <= start)
        {
            ModelState.AddModelError("", "Bitiş saati başlangıç saatinden sonra olmalıdır.");
            return View();
        }

        var slot = new ScheduleSlot
        {
            TrainerId = trainer.Id,
            DayOfWeek = dayOfWeek,
            StartTime = start,
            EndTime = end,
            Capacity = capacity
        };

        _context.ScheduleSlots.Add(slot);
        await _context.SaveChangesAsync();

        return RedirectToAction("MySchedule");
    }

    [Authorize(Roles = Roles.RoleTrainer)]
    public async Task<IActionResult> Edit(int id)
    {
        var schedule = await _context.ScheduleSlots.FindAsync(id);
        if (schedule == null)
            return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Index", "Home");

        var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.ApplicationUserId == user.Id);

        if (trainer?.Id != schedule.TrainerId)
            return Forbid();

        return View(schedule);
    }

    [HttpPost]
    [Authorize(Roles = Roles.RoleTrainer)]
    public async Task<IActionResult> Edit(int id, int dayOfWeek, string startTime, string endTime, int capacity)
    {
        var schedule = await _context.ScheduleSlots.FindAsync(id);
        if (schedule == null)
            return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Index", "Home");

        var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.ApplicationUserId == user.Id);

        if (trainer?.Id != schedule.TrainerId)
            return Forbid();

        if (!TimeOnly.TryParse(startTime, out var start) || !TimeOnly.TryParse(endTime, out var end))
        {
            ModelState.AddModelError("", "Geçersiz saat formatı.");
            return View(schedule);
        }

        if (end <= start)
        {
            ModelState.AddModelError("", "Bitiş saati başlangıç saatinden sonra olmalıdır.");
            return View(schedule);
        }

        schedule.DayOfWeek = dayOfWeek;
        schedule.StartTime = start;
        schedule.EndTime = end;
        schedule.Capacity = capacity;
        schedule.UpdatedAt = DateTime.Now;

        _context.ScheduleSlots.Update(schedule);
        await _context.SaveChangesAsync();

        return RedirectToAction("MySchedule");
    }

    // Delete schedule slot
    [Authorize(Roles = Roles.RoleTrainer)]
    public async Task<IActionResult> Delete(int id)
    {
        var schedule = await _context.ScheduleSlots.FindAsync(id);
        if (schedule == null)
            return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Index", "Home");

        var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.ApplicationUserId == user.Id);

        if (trainer?.Id != schedule.TrainerId)
            return Forbid();

        return View(schedule);
    }

    [HttpPost]
    [ActionName("Delete")]
    [Authorize(Roles = Roles.RoleTrainer)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var schedule = await _context.ScheduleSlots.FindAsync(id);
        if (schedule == null)
            return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Index", "Home");

        var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.ApplicationUserId == user.Id);

        if (trainer?.Id != schedule.TrainerId)
            return Forbid();

        _context.ScheduleSlots.Remove(schedule);
        await _context.SaveChangesAsync();

        return RedirectToAction("MySchedule");
    }
}
