using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PowerUp.Models;

public class Subscription
{
    public int Id { get; set; }

    [ForeignKey("Gym")]
    public int GymId { get; set; }
    public Gym? Gym { get; set; }

    [ForeignKey("User")]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    [Required]
    [EnumDataType(typeof(SubscriptionType))]
    public SubscriptionType Type { get; set; }

    public DateTime StartDate { get; set; } = DateTime.Now;
    
    public DateTime? EndDate { get; set; }

    public bool IsActive { get; set; } = true;
}

public enum SubscriptionType
{
    Monthly,
    Annually
}
