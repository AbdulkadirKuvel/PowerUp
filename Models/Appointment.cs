using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PowerUp.Models;

public class Appointment
{
    public int Id { get; set; }

    [ForeignKey("Trainer")]
    public int TrainerId { get; set; }
    public Trainer? Trainer { get; set; }

    [ForeignKey("User")]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    [ForeignKey("ScheduleSlot")]
    public int ScheduleSlotId { get; set; }
    public ScheduleSlot? ScheduleSlot { get; set; }

    [Required]
    public DateTime AppointmentDate { get; set; }

    [Required]
    public TimeSpan AppointmentTime { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    // Status: 0 = Awaiting, 1 = Accepted, 2 = Rejected, 3 = Completed
    public int Status { get; set; } = 0; // 0 = Awaiting

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; } = null;
}
