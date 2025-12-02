using Microsoft.AspNetCore.Identity;

namespace PowerUp.Models;

public class ApplicationUser : IdentityUser
{
    public ICollection<Appointment> Appointments { get; set; } = [];
    public ICollection<Subscription> Subscriptions { get; set; } = [];
}