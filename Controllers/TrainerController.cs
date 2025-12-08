using PowerUp.Data;
using PowerUp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using PowerUp.Utility;

namespace PowerUp.Controllers;

public class TrainerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : Controller
{
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<IActionResult> Index()
    {
        var trainers = await _context.Trainers
        .Include(t => t.TrainerServices)
            .ThenInclude(ts => ts.Service)
        .ToListAsync();

        return View(trainers);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var trainerDetails = await _context.Trainers
            .Include(t => t.TrainerServices)
                .ThenInclude(ts => ts.Service)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (trainerDetails == null)
            return NotFound();
        else
            return View(trainerDetails);
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
        var trainer = await _context.Trainers
            .Include(t => t.ScheduleSlots)
            .FirstOrDefaultAsync(t => t.Id == trainerId);
        var scheduleSlot = await _context.ScheduleSlots.FindAsync(scheduleSlotId);

        if (trainer == null || scheduleSlot == null)
            return NotFound();

        // Validate the appointment date matches the schedule day of week
        if ((int)appointmentDate.DayOfWeek != scheduleSlot.DayOfWeek)
        {
            ModelState.AddModelError("", "Seçilen tarih seçilen günle eşleşmemektedir.");
            return View("TakeAppointment", trainer);
        }

        // Check if this slot is already booked for this date and time
        var existingAppointment = await _context.Appointments
            .FirstOrDefaultAsync(a => 
                a.ScheduleSlotId == scheduleSlotId && 
                a.AppointmentDate == appointmentDate);

        if (existingAppointment != null)
        {
            ModelState.AddModelError("", "Bu zaman dilimi, bu tarih için zaten dolu. Lütfen başka bir zaman seçiniz.");
            trainer = await _context.Trainers
                .Include(t => t.ScheduleSlots)!
                .ThenInclude(s => s.ScheduleSlotServices!)
                .ThenInclude(sss => sss.Service)
                .FirstOrDefaultAsync(t => t.Id == trainerId);
            return View("TakeAppointment", trainer);
        }

        var appointment = new Appointment
        {
            TrainerId = trainerId,
            UserId = user!.Id,
            ScheduleSlotId = scheduleSlotId,
            AppointmentDate = appointmentDate,
            AppointmentTime = TimeSpan.FromHours(scheduleSlot.Hour),
            Notes = notes ?? string.Empty
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Randevu başarıyla oluşturuldu!";
        return RedirectToAction("Detail", new { id = trainerId });
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
    public IActionResult Delete(int id)
    {
        return View(id);
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

}
