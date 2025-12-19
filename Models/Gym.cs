using System.ComponentModel.DataAnnotations;

namespace PowerUp.Models;

public class Gym
{
    public int Id { get; set; }

    [Length(5, 50, ErrorMessage = "İsim 5-50 Karakter Uzunluğu Arasında Olmalıdır.")]
    public required string Name { get; set; }

    [MaxLength(150, ErrorMessage = "Adres En Fazla 150 Karakter Olabilir.")]
    public required string Address { get; set; }

    public int MonthlyPrice { get; set; }

    public int AnnuallyPrice { get; set; }

    public ICollection<Trainer> Trainers { get; set; } = [];
    public ICollection<Subscription> Subscriptions { get; set; } = [];
    
    // YENİ EKLENEN KISIM:
    public ICollection<GymFeature> Features { get; set; } = [];
}