using System.ComponentModel.DataAnnotations.Schema;

namespace PowerUp.Models;

public class TrainerService
{
    public int TrainerId { get; set; } // FK1
    public Trainer? Trainer { get; set; }

    public int ServiceId { get; set; } // FK2
    public Service? Service { get; set; }

    [Column(TypeName = "decimal(18,2)")] 
    public decimal Price { get; set; }
}