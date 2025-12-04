using PowerUp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace PowerUp.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
: IdentityDbContext<ApplicationUser>(options)
{

    public DbSet<Gym> Gyms { get; set; }
    public DbSet<Trainer> Trainers { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<TrainerService> TrainerServices { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<ScheduleSlot> ScheduleSlots { get; set; }
    public DbSet<ScheduleSlotService> ScheduleSlotServices { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<TrainerService>()
            .HasKey(ts => new { ts.TrainerId, ts.ServiceId });
        
        modelBuilder.Entity<ScheduleSlotService>()
            .HasKey(sss => new { sss.ScheduleSlotId, sss.ServiceId });
    }
}