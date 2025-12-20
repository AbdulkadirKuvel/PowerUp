using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerUp.Data;
using PowerUp.Models;
using PowerUp.Utility;

namespace PowerUp.Controllers;

[Authorize]
public class ScheduleController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : Controller
{
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    // Get trainer's schedule slots
    [Authorize(Roles = Roles.RoleTrainer)]
    public async Task<IActionResult> MySchedule()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Index", "Home");

        var trainer = await _context.Trainers
            .FirstOrDefaultAsync(t => t.ApplicationUserId == user.Id);

        if (trainer == null)
            return RedirectToAction("Index", "Home");

        var slots = await _context.ScheduleSlots
            .Where(s => s.TrainerId == trainer.Id)
            .Include(s => s.ScheduleSlotServices!)
            .ThenInclude(ss => ss.Service)
            .Include(s => s.Gym)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.Hour)
            .ToListAsync();

        // var acceptedAppointments = await _context.Appointments
        //     .Where(a => a.TrainerId == trainer.Id && a.Status == 1)
        //     .ToListAsync();

        // var waitingAppointments = await _context.Appointments
        //     .Where(a => a.TrainerId == trainer.Id && a.Status == 0)
        //     .ToListAsync();

        ViewBag.trainerId = trainer.Id;

        return View(slots);
    }

    // Create new schedule slot
    [Authorize(Roles = Roles.RoleTrainer)]
    public async Task<IActionResult> Create()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Index", "Home");

        var trainer = await _context.Trainers
            .Include(t => t.TrainerServices)
            .ThenInclude(ts => ts.Service)
            .Include(t => t.Gym)
            .FirstOrDefaultAsync(t => t.ApplicationUserId == user.Id);

        if (trainer == null)
            return RedirectToAction("Index", "Home");

        ViewBag.TrainerServices = trainer.TrainerServices ?? [];
        ViewBag.TrainerGym = trainer.Gym;
        ViewBag.GymId = trainer.GymId;
        return View();
    }
    // [HttpPost]
    // [Authorize(Roles = Roles.RoleTrainer)]
    // public async Task<IActionResult> Create(string slotType, int? dayOfWeek, int hour, int? gymId, int[] selectedServices)
    // {
    //     var user = await _userManager.GetUserAsync(User);

    //     // slotType: "specific" or "everyday"
    //     if (slotType != "specific" && slotType != "everyday")
    //     {
    //         ModelState.AddModelError("slotType", "Lütfen geçerli bir seçim yapınız.");
    //     }

    //     if (slotType == "specific" && (dayOfWeek == null || dayOfWeek < 0 || dayOfWeek > 6))
    //     {
    //         ModelState.AddModelError("dayOfWeek", "Lütfen geçerli bir gün seçiniz.");
    //     }

    //     if (hour < 0 || hour > 23)
    //     {
    //         ModelState.AddModelError("hour", "Lütfen geçerli bir saat seçiniz.");
    //     }

    //     if (gymId == null || gymId <= 0)
    //     {
    //         ModelState.AddModelError("gymId", "Lütfen spor merkezini seçiniz.");
    //     }

    //     if (selectedServices == null || selectedServices.Length == 0)
    //     {
    //         ModelState.AddModelError("selectedServices", "Lütfen en az bir hizmet seçiniz.");
    //     }

    //     if (!ModelState.IsValid)
    //     {
    //         var trainerForError = await _context.Trainers
    //             .Include(t => t.TrainerServices)
    //             .ThenInclude(ts => ts.Service)
    //             .Include(t => t.Gym)
    //             .FirstOrDefaultAsync(t => t.ApplicationUserId == user!.Id);

    //         ViewBag.TrainerServices = trainerForError?.TrainerServices ?? [];
    //         ViewBag.TrainerGym = trainerForError?.Gym;
    //         ViewBag.GymId = trainerForError?.GymId;
    //         return View();
    //     }

    //     if (user == null)
    //         return RedirectToAction("Index", "Home");

    //     var trainer = await _context.Trainers
    //         .Include(t => t.Gym)
    //         .FirstOrDefaultAsync(t => t.ApplicationUserId == user.Id);
    //     if (trainer == null)
    //         return RedirectToAction("Index", "Home");

    //     var daysToCreate = slotType == "everyday" ? [0, 1, 2, 3, 4, 5, 6] : new[] { dayOfWeek!.Value };

    //     foreach (var day in daysToCreate)
    //     {
    //         var slot = new ScheduleSlot
    //         {
    //             TrainerId = trainer.Id,
    //             GymId = gymId!.Value,
    //             DayOfWeek = day,
    //             Hour = hour,
    //             IsWeekly = false
    //         };

    //         _context.ScheduleSlots.Add(slot);
    //         await _context.SaveChangesAsync();

    //         // Add services to the schedule slot
    //         foreach (var serviceId in selectedServices!)
    //         {
    //             var slotService = new ScheduleSlotService
    //             {
    //                 ScheduleSlotId = slot.Id,
    //                 ServiceId = serviceId
    //             };
    //             _context.ScheduleSlotServices.Add(slotService);
    //         }
    //     }

    //     await _context.SaveChangesAsync();
    //     return RedirectToAction("MySchedule");
    // }

    [HttpPost]
    [Authorize(Roles = Roles.RoleTrainer)]
    public async Task<IActionResult> Create(string slotType, int? dayOfWeek, int hour, int? gymId, int[] selectedServices)
    {
        var user = await _userManager.GetUserAsync(User);

        // --- 1. Temel Validasyonlar ---
        if (slotType != "specific" && slotType != "everyday")
            ModelState.AddModelError("slotType", "Lütfen geçerli bir seçim yapınız.");

        if (slotType == "specific" && (dayOfWeek == null || dayOfWeek < 0 || dayOfWeek > 6))
            ModelState.AddModelError("dayOfWeek", "Lütfen geçerli bir gün seçiniz.");

        if (hour < 0 || hour > 23)
            ModelState.AddModelError("hour", "Lütfen geçerli bir saat seçiniz.");

        if (gymId == null || gymId <= 0)
            ModelState.AddModelError("gymId", "Lütfen spor merkezini seçiniz.");

        if (selectedServices == null || selectedServices.Length == 0)
            ModelState.AddModelError("selectedServices", "Lütfen en az bir hizmet seçiniz.");

        // --- 2. SAAT KONTROLÜ (YENİ EKLENEN KISIM) ---
        if (ModelState.IsValid) // Temel formatlar doğruysa mantıksal kontrolü yap
        {
            var targetGym = await _context.Gyms.FindAsync(gymId);
            if (targetGym != null)
            {
                // Seçilen saat (TimeSpan formatında)
                TimeSpan selectedStart = TimeSpan.FromHours(hour);
                TimeSpan selectedEnd = TimeSpan.FromHours(hour + 1); // 1 saatlik seans varsayımı

                // Kontrol: Başlangıç açılıştan önce mi? VEYA Bitiş kapanıştan sonra mı?
                if (selectedStart < targetGym.OpeningTime || selectedEnd > targetGym.ClosingTime)
                {
                    string openStr = targetGym.OpeningTime.ToString(@"hh\:mm");
                    string closeStr = targetGym.ClosingTime.ToString(@"hh\:mm");

                    ModelState.AddModelError("hour",
                        $"Seçilen saat ({hour}:00 - {hour + 1}:00), salonun çalışma saatleri ({openStr} - {closeStr}) dışındadır.");
                }
            }
        }

        // --- 3. Hata Varsa View'i Tekrar Doldur ---
        if (!ModelState.IsValid)
        {
            // Kullanıcıyı ve Trainer detaylarını tekrar çekiyoruz (Dropdownlar için)
            var trainerForError = await _context.Trainers
                .Include(t => t.TrainerServices)
                .ThenInclude(ts => ts.Service)
                .Include(t => t.Gym)
                .FirstOrDefaultAsync(t => t.ApplicationUserId == user!.Id);

            ViewBag.TrainerServices = trainerForError?.TrainerServices ?? [];
            ViewBag.TrainerGym = trainerForError?.Gym;
            ViewBag.GymId = trainerForError?.GymId;

            // Eğer veritabanından Gym çekildiyse hata mesajını view'da göstermek için geri yolladık
            return View();
        }

        if (user == null) return RedirectToAction("Index", "Home");

        var trainer = await _context.Trainers
            .Include(t => t.Gym)
            .FirstOrDefaultAsync(t => t.ApplicationUserId == user.Id);

        if (trainer == null) return RedirectToAction("Index", "Home");

        // --- 4. Kayıt İşlemi ---
        var daysToCreate = slotType == "everyday" ? [0, 1, 2, 3, 4, 5, 6] : new[] { dayOfWeek!.Value };

        foreach (var day in daysToCreate)
        {
            // Çakışma kontrolü (Opsiyonel ama önerilir: Aynı güne ve saate zaten kayıt var mı?)
            bool isDuplicate = await _context.ScheduleSlots.AnyAsync(s =>
                s.TrainerId == trainer.Id &&
                s.DayOfWeek == day &&
                s.Hour == hour);

            if (!isDuplicate)
            {
                var slot = new ScheduleSlot
                {
                    TrainerId = trainer.Id,
                    GymId = gymId!.Value,
                    DayOfWeek = day,
                    Hour = hour,
                    IsWeekly = false // Veya formdan geliyorsa formdan al
                };

                _context.ScheduleSlots.Add(slot);
                await _context.SaveChangesAsync(); // Slot Id oluşması için save şart

                foreach (var serviceId in selectedServices!)
                {
                    var slotService = new ScheduleSlotService
                    {
                        ScheduleSlotId = slot.Id,
                        ServiceId = serviceId
                    };
                    _context.ScheduleSlotServices.Add(slotService);
                }
            }
        }

        await _context.SaveChangesAsync();
        return RedirectToAction("MySchedule");
    }

    [Authorize(Roles = Roles.RoleTrainer)]
    public async Task<IActionResult> Edit(int id)
    {
        var schedule = await _context.ScheduleSlots
            .Include(s => s.ScheduleSlotServices)
            .Include(s => s.Gym)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (schedule == null)
            return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Index", "Home");

        var trainer = await _context.Trainers
            .Include(t => t.TrainerServices)
            .ThenInclude(ts => ts.Service)
            .Include(t => t.Gym)
            .FirstOrDefaultAsync(t => t.ApplicationUserId == user.Id);

        if (trainer?.Id != schedule.TrainerId)
            return Forbid();

        ViewBag.TrainerServices = trainer?.TrainerServices ?? new List<TrainerService>();
        ViewBag.TrainerGym = trainer?.Gym;
        ViewBag.GymId = trainer?.GymId;
        ViewBag.SelectedServiceIds = schedule.ScheduleSlotServices?.Select(s => s.ServiceId).ToList() ?? new List<int>();
        return View(schedule);
    }

    // [HttpPost]
    // [Authorize(Roles = Roles.RoleTrainer)]
    // public async Task<IActionResult> Edit(int id, int dayOfWeek, int hour, int? gymId, bool isWeekly, int[] selectedServices)
    // {
    //     var schedule = await _context.ScheduleSlots
    //         .Include(s => s.ScheduleSlotServices)
    //         .FirstOrDefaultAsync(s => s.Id == id);
    //     if (schedule == null)
    //         return NotFound();

    //     if (selectedServices == null || selectedServices.Length == 0)
    //     {
    //         ModelState.AddModelError("", "Lütfen en az bir hizmet seçiniz.");
    //         return View(schedule);
    //     }

    //     if (hour < 0 || hour > 23)
    //     {
    //         ModelState.AddModelError("", "Lütfen geçerli bir saat seçiniz.");
    //         return View(schedule);
    //     }

    //     if (gymId == null || gymId <= 0)
    //     {
    //         ModelState.AddModelError("", "Lütfen spor merkezini seçiniz.");
    //         return View(schedule);
    //     }

    //     var user = await _userManager.GetUserAsync(User);
    //     if (user == null)
    //         return RedirectToAction("Index", "Home");

    //     var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.ApplicationUserId == user.Id);

    //     if (trainer?.Id != schedule.TrainerId)
    //         return Forbid();

    //     schedule.DayOfWeek = dayOfWeek;
    //     schedule.Hour = hour;
    //     schedule.GymId = gymId.Value;
    //     schedule.IsWeekly = isWeekly;
    //     schedule.UpdatedAt = DateTime.Now;

    //     // Update services
    //     if (schedule.ScheduleSlotServices != null && schedule.ScheduleSlotServices.Any())
    //     {
    //         _context.ScheduleSlotServices.RemoveRange(schedule.ScheduleSlotServices);
    //     }

    //     foreach (var serviceId in selectedServices)
    //     {
    //         var slotService = new ScheduleSlotService
    //         {
    //             ScheduleSlotId = schedule.Id,
    //             ServiceId = serviceId
    //         };
    //         _context.ScheduleSlotServices.Add(slotService);
    //     }

    //     _context.ScheduleSlots.Update(schedule);
    //     await _context.SaveChangesAsync();

    //     return RedirectToAction("MySchedule");
    // }

    [HttpPost]
    [Authorize(Roles = Roles.RoleTrainer)]
    public async Task<IActionResult> Edit(int id, int dayOfWeek, int hour, int? gymId, bool isWeekly, int[] selectedServices)
    {
        var schedule = await _context.ScheduleSlots
            .Include(s => s.ScheduleSlotServices)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule == null) return NotFound();

        // --- Validasyonlar ---
        if (selectedServices == null || selectedServices.Length == 0)
            ModelState.AddModelError("", "Lütfen en az bir hizmet seçiniz.");

        if (hour < 0 || hour > 23)
            ModelState.AddModelError("", "Lütfen geçerli bir saat seçiniz.");

        if (gymId == null || gymId <= 0)
            ModelState.AddModelError("", "Lütfen spor merkezini seçiniz.");

        // --- SAAT KONTROLÜ (YENİ) ---
        if (ModelState.IsValid)
        {
            var targetGym = await _context.Gyms.FindAsync(gymId);
            if (targetGym != null)
            {
                TimeSpan selectedStart = TimeSpan.FromHours(hour);
                TimeSpan selectedEnd = TimeSpan.FromHours(hour + 1);

                if (selectedStart < targetGym.OpeningTime || selectedEnd > targetGym.ClosingTime)
                {
                    string openStr = targetGym.OpeningTime.ToString(@"hh\:mm");
                    string closeStr = targetGym.ClosingTime.ToString(@"hh\:mm");
                    ModelState.AddModelError("", $"Seçilen saat, salonun çalışma saatleri ({openStr} - {closeStr}) dışındadır.");
                }
            }
        }

        // --- Hata Varsa Geri Dön ---
        if (!ModelState.IsValid)
        {
            // ViewBag verilerini tekrar doldurmalıyız, aksi takdirde edit sayfasındaki dropdownlar patlar.
            var userCheck = await _userManager.GetUserAsync(User);
            var trainerCheck = await _context.Trainers
                .Include(t => t.TrainerServices).ThenInclude(ts => ts.Service)
                .Include(t => t.Gym)
                .FirstOrDefaultAsync(t => t.ApplicationUserId == userCheck!.Id);

            ViewBag.TrainerServices = trainerCheck?.TrainerServices ?? new List<TrainerService>();
            ViewBag.TrainerGym = trainerCheck?.Gym;
            ViewBag.GymId = trainerCheck?.GymId;
            ViewBag.SelectedServiceIds = selectedServices?.ToList() ?? new List<int>(); // Formdan gelenleri seçili yap

            return View(schedule);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Index", "Home");

        var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.ApplicationUserId == user.Id);
        if (trainer?.Id != schedule.TrainerId) return Forbid();

        // --- Güncelleme ---
        schedule.DayOfWeek = dayOfWeek;
        schedule.Hour = hour;
        schedule.GymId = gymId!.Value;
        schedule.IsWeekly = isWeekly;
        schedule.UpdatedAt = DateTime.Now;

        // Hizmetleri güncelle
        if (schedule.ScheduleSlotServices != null)
        {
            _context.ScheduleSlotServices.RemoveRange(schedule.ScheduleSlotServices);
        }

        foreach (var serviceId in selectedServices!)
        {
            _context.ScheduleSlotServices.Add(new ScheduleSlotService
            {
                ScheduleSlotId = schedule.Id,
                ServiceId = serviceId
            });
        }

        _context.ScheduleSlots.Update(schedule);
        await _context.SaveChangesAsync();

        return RedirectToAction("MySchedule");
    }

    // Delete schedule slot
    [Authorize(Roles = Roles.RoleTrainer)]
    public async Task<IActionResult> Delete(int id)
    {
        var schedule = await _context.ScheduleSlots.FindAsync(id);
        if (schedule == null)
            return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Index", "Home");

        var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.ApplicationUserId == user.Id);

        if (trainer?.Id != schedule.TrainerId)
            return Forbid();

        // Check for accepted appointments
        var hasAccepted = await _context.Appointments.AnyAsync(a => a.ScheduleSlotId == id && a.Status == 1);
        var awaitingCount = await _context.Appointments.CountAsync(a => a.ScheduleSlotId == id && a.Status == 0);
        ViewBag.HasAcceptedAppointments = hasAccepted;
        ViewBag.AwaitingCount = awaitingCount;

        return View(schedule);
    }

    [HttpPost]
    [ActionName("Delete")]
    [Authorize(Roles = Roles.RoleTrainer)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var schedule = await _context.ScheduleSlots.FindAsync(id);
        if (schedule == null)
            return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Index", "Home");

        var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.ApplicationUserId == user.Id);

        if (trainer?.Id != schedule.TrainerId)
            return Forbid();

        // Check for accepted appointments - if any accepted appointments exist, block deletion and route to appointments
        var acceptedExists = await _context.Appointments.AnyAsync(a => a.ScheduleSlotId == id && a.Status == 1);
        if (acceptedExists)
        {
            TempData["Error"] = "Bu zaman diliminde kabul edilmiş randevular bulunmaktadır. Lütfen randevular sayfasına giderek öncelikle bu randevuları yönetin.";
            return RedirectToAction("Appointments", "Trainer");
        }

        // For awaiting appointments, mark them as rejected and notify users
        var awaiting = await _context.Appointments.Where(a => a.ScheduleSlotId == id && a.Status == 0).ToListAsync();
        foreach (var app in awaiting)
        {
            app.Status = 2; // rejected
            app.UpdatedAt = DateTime.Now;

            // Create notification for user
            var notif = new Notification
            {
                UserId = app.UserId,
                Subject = "Randevunuz iptal edildi",
                Description = $"Üzgünüz, {schedule.DayOfWeek} {schedule.Hour}:00 saatindeki randevunuz iptal edildi.",
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notif);
        }

        _context.ScheduleSlots.Remove(schedule);
        await _context.SaveChangesAsync();

        return RedirectToAction("MySchedule");
    }
}
