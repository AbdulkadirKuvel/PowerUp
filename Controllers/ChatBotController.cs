using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes; // JSON işlemleri için gerekli
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PowerUp.Controllers;

[Authorize]
public class ChatBotController : Controller
{
    private readonly ILogger<ChatBotController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    // "gemini-flash-latest" takma adını kullanıyoruz.
    // Bu, Google'ın sizin için izin verdiği en güncel çalışan versiyonu otomatik seçer.
    private const string GeminiApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent";
    public ChatBotController(ILogger<ChatBotController> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [Authorize]
    // IFormFile ekledik: Artık resim de alabiliyoruz
    public async Task<IActionResult> SendMessage(string message, IFormFile? file)
    {
        if (string.IsNullOrWhiteSpace(message) && file == null)
            return BadRequest(new { success = false, message = "Mesaj veya resim göndermelisiniz." });

        try
        {
            // 1. API Anahtarını Bul
            string apiKey = await GetApiKeyAsync();
            if (string.IsNullOrEmpty(apiKey))
                return BadRequest(new { success = false, message = "API anahtarı bulunamadı." });

            // 2. Kullanıcı "Resim Oluştur" mu istiyor kontrol et
            // (Basit bir kontrol, bunu geliştirebilirsiniz)
            bool isImageGenerationRequest = message != null &&
                (message.ToLower().Contains("çiz") ||
                 message.ToLower().Contains("oluştur") ||
                 message.ToLower().Contains("nasıl görünürüm") ||
                 message.ToLower().Contains("resim"));

            string botResponse;
            string? generatedImageUrl = null;

            if (isImageGenerationRequest && file != null)
            {
                // SENARYO 1: Resim Yüklemiş + "Nasıl görünürüm?" diyor -> Resim Analizi + Yeni Resim Üretimi
                // Önce Gemini'ye resmi analiz ettirip İngilizce prompt yazdıracağız.
                var promptForImage = await CallGeminiApi(apiKey,
                    "Bu resimdeki kişiyi analiz et. Kullanıcı şunu sordu: '" + message + "'. " +
                    "Buna dayanarak, bu kişinin 3 ay sonraki halini (örneğin daha kaslı veya fit) tasvir eden " +
                    "kısa, net ve detaylı bir İngilizce 'Image Generation Prompt' (resim üretme komutu) yaz. " +
                    "Sadece promptu yaz, başka bir şey yazma.",
                    file);

                // Gelen prompt ile resim URL'si oluştur
                generatedImageUrl = $"https://image.pollinations.ai/prompt/{System.Net.WebUtility.UrlEncode(promptForImage)}";
                botResponse = "Vücut yapınızı analiz ettim ve ulaşabileceğiniz formu görselleştirdim:";
            }
            else if (isImageGenerationRequest && file == null)
            {
                // SENARYO 2: Sadece metin ile "Kaslı bir adam çiz" dedi.
                var promptForImage = await CallGeminiApi(apiKey,
                    $"Kullanıcı şu resmi istiyor: '{message}'. Bunu çizmek için İngilizce bir prompt yaz. Sadece promptu döndür.",
                    null);

                generatedImageUrl = $"https://image.pollinations.ai/prompt/{System.Net.WebUtility.UrlEncode(promptForImage)}";
                botResponse = "İstediğiniz görseli oluşturdum:";
            }
            else
            {
                // SENARYO 3: Normal Sohbet (Spor tavsiyesi, site bilgisi vb.)
                // System Prompt: Botun kimliğini tanımlıyoruz.
                string systemInstruction = "Sen PowerUp spor salonu zincirinin uzman AI asistanısın. " +
                                           "Kullanıcılara antrenman, beslenme ve PowerUp web sitesi kullanımı hakkında yardımcı olursun. " +
                                           "Site özellikleri: Antrenörlerden randevu alma, spor salonlarını listeleme. " +
                                           "Cevapların motive edici, kısa ama öz ve Türkçe olsun.";

                string fullMessage = systemInstruction + "\n\nKullanıcı: " + message;
                botResponse = await CallGeminiApi(apiKey, fullMessage, file);
            }

            return Ok(new { success = true, message = botResponse, imageUrl = generatedImageUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chatbot hatası");
            return BadRequest(new { success = false, message = "Üzgünüm, şu an yanıt veremiyorum. Hata: " + ex.Message });
        }
    }

    // Gemini API Çağrısı Yapan Yardımcı Metot
    private async Task<string> CallGeminiApi(string apiKey, string text, IFormFile? imageFile)
    {
        using var client = _httpClientFactory.CreateClient();

        // JSON Yapısını Dinamik Oluşturma
        var parts = new List<object>
        {
            new { text = text }
        };

        // Eğer resim varsa Base64'e çevirip ekle
        if (imageFile != null)
        {
            using var ms = new MemoryStream();
            await imageFile.CopyToAsync(ms);
            byte[] fileBytes = ms.ToArray();
            string base64Data = Convert.ToBase64String(fileBytes);

            parts.Add(new
            {
                inline_data = new
                {
                    mime_type = imageFile.ContentType, // örn: image/jpeg
                    data = base64Data
                }
            });
        }

        var requestBody = new
        {
            contents = new[]
            {
                new { parts = parts }
            }
        };

        string jsonContent = JsonSerializer.Serialize(requestBody);
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await client.PostAsync($"{GeminiApiUrl}?key={apiKey}", httpContent);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Gemini API Hatası: {response.StatusCode} - {errorContent}");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();

        // JSON'dan cevabı ayıklama
        try
        {
            var jsonNode = JsonNode.Parse(jsonResponse);
            string? resultText = jsonNode?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
            return resultText ?? "Anlamsız yanıt.";
        }
        catch
        {
            return "API yanıtı işlenemedi.";
        }
    }

    // API Key Okuma (Dosya veya Environment)
    private async Task<string?> GetApiKeyAsync()
    {
        try
        {
            var keyFile = Path.Combine(Directory.GetCurrentDirectory(), "Data", "chatbotAPIKey.txt");
            if (System.IO.File.Exists(keyFile))
            {
                var lines = await System.IO.File.ReadAllLinesAsync(keyFile);
                var key = lines.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Trim();
                if (!string.IsNullOrEmpty(key)) return key;
            }
        }
        catch { /* ignored */ }

        return Environment.GetEnvironmentVariable("OPENAI_API_KEY") ??
               HttpContext.RequestServices.GetService<IConfiguration>()?.GetValue<string>("Gemini:ApiKey");
    }

    [HttpGet]
    public async Task<IActionResult> TestConnection()
    {
        try
        {
            string apiKey = await GetApiKeyAsync();
            if (string.IsNullOrEmpty(apiKey)) return Content("API Key dosyadan okunamadı!");

            using var client = _httpClientFactory.CreateClient();
            // Google'a "Elimdeki bu anahtarla hangi modelleri kullanabilirim?" diye soruyoruz.
            var response = await client.GetAsync($"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}");

            var jsonString = await response.Content.ReadAsStringAsync();

            // Ham cevabı ekrana basalım
            return Content(jsonString, "application/json");
        }
        catch (Exception ex)
        {
            return Content("Hata: " + ex.Message);
        }
    }
}