using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PowerUp.Models;

public class ScheduleSlot
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Lütfen Eğitmeni seçiniz.")]
    [ForeignKey("Trainer")]
    public int TrainerId { get; set; }
    public Trainer? Trainer { get; set; }

    [Required(ErrorMessage = "Lütfen Spor Merkezini seçiniz.")]
    [ForeignKey("Gym")]
    public int GymId { get; set; }
    public Gym? Gym { get; set; }

    [Required(ErrorMessage = "Lütfen Haftanın Günü seçiniz.")]
    [Range(0, 6, ErrorMessage = "Gün 0-6 arasında olmalıdır.")]
    public int DayOfWeek { get; set; } // 0 = Monday, 1 = Tuesday, ..., 6 = Sunday

    [Required(ErrorMessage = "Lütfen Saat seçiniz.")]
    [Range(0, 23, ErrorMessage = "Saat 0-23 arasında olmalıdır.")]
    public int Hour { get; set; } // 0-23 representing the start hour of the session

    [Required(ErrorMessage = "Lütfen tekrar seçimi yapınız.")]
    public bool IsWeekly { get; set; } = false; // false = daily, true = weekly

    public ICollection<ScheduleSlotService>? ScheduleSlotServices { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
