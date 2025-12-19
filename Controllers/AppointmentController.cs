using PowerUp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using PowerUp.Models;

namespace PowerUp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : ControllerBase
{
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    [HttpGet("rejected-today")]
    public async Task<IActionResult> GetRejectedTodayAppointments()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var today = DateTime.Today;
        var rejectedAppointments = await _context.Appointments
            .Where(a => a.UserId == user.Id && 
                        a.Status == 2 && // Rejected
                        a.AppointmentDate == today)
            .Include(a => a.Trainer)
            .Include(a => a.ScheduleSlot)
            .OrderByDescending(a => a.UpdatedAt)
            .Select(a => new
            {
                id = a.Id,
                trainerName = a.Trainer!.Name,
                appointmentDate = a.AppointmentDate,
                scheduleSlotHour = a.ScheduleSlot!.Hour,
                updatedAt = a.UpdatedAt
            })
            .ToListAsync();

        return Ok(rejectedAppointments);
    }
}
