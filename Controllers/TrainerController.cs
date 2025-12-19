using PowerUp.Data;
using PowerUp.Models;
using PowerUp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using PowerUp.Utility;

namespace PowerUp.Controllers;

public class TrainerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, NotificationService notificationService) : Controller
{
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly NotificationService _notificationService = notificationService;

    public async Task<IActionResult> Index()
    {
        var trainers = await _context.Trainers
            .Include(t => t.Gym)
            .Include(t => t.TrainerServices)
            .ThenInclude(ts => ts.Service)
            .ToListAsync();

        return View(trainers);
    }

    public async Task<IActionResult> Details(int id)
    {
        var trainerDetails = await _context.Trainers
            .Include(t => t.Gym)
            .Include(t => t.TrainerServices)
            .ThenInclude(ts => ts.Service)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (trainerDetails == null)
            return NotFound();
        else
            return View(trainerDetails);
    }

    [HttpGet]
    [Route("api/trainer/{id}/booked-appointments")]
    public async Task<IActionResult> GetBookedAppointments(int id)
    {
        // Get all accepted appointments for this trainer
        var bookedAppointments = await _context.Appointments
            .Where(a => a.TrainerId == id && a.Status == 1) // Status 1 = Accepted
            .Select(a => new
            {
                scheduleSlotId = a.ScheduleSlotId,
                appointmentDate = a.AppointmentDate
            })
            .ToListAsync();

        return Json(bookedAppointments);
    }

    [HttpGet]
    [Route("api/trainer/current/trainer-id")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUserTrainerId()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var trainer = await _context.Trainers
            .FirstOrDefaultAsync(t => t.ApplicationUserId == user.Id);

        if (trainer == null)
            return Ok(new { trainerId = (int?)null });

        return Ok(new { trainerId = trainer.Id });
    }

    [Authorize]
    public async Task<IActionResult> TakeAppointment(int id)
    {
        var trainer = await _context.Trainers
            .Include(t => t.ScheduleSlots)!
            .ThenInclude(s => s.ScheduleSlotServices!)
            .ThenInclude(sss => sss.Service)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (trainer == null)
            return NotFound();

        return View(trainer);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> BookAppointment(int trainerId, int scheduleSlotId, DateTime appointmentDate, string? notes)
    {
        var user = await _userManager.GetUserAsync(User);

        // Validate input
        if (trainerId <= 0 || scheduleSlotId <= 0 || appointmentDate == default)
        {
            return BadRequest(new { success = false, message = "Lütfen tüm gerekli alanları doldurunuz." });
        }

        var trainerData = await _context.Trainers
            .Include(t => t.ScheduleSlots)
            .FirstOrDefaultAsync(t => t.Id == trainerId);
        var scheduleSlot = await _context.ScheduleSlots
            .Include(s => s.ScheduleSlotServices!)
            .ThenInclude(ss => ss.Service)
            .FirstOrDefaultAsync(s => s.Id == scheduleSlotId);

        if (trainerData == null || scheduleSlot == null)
            return NotFound();

        // Prevent trainers from booking their own appointments
        if (!string.IsNullOrEmpty(trainerData.ApplicationUserId) && trainerData.ApplicationUserId == user!.Id)
        {
            return BadRequest(new { success = false, message = "Kendi zamanlarınıza randevu alamazsınız!" });
        }

        // Allow multiple awaiting appointments at the same time slot
        // They will be auto-rejected when trainer accepts one
        var appointment = new Appointment
        {
            TrainerId = trainerId,
            UserId = user!.Id,
            ScheduleSlotId = scheduleSlotId,
            AppointmentDate = appointmentDate,
            AppointmentTime = TimeSpan.FromHours(scheduleSlot.Hour),
            Notes = notes ?? string.Empty,
            Status = 0, // Awaiting
            CreatedAt = DateTime.Now
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        // Notify trainer about the appointment request
        var userName = user!.UserName ?? "Bilinmeyen Kullanıcı";
        var serviceNames = string.Join(", ", scheduleSlot.ScheduleSlotServices?.Select(ss => ss.Service?.Name) ?? new List<string>());
        var timeStr = $"{scheduleSlot.Hour}:00 - {scheduleSlot.Hour + 1}:00";
        var appointmentDateStr = appointmentDate.ToString("dd.MM.yyyy");
        var notificationDescription = $"Yeni randevu isteği: {userName}\nTarih: {appointmentDateStr}, Saat: {timeStr}\nHizmet: {serviceNames}";
        if (!string.IsNullOrEmpty(trainerData.ApplicationUserId))
        {
            await _notificationService.CreateNotificationAsync(trainerData.ApplicationUserId, "Yeni Randevu İsteği", notificationDescription);
        }

        // Return response that will trigger dialog box on client side
        return Ok(new
        {
            success = true,
            message = "Randevu başarıyla oluşturuldu!",
            trainerId = trainerId
        });
    }

    [Authorize(Roles = Roles.RoleAdmin)]
    public async Task<IActionResult> Create()
    {
        var gyms = await _context.Gyms.ToListAsync();
        ViewBag.Gyms = gyms;
        return View();
    }

    [HttpPost]
    [Authorize(Roles = Roles.RoleAdmin)]
    public async Task<IActionResult> Create(string name, string phoneNumber, int gymId, string email, string password)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(phoneNumber) || gymId <= 0 ||
            string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ModelState.AddModelError(string.Empty, "Tüm alanlar gereklidir.");
            var gyms = await _context.Gyms.ToListAsync();
            ViewBag.Gyms = gyms;
            return View();
        }

        // Check if email already exists
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            ModelState.AddModelError("email", "Bu e-posta adresi zaten kullanılmaktadır.");
            var gyms = await _context.Gyms.ToListAsync();
            ViewBag.Gyms = gyms;
            return View();
        }

        // Create ApplicationUser for the trainer
        var trainerUser = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var createUserResult = await _userManager.CreateAsync(trainerUser, password);

        if (!createUserResult.Succeeded)
        {
            foreach (var error in createUserResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            var gyms = await _context.Gyms.ToListAsync();
            ViewBag.Gyms = gyms;
            return View();
        }

        // Assign Trainer role to the user
        await _userManager.AddToRoleAsync(trainerUser, Roles.RoleTrainer);

        // Create Trainer record linked to the user
        var trainer = new PowerUp.Models.Trainer
        {
            Name = name,
            PhoneNumber = phoneNumber,
            GymId = gymId,
            ApplicationUserId = trainerUser.Id
        };

        _context.Trainers.Add(trainer);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Antrenör {name} başarıyla oluşturuldu. E-posta: {email}";
        return RedirectToAction("Index");
    }

    [Authorize(Roles = Roles.RoleAdmin)]
    public async Task<IActionResult> Edit(int id)
    {
        var trainer = await _context.Trainers.FindAsync(id);
        if (trainer == null)
            return NotFound();

        var gyms = await _context.Gyms.ToListAsync();
        ViewBag.Gyms = gyms;
        return View(trainer);
    }

    [HttpPost]
    [Authorize(Roles = Roles.RoleAdmin)]
    public async Task<IActionResult> Edit(int id, string name, string phoneNumber, int gymId)
    {
        var trainer = await _context.Trainers.FindAsync(id);
        if (trainer == null)
            return NotFound();

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(phoneNumber) || gymId <= 0)
        {
            ModelState.AddModelError(string.Empty, "Tüm alanlar gereklidir.");
            var gyms = await _context.Gyms.ToListAsync();
            ViewBag.Gyms = gyms;
            return View(trainer);
        }

        trainer.Name = name;
        trainer.PhoneNumber = phoneNumber;
        trainer.GymId = gymId;

        _context.Update(trainer);
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    [Authorize(Roles = Roles.RoleAdmin)]
    [Authorize(Roles = Roles.RoleAdmin)]
    public async Task<IActionResult> Delete(int id)
    {
        var trainer = await _context.Trainers
            .Include(t => t.TrainerServices)
            .FirstOrDefaultAsync(t => t.Id == id);

        // if (trainer == null)
        //     return NotFound();

        // Check if trainer has any accepted appointments
        var acceptedAppointments = await _context.Appointments
            .Where(a => a.TrainerId == id && a.Status == 1)
            .ToListAsync();

        ViewBag.HasAcceptedAppointments = acceptedAppointments.Count > 0;
        ViewBag.AcceptedAppointmentCount = acceptedAppointments.Count;

        return View(trainer);
        // await DeleteConfirmed(id);
    }

    [HttpGet]
    [Route("api/trainer/{id}/appointments-summary")]
    public async Task<IActionResult> GetAppointmentsSummary(int id)
    {
        var accepted = await _context.Appointments
            .Where(a => a.TrainerId == id && a.Status == 1)
            .Include(a => a.User)
            .Include(a => a.ScheduleSlot)
            .Select(a => new
            {
                id = a.Id,
                userName = a.User != null ? a.User.UserName : "Bilinmeyen",
                date = a.AppointmentDate.ToString("dd.MM.yyyy"),
                time = a.ScheduleSlot != null ? (a.ScheduleSlot.Hour + ":00 - " + (a.ScheduleSlot.Hour + 1) + ":00") : "",
                service = string.Join(", ", a.ScheduleSlot!.ScheduleSlotServices!.Select(ss => ss.Service!.Name))
            })
            .ToListAsync();

        var awaiting = await _context.Appointments
            .Where(a => a.TrainerId == id && a.Status == 0)
            .Include(a => a.User)
            .Include(a => a.ScheduleSlot)
            .Select(a => new
            {
                id = a.Id,
                userName = a.User != null ? a.User.UserName : "Bilinmeyen",
                date = a.AppointmentDate.ToString("dd.MM.yyyy"),
                time = a.ScheduleSlot != null ? (a.ScheduleSlot.Hour + ":00 - " + (a.ScheduleSlot.Hour + 1) + ":00") : "",
                service = string.Join(", ", a.ScheduleSlot!.ScheduleSlotServices!.Select(ss => ss.Service!.Name))
            })
            .ToListAsync();

        return Ok(new { accepted, awaiting });
    }

    [HttpPost]
    [Route("api/trainer/{id}/delete")]
    [Authorize(Roles = Roles.RoleAdmin)]
    public async Task<IActionResult> DeleteTrainerApi(int id)
    {
        // Check accepted appointments
        var acceptedCount = await _context.Appointments.CountAsync(a => a.TrainerId == id && a.Status == 1);
        if (acceptedCount > 0)
        {
            return BadRequest(new { success = false, message = "Kabul edilmiş randevular bulunduğu için antrenör silinemez." });
        }

        // Perform deletion logic (reuse existing DeleteConfirmed behavior)
        var trainer = await _context.Trainers
            .Include(t => t.ScheduleSlots)
            .Include(t => t.TrainerServices)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (trainer == null)
            return NotFound(new { success = false, message = "Antrenör bulunamadı." });

        // Get all appointments for this trainer
        var appointments = await _context.Appointments
            .Include(a => a.User)
            .Include(a => a.ScheduleSlot)
            .ThenInclude(s => s!.ScheduleSlotServices!)
            .ThenInclude(ss => ss.Service)
            .Where(a => a.TrainerId == id)
            .ToListAsync();

        // Cancel accepted (none should exist here) and reject awaiting ones
        foreach (var appointment in appointments)
        {
            if (appointment.Status == 0) // Awaiting
            {
                appointment.Status = 2; // Reject
                appointment.UpdatedAt = DateTime.Now;
                _context.Appointments.Update(appointment);

                // Notify user
                var serviceNames = string.Join(", ", appointment.ScheduleSlot?.ScheduleSlotServices?.Select(ss => ss.Service?.Name) ?? new List<string>());
                var timeStr = $"{appointment.ScheduleSlot?.Hour}:00 - {(appointment.ScheduleSlot?.Hour ?? 0) + 1}:00";
                var appointmentDateStr = appointment.AppointmentDate.ToString("dd.MM.yyyy");
                var notificationTitle = "Randevu Reddedildi";
                var notificationDescription = $"Randevu reddedildi. Eğitmen: {trainer.Name}\nTarih: {appointmentDateStr}, Saat: {timeStr}\nHizmet: {serviceNames}";
                if (!string.IsNullOrEmpty(appointment.UserId))
                {
                    await _notificationService.CreateNotificationAsync(appointment.UserId, notificationTitle, notificationDescription);
                }
            }
        }

        // Delete associated schedule slots
        _context.ScheduleSlots.RemoveRange(trainer.ScheduleSlots ?? new List<ScheduleSlot>());

        // Delete associated trainer services
        _context.TrainerServices.RemoveRange(trainer.TrainerServices ?? new List<TrainerService>());

        // Delete the trainer
        _context.Trainers.Remove(trainer);

        // Delete the associated user
        if (!string.IsNullOrEmpty(trainer.ApplicationUserId))
        {
            var user = await _userManager.FindByIdAsync(trainer.ApplicationUserId);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = $"Antrenör {trainer.Name} silindi." });
    }

    [HttpPost]
    [Authorize(Roles = Roles.RoleAdmin)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var trainer = await _context.Trainers
            .Include(t => t.ScheduleSlots)
            .Include(t => t.TrainerServices)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (trainer == null)
            return NotFound();

        // Get all appointments for this trainer
        var appointments = await _context.Appointments
            .Include(a => a.User)
            .Include(a => a.ScheduleSlot)
            .ThenInclude(s => s!.ScheduleSlotServices!)
            .ThenInclude(ss => ss.Service)
            .Where(a => a.TrainerId == id)
            .ToListAsync();

        // Cancel accepted appointments and reject awaiting ones
        foreach (var appointment in appointments)
        {
            if (appointment.Status == 0 || appointment.Status == 1) // Awaiting or Accepted
            {
                int oldStatus = appointment.Status;
                appointment.Status = (oldStatus == 0) ? 2 : 3; // Reject awaiting, cancel accepted
                appointment.UpdatedAt = DateTime.Now;
                _context.Appointments.Update(appointment);

                // Notify user about the status change
                var serviceNames = string.Join(", ", appointment.ScheduleSlot?.ScheduleSlotServices?.Select(ss => ss.Service?.Name) ?? new List<string>());
                var timeStr = $"{appointment.ScheduleSlot?.Hour}:00 - {(appointment.ScheduleSlot?.Hour ?? 0) + 1}:00";
                var appointmentDateStr = appointment.AppointmentDate.ToString("dd.MM.yyyy");
                string notificationTitle;
                string notificationDescription;

                if (oldStatus == 0) // Was awaiting
                {
                    notificationTitle = "Randevu Reddedildi";
                    notificationDescription = $"Randevu reddedildi. Eğitmen: {trainer.Name}\nTarih: {appointmentDateStr}, Saat: {timeStr}\nHizmet: {serviceNames}";
                }
                else // Was accepted
                {
                    notificationTitle = "Randevu İptal Edildi";
                    notificationDescription = $"Randevu iptal edildi. Eğitmen: {trainer.Name}\nTarih: {appointmentDateStr}, Saat: {timeStr}\nHizmet: {serviceNames}";
                }

                if (!string.IsNullOrEmpty(appointment.UserId))
                {
                    await _notificationService.CreateNotificationAsync(appointment.UserId, notificationTitle, notificationDescription);
                }
            }
        }

        // Delete associated schedule slots
        _context.ScheduleSlots.RemoveRange(trainer.ScheduleSlots ?? new List<ScheduleSlot>());

        // Delete associated trainer services
        _context.TrainerServices.RemoveRange(trainer.TrainerServices ?? new List<TrainerService>());

        // Delete the trainer
        _context.Trainers.Remove(trainer);

        // Delete the associated user
        if (!string.IsNullOrEmpty(trainer.ApplicationUserId))
        {
            var user = await _userManager.FindByIdAsync(trainer.ApplicationUserId);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Antrenör {trainer.Name} başarıyla silindi.";
        return RedirectToAction("Index");
    }

    [Authorize(Roles = Roles.RoleTrainer)]
    public async Task<IActionResult> MyServices()
    {
        var user = await _userManager.GetUserAsync(User);
        var trainer = await _context.Trainers
            .Include(t => t.TrainerServices)
            .ThenInclude(ts => ts.Service)
            .FirstOrDefaultAsync(t => t.ApplicationUserId == user!.Id);

        if (trainer == null)
            return RedirectToAction("Index", "Home");

        return View(trainer);
    }

    [HttpPost]
    [Authorize(Roles = Roles.RoleTrainer)]
    public async Task<IActionResult> UpdateServicePrice(int serviceId, decimal price)
    {
        var user = await _userManager.GetUserAsync(User);
        var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.ApplicationUserId == user!.Id);
        if (trainer == null)
            return BadRequest(new { success = false, message = "Antrenör bulunamadı." });

        var trainerService = await _context.TrainerServices.FirstOrDefaultAsync(ts => ts.TrainerId == trainer.Id && ts.ServiceId == serviceId);
        if (trainerService == null)
            return BadRequest(new { success = false, message = "Hizmet bulunamadı." });

        trainerService.Price = price;
        _context.TrainerServices.Update(trainerService);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Fiyat güncellendi." });
    }

    [Authorize(Roles = Roles.RoleTrainer)]
    public async Task<IActionResult> AddService(int serviceId)
    {
        var user = await _userManager.GetUserAsync(User);
        var trainer = await _context.Trainers
            .FirstOrDefaultAsync(t => t.ApplicationUserId == user!.Id);

        if (trainer == null)
            return RedirectToAction("Index", "Home");

        var existingService = await _context.TrainerServices
            .FirstOrDefaultAsync(ts => ts.TrainerId == trainer.Id && ts.ServiceId == serviceId);

        if (existingService == null)
        {
            var trainerService = new TrainerService
            {
                TrainerId = trainer.Id,
                ServiceId = serviceId
            };
            _context.TrainerServices.Add(trainerService);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("MyServices");
    }

    [Authorize(Roles = Roles.RoleTrainer)]
    public async Task<IActionResult> RemoveService(int serviceId)
    {
        var user = await _userManager.GetUserAsync(User);
        var trainer = await _context.Trainers
            .FirstOrDefaultAsync(t => t.ApplicationUserId == user!.Id);

        if (trainer == null)
            return RedirectToAction("Index", "Home");

        var trainerService = await _context.TrainerServices
            .FirstOrDefaultAsync(ts => ts.TrainerId == trainer.Id && ts.ServiceId == serviceId);

        if (trainerService != null)
        {
            _context.TrainerServices.Remove(trainerService);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("MyServices");
    }

    [Authorize(Roles = Roles.RoleTrainer)]
    public async Task<IActionResult> Appointments()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Index", "Home");

        var trainer = await _context.Trainers
            .FirstOrDefaultAsync(t => t.ApplicationUserId == user.Id);
        if (trainer == null)
            return RedirectToAction("Index", "Home");

        var appointments = await _context.Appointments
            .Where(a => a.TrainerId == trainer.Id)
            .Include(a => a.User)
            .Include(a => a.ScheduleSlot)
            .ThenInclude(s => s!.ScheduleSlotServices!)
            .ThenInclude(ss => ss.Service)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();

        return View(appointments);
    }

    [HttpPost]
    [Authorize(Roles = Roles.RoleTrainer)]
    public async Task<IActionResult> AcceptAppointment(int id)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Trainer)
            .Include(a => a.User)
            .Include(a => a.ScheduleSlot)
            .ThenInclude(s => s!.ScheduleSlotServices!)
            .ThenInclude(ss => ss.Service)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null)
            return NotFound();

        var user = await _userManager.GetUserAsync(User);
        var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.ApplicationUserId == user!.Id);

        if (trainer?.Id != appointment.TrainerId)
            return Forbid();

        appointment.Status = 1; // Accepted
        appointment.UpdatedAt = DateTime.Now;
        _context.Appointments.Update(appointment);

        // Auto-reject other awaiting appointments at the same time and date
        var otherAppointments = await _context.Appointments
            .Where(a => a.ScheduleSlotId == appointment.ScheduleSlotId &&
                        a.AppointmentDate == appointment.AppointmentDate &&
                        a.Status == 0 && // Only awaiting
                        a.Id != appointment.Id)
            .ToListAsync();

        foreach (var other in otherAppointments)
        {
            other.Status = 2; // Rejected (auto-rejected due to conflict)
            other.UpdatedAt = DateTime.Now;
            _context.Appointments.Update(other);

            // Notify the other user
            var serviceNames = string.Join(", ", appointment.ScheduleSlot?.ScheduleSlotServices?.Select(ss => ss.Service?.Name) ?? new List<string>());
            var timeStr = $"{appointment.ScheduleSlot?.Hour}:00 - {(appointment.ScheduleSlot?.Hour ?? 0) + 1}:00";
            var description = $"Randevu otomatik olarak reddedildi (aynı saatte başka bir randevu kabul edildi). Eğitmen: {appointment.Trainer?.Name}, Saat: {timeStr}";
            if (!string.IsNullOrEmpty(other.UserId))
            {
                await _notificationService.CreateNotificationAsync(other.UserId, "Randevu Reddedildi", description);
            }
        }

        await _context.SaveChangesAsync();

        // Notify user (the accepted one)
        var serviceNames_main = string.Join(", ", appointment.ScheduleSlot?.ScheduleSlotServices?.Select(ss => ss.Service?.Name) ?? new List<string>());
        var timeStr_main = $"{appointment.ScheduleSlot?.Hour}:00 - {(appointment.ScheduleSlot?.Hour ?? 0) + 1}:00";
        var description_main = $"Randevu kabul edildi. Eğitmen: {appointment.Trainer?.Name}, Saat: {timeStr_main}, Hizmet: {serviceNames_main}";
        await _notificationService.CreateNotificationAsync(appointment.UserId, "Randevu Kabul Edildi", description_main);

        return RedirectToAction("Appointments");
    }

    [HttpPost]
    [Authorize(Roles = Roles.RoleTrainer)]
    public async Task<IActionResult> RejectAppointment(int id)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Trainer)
            .Include(a => a.User)
            .Include(a => a.ScheduleSlot)
            .ThenInclude(s => s!.ScheduleSlotServices!)
            .ThenInclude(ss => ss.Service)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null)
            return NotFound();

        var user = await _userManager.GetUserAsync(User);
        var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.ApplicationUserId == user!.Id);

        if (trainer?.Id != appointment.TrainerId)
            return Forbid();

        appointment.Status = 2; // Rejected
        appointment.UpdatedAt = DateTime.Now;
        _context.Appointments.Update(appointment);
        await _context.SaveChangesAsync();

        // Notify user
        var serviceNames = string.Join(", ", appointment.ScheduleSlot?.ScheduleSlotServices?.Select(ss => ss.Service?.Name) ?? new List<string>());
        var timeStr = $"{appointment.ScheduleSlot?.Hour}:00 - {(appointment.ScheduleSlot?.Hour ?? 0) + 1}:00";
        var description = $"Randevu reddedildi. Eğitmen: {appointment.Trainer?.Name}, Saat: {timeStr}, Hizmet: {serviceNames}";
        if (!string.IsNullOrEmpty(appointment.UserId))
        {
            await _notificationService.CreateNotificationAsync(appointment.UserId, "Randevu Reddedildi", description);
        }

        return RedirectToAction("Appointments");
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CancelAppointment(int id)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Trainer)
            .Include(a => a.User)
            .Include(a => a.ScheduleSlot)
            .ThenInclude(s => s!.ScheduleSlotServices!)
            .ThenInclude(ss => ss.Service)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null)
            return NotFound();

        var user = await _userManager.GetUserAsync(User);
        var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.ApplicationUserId == user!.Id);

        if (trainer?.Id != appointment.TrainerId)
            return Forbid();

        // Only allow cancellation of accepted appointments
        if (appointment.Status != 1)
            return BadRequest(new { success = false, message = "Sadece kabul edilen randevular iptal edilebilir." });

        appointment.Status = 3; // Cancelled
        appointment.UpdatedAt = DateTime.Now;
        _context.Appointments.Update(appointment);
        await _context.SaveChangesAsync();

        // Notify user about cancellation
        var serviceNames = string.Join(", ", appointment.ScheduleSlot?.ScheduleSlotServices?.Select(ss => ss.Service?.Name) ?? new List<string>());
        var timeStr = $"{appointment.ScheduleSlot?.Hour}:00 - {(appointment.ScheduleSlot?.Hour ?? 0) + 1}:00";
        var appointmentDateStr = appointment.AppointmentDate.ToString("dd.MM.yyyy");
        var description = $"Randevu iptal edildi. Eğitmen: {appointment.Trainer?.Name}\nTarih: {appointmentDateStr}, Saat: {timeStr}\nHizmet: {serviceNames}";
        if (!string.IsNullOrEmpty(appointment.UserId))
        {
            await _notificationService.CreateNotificationAsync(appointment.UserId, "Randevu İptal Edildi", description);
        }

        return RedirectToAction("Appointments");
    }

}
