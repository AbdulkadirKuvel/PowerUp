using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Net.Http.Headers;
namespace PowerUp.Models;

public class Trainer
{
    public int Id { get; set; }

    [Length(5, 50, ErrorMessage = "İsim 5-50 Karakter Uzunluğu Arasında Olmalıdır.")]
    public required string Name { get; set; }

    [Phone(ErrorMessage = "Telefon Numarasının Doğruluğunu Kontrol Ediniz.")]
    public required string PhoneNumber { get; set; }

    [ForeignKey("Gym")]
    public int GymId { get; set; }
    public Gym? Gym { get; set; }

    public string? GymName => Gym?.Name ?? "Spor Salonu Bilinmiyor";

    public string Specialization { get; set; } = "Genel Fitness";

    [ForeignKey("ApplicationUser")]
    public string? ApplicationUserId { get; set; }
    public ApplicationUser? ApplicationUser { get; set; }

    public ICollection<TrainerService> TrainerServices { get; set; } = [];
    public ICollection<Appointment> Appointments { get; set; } = [];
    public ICollection<ScheduleSlot> ScheduleSlots { get; set; } = [];
}