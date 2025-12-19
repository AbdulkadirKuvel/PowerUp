using System.ComponentModel.DataAnnotations;

namespace PowerUp.Models;

public class GymFeature
{
    public int Id { get; set; }

    [Required]
    public required string Name { get; set; }

    // FontAwesome class'ı (örn: "fas fa-dumbbell"). Boşsa varsayılan atanır.
    public string IconClass { get; set; } = "fas fa-check-circle"; 

    // Çoka-çok ilişki için
    public ICollection<Gym> Gyms { get; set; } = [];
}