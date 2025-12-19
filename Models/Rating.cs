using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PowerUp.Models;

public class Rating
{
    public int Id { get; set; }

    [ForeignKey("Appointment")]
    public int AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    [Range(1,5)]
    public int TrainerRating { get; set; }

    [Range(1,5)]
    public int GymRating { get; set; }

    [ForeignKey("User")]
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}