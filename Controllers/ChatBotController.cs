using System.Diagnostics;
using PowerUp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace PowerUp.Controllers;

[Authorize]
public class ChatBotController(ILogger<ChatBotController> logger) : Controller
{
    private readonly ILogger<ChatBotController> _logger = logger;

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> SendMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return BadRequest(new { success = false, message = "Mesaj boş olamaz." });

        try
        {
            // Prefer a file-based API key at Data/chatbotAPIKey.txt (first non-empty line), fall back to env/config
            string? apiKey = null;
            try
            {
                var keyFile = Path.Combine(Directory.GetCurrentDirectory(), "Data", "chatbotAPIKey.txt");
                if (System.IO.File.Exists(keyFile))
                {
                    var lines = await System.IO.File.ReadAllLinesAsync(keyFile);
                    apiKey = lines.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Trim();
                }
            }
            catch
            {
                // Ignore file read errors; fallback below
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = System.Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? 
                        HttpContext.RequestServices.GetService<IConfiguration>()?.GetValue<string>("OpenAI:ApiKey");
            }

            if (string.IsNullOrEmpty(apiKey))
                return BadRequest(new { success = false, message = "API anahtarı yapılandırılmamış." });

            // For now, return a placeholder response
            string botResponse = await GetChatbotResponse(message, apiKey);

            return Ok(new { success = true, message = botResponse });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chatbot");
            return BadRequest(new { success = false, message = "Bir hata oluştu: " + ex.Message });
        }
    }

    private Task<string> GetChatbotResponse(string userMessage, string apiKey)
    {
        // What is this??
        // Placeholder responses - will be replaced with actual API calls
        var placeholderResponses = new[]
        {
            "Sorunuz hakkında daha fazla bilgi verebilir misiniz?",
            "PowerUp hizmetlerimiz hakkında yardımcı olmaktan memnuniyet duyarım.",
            "Antrenör bulma, randevu alma veya diğer hizmetler hakkında sorularınız mı var?",
            "Başka bir sorunuz var mı?",
            "Yardımcı olabileceğim başka bir konu?"
        };

        // Simple keyword-based responses for now
        if (userMessage.Contains("merhaba", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult("Merhaba! PowerUp'a hoşgeldiniz. Size nasıl yardımcı olabilirim?");
        
        if (userMessage.Contains("antrenör", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult("Antrenör bulma hakkında yardımcı olmaktan memnuniyet duyarım. Hangi tür antrenörlüğü aradığınızı söyleyebilir misiniz?");
        
        if (userMessage.Contains("randevu", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult("Randevu almak istiyorsanız, Antrenörler sayfasından bir eğitmeni seçip uygun zaman dilimini seçebilirsiniz.");
        
        if (userMessage.Contains("spor salonu", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult("Spor Salonları sayfasında çeşitli spor salonlarını görebilir ve tercih edebilirsiniz.");

        // Return random placeholder response
        return Task.FromResult(placeholderResponses[new Random().Next(placeholderResponses.Length)]);
    }
}

