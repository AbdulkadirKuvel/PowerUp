using PowerUp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace PowerUp.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
: IdentityDbContext<ApplicationUser>(options)
{

    public DbSet<Gym> Gyms { get; set; }
    public DbSet<GymFeature> GymFeatures { get; set; }
    public DbSet<Trainer> Trainers { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<TrainerService> TrainerServices { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<ScheduleSlot> ScheduleSlots { get; set; }
    public DbSet<ScheduleSlotService> ScheduleSlotServices { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Rating> Ratings { get; set; }
    public DbSet<FeaturedItem> FeaturedItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<TrainerService>()
            .HasKey(ts => new { ts.TrainerId, ts.ServiceId });

        modelBuilder.Entity<ScheduleSlotService>()
            .HasKey(sss => new { sss.ScheduleSlotId, sss.ServiceId });

        modelBuilder.Entity<GymFeature>().HasData(
            new GymFeature { Id = 1, Name = "Profesyonel Ekipmanlar", IconClass = "fas fa-dumbbell" },
            new GymFeature { Id = 2, Name = "Duş ve Soyunma", IconClass = "fas fa-shower" },
            new GymFeature { Id = 3, Name = "Ücretsiz Wi-Fi", IconClass = "fas fa-wifi" },
            new GymFeature { Id = 4, Name = "Sauna", IconClass = "fas fa-hot-tub" },
            new GymFeature { Id = 5, Name = "Otopark", IconClass = "fas fa-parking" },
            new GymFeature { Id = 6, Name = "Kafeterya", IconClass = "fas fa-coffee" }
        );
    }
}