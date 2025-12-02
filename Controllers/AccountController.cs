using PowerUp.Models;
using PowerUp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace PowerUp.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context;

    public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
    }

    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ModelState.AddModelError(string.Empty, "Email ve şifre gereklidir.");
            return View();
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user != null && !string.IsNullOrEmpty(user.UserName))
        {
            var result = await _signInManager.PasswordSignInAsync(user.UserName, password, false, false);
            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }
        }

        ModelState.AddModelError(string.Empty, "Email veya şifre hatalıdır.");
        return View();
    }

    public IActionResult SignUp()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SignUp(string username, string email, string password, string confirmPassword)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ModelState.AddModelError(string.Empty, "Kullanıcı adı, email ve şifre gereklidir.");
            return View();
        }

        if (password != confirmPassword)
        {
            ModelState.AddModelError(string.Empty, "Şifreler eşleşmiyor.");
            return View();
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user != null)
        {
            ModelState.AddModelError(string.Empty, "Bu email adresi zaten kayıtlı.");
            return View();
        }

        var userByUsername = await _userManager.FindByNameAsync(username);
        if (userByUsername != null)
        {
            ModelState.AddModelError(string.Empty, "Bu kullanıcı adı zaten alınmış.");
            return View();
        }

        var newUser = new ApplicationUser { UserName = username, Email = email };
        var result = await _userManager.CreateAsync(newUser, password);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(newUser, "User");
            TempData["SignUpSuccess"] = true;
            TempData["NewUsername"] = username;
            return RedirectToAction("Login");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View();
    }

    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Login");

        // Check if user is a trainer
        var trainer = await _context.Trainers
            .Include(t => t.TrainerServices)
            .ThenInclude(ts => ts.Service)
            .Include(t => t.ScheduleSlots)
            .FirstOrDefaultAsync(t => t.ApplicationUserId == user.Id);

        ViewBag.Trainer = trainer;
        ViewBag.IsTrainer = trainer != null;

        if (trainer != null)
        {
            // Get available services
            var allServices = await _context.Services.ToListAsync();
            ViewBag.AllServices = allServices;
            ViewBag.AssignedServiceIds = trainer.TrainerServices.Select(ts => ts.ServiceId).ToList();
        }

        return View();
    }
}
