using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PowerUp.Data;
using PowerUp.Models;
using PowerUp.Services;

namespace PowerUp.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotificationController : ControllerBase
{
    private readonly NotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationController(NotificationService notificationService, UserManager<ApplicationUser> userManager)
    {
        _notificationService = notificationService;
        _userManager = userManager;
    }

    [HttpGet("unread")]
    public async Task<IActionResult> GetUnreadNotifications()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var notifications = await _notificationService.GetUnreadNotificationsAsync(user.Id);
        return Ok(notifications);
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllNotifications()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var notifications = await _notificationService.GetAllNotificationsAsync(user.Id);
        return Ok(notifications);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var count = await _notificationService.GetUnreadCountAsync(user.Id);
        return Ok(new { count });
    }

    [HttpPost("{id}/mark-as-read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        await _notificationService.MarkAsReadAsync(id);
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        await _notificationService.DeleteNotificationAsync(id);
        return Ok();
    }
}
