using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PowerUp.Models;

public class ScheduleSlotService
{
    public int Id { get; set; }

    [ForeignKey("ScheduleSlot")]
    public int ScheduleSlotId { get; set; }
    public ScheduleSlot? ScheduleSlot { get; set; }

    [ForeignKey("Service")]
    public int ServiceId { get; set; }
    public Service? Service { get; set; }
}
