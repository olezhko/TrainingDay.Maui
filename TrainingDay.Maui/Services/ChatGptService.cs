using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TrainingDay.Maui.Extensions;

namespace TrainingDay.Maui.Services
{
    public class ChatGptService
    {
        private readonly HttpClient _httpClient;
        private const string ApiUrl = "https://api.openai.com/v1/chat/completions";
        private readonly string _apiKey = ConstantKeys.OpenAIKey;

        public ChatGptService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.DefaultRequestHeaders.Add("OpenAI-Organization", "org-BxqusOm83Qb55kCsUtO0Zvcf");
            _httpClient.DefaultRequestHeaders.Add("OpenAI-Project", "proj_QrYjNHbywkEW6kyUPEtaOwHs");
        }

        public async Task<string> GetChatGptResponseAsync(string userMessage)
        {
            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "user", content = userMessage }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(ApiUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                return string.Empty;
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        }
    }
}
