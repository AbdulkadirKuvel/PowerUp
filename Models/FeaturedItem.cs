using System.ComponentModel.DataAnnotations.Schema;

namespace PowerUp.Models;

public class FeaturedItem
{
    public int Id { get; set; }

    // One of TrainerId or GymId should be populated
    public int? TrainerId { get; set; }
    public Trainer? Trainer { get; set; }

    public int? GymId { get; set; }
    public Gym? Gym { get; set; }

    public int Order { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}