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

    [Required(ErrorMessage = "Lütfen Haftanın Günü seçiniz.")]
    [Range(0, 6, ErrorMessage = "Gün 0-6 arasında olmalıdır.")]
    public int DayOfWeek { get; set; } // 0 = Monday, 1 = Tuesday, ..., 6 = Sunday

    [Required(ErrorMessage = "Lütfen Başlangıç Saati giriniz.")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "Lütfen Bitiş Saati giriniz.")]
    public TimeOnly EndTime { get; set; }

    [Required(ErrorMessage = "Lütfen Kapasite giriniz.")]
    [Range(1, 100, ErrorMessage = "Kapasite 1-100 arasında olmalıdır.")]
    public int Capacity { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
