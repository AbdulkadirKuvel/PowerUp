using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerUp.Data;
using PowerUp.Models;

namespace PowerUp.Controllers.Api;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RatingController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public RatingController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public class RatingDto
    {
        public int AppointmentId { get; set; }
        public int TrainerRating { get; set; }
        public int GymRating { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> CreateRating([FromBody] RatingDto dto)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var appointment = await _context.Appointments
            .Include(a => a.Trainer)
            .FirstOrDefaultAsync(a => a.Id == dto.AppointmentId);

        if (appointment == null) return NotFound();
        if (appointment.UserId != user.Id) return Forbid();

        // prevent duplicate rating for same appointment
        if (await _context.Ratings.AnyAsync(r => r.AppointmentId == dto.AppointmentId && r.UserId == user.Id))
        {
            return BadRequest(new { message = "Bu randevu için zaten oy kullandınız." });
        }

        var rating = new Rating
        {
            AppointmentId = dto.AppointmentId,
            TrainerRating = Math.Clamp(dto.TrainerRating, 1, 5),
            GymRating = Math.Clamp(dto.GymRating, 1, 5),
            UserId = user.Id
        };

        _context.Ratings.Add(rating);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Teşekkürler, puanınız kaydedildi." });
    }
}
