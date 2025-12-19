using Microsoft.EntityFrameworkCore;
using PowerUp.Data;
using PowerUp.Models;

namespace PowerUp.Services;

public class AppointmentCompletionService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AppointmentCompletionService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1); // runs every minute in dev; adjust for production

    public AppointmentCompletionService(IServiceProvider services, ILogger<AppointmentCompletionService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AppointmentCompletionService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();

                var today = DateTime.Today;
                // Find appointments that are scheduled on or before today and still awaiting(0) or accepted(1)
                var toProcess = await db.Appointments
                    .Where(a => a.AppointmentDate.Date <= today && (a.Status == 0 || a.Status == 1))
                    .ToListAsync(stoppingToken);

                foreach (var appointment in toProcess)
                {
                    if (appointment.Status == 0)
                    {
                        appointment.Status = 2; // Rejected
                        appointment.UpdatedAt = DateTime.Now;

                        // notify user that request was rejected due to missed schedule
                        var title = "Randevunuz reddedildi";
                        var desc = $"Randevunuz otomatik olarak reddedildi. Tarih: {appointment.AppointmentDate:dd/MM/yyyy}.";
                        await notificationService.CreateNotificationAsync(appointment.UserId, title, desc);
                    }
                    else if (appointment.Status == 1)
                    {
                        appointment.Status = 3; // Completed
                        appointment.UpdatedAt = DateTime.Now;
                        appointment.CompletedAt = DateTime.Now;

                        // Create notification to user with action to rate
                        var title = "Randevu tamamlandı — Lütfen değerlendiriniz";
                        var desc = $"Randevunuz tamamlandı. Eğitmeni ve salonu değerlendirmek için " +
                                   "lütfen butona tıklayın.";
                        // store appointment id as payload
                        await notificationService.CreateNotificationAsync(appointment.UserId, title, desc, "rating", appointment.Id.ToString(), "Oy Ver");
                    }
                }

                if (toProcess.Count > 0)
                {
                    db.Appointments.UpdateRange(toProcess);
                    await db.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing appointments in AppointmentCompletionService.");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("AppointmentCompletionService stopped.");
    }
}
