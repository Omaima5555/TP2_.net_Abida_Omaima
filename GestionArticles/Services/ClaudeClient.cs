using System.Net.Http.Json;
using System.Text.Json;

namespace GestionArticles.Services
{
    public class ClaudeClient
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _cfg;

        public ClaudeClient(HttpClient http, IConfiguration cfg)
        {
            _http = http;
            _cfg = cfg;
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_cfg["Claude:ApiKey"]);

        // Retourne null en cas d'erreur => le chatbot bascule sur les règles
        public async Task<string?> CompleteAsync(string system, List<ChatTurn> turns)
        {
            var body = new
            {
                model = _cfg["Claude:Model"] ?? "claude-haiku-5-5",
                max_tokens = 500,
                system,
                messages = turns.Select(t => new { role = t.Role, content = t.Content })
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            req.Headers.Add("x-api-key", _cfg["Claude:ApiKey"]);
            req.Headers.Add("anthropic-version", "2023-06-01");
            req.Content = JsonContent.Create(body);

            using var res = await _http.SendAsync(req);
            if (!res.IsSuccessStatusCode) return null;

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            foreach (var block in doc.RootElement.GetProperty("content").EnumerateArray())
            {
                if (block.GetProperty("type").GetString() == "text")
                    return block.GetProperty("text").GetString();
            }
            return null;
        }
    }
}