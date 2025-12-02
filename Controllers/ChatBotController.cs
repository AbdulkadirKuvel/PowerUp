using System.Diagnostics;
using PowerUp.Models;
using Microsoft.AspNetCore.Mvc;

namespace PowerUp.Controllers;

public class ChatBotController(ILogger<ChatBotController> logger) : Controller
{
    private readonly ILogger<ChatBotController> _logger = logger;

    public IActionResult Index()
    {
        return View();
    }
}
