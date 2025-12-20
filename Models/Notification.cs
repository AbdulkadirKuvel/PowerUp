using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PowerUp.Models;

public class Notification
{
    public int Id { get; set; }

    [ForeignKey("User")]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Optional action for in-notification actions (e.g., 'rating')
    [MaxLength(100)]
    public string? ActionType { get; set; }

    [MaxLength(200)]
    public string? ActionPayload { get; set; }

    [MaxLength(50)]
    public string? ActionLabel { get; set; }
}
