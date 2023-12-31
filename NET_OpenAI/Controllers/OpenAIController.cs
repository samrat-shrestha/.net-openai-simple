using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NET_OpenAI.Models;

namespace NET_OpenAI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OpenAIController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly OpenAIConfig _openAIConfig;
        private readonly ChatContext _context;
        private readonly ILogger<OpenAIController> _logger;

        public OpenAIController(IHttpClientFactory httpClientFactory, IOptions<OpenAIConfig> openAIConfig, ChatContext context, ILogger<OpenAIController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _openAIConfig = openAIConfig.Value;
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] UserMessageRequest userMessageRequest)
        {
            if (userMessageRequest == null || string.IsNullOrEmpty(userMessageRequest.Message))
            {
                _logger.LogError("Message cannot be empty.");
                return BadRequest("Invalid request. Message is required.");
            }

            try
            {
                if (!_context.ChatMessages.Any())
                {
                    _context.ChatMessages.Add(new ChatMessage { Role = "system", Content = "You are a helpful assistant." });
                }
                _context.ChatMessages.Add(new ChatMessage { Role = "user", Content = userMessageRequest.Message });
                await _context.SaveChangesAsync();
                string chatbotResponse = await CallOpenAIChatbot();
                _context.ChatMessages.Add(new ChatMessage { Role = "assistant", Content = chatbotResponse });
                await _context.SaveChangesAsync();

                return Ok(new ChatbotResponse { Message = chatbotResponse });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, "Internal Server Error");
            }
        }

        private async Task<string> CallOpenAIChatbot()
        {
            using (var httpClient = _httpClientFactory.CreateClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_openAIConfig.ApiKey}");

                string openaiEndpoint = "https://api.openai.com/v1/chat/completions";

                var payload = new
                {
                    model = "gpt-3.5-turbo",
                    messages = _context.ChatMessages.Select(message => new { role = message.Role, content = message.Content }).ToList()
                };

                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");

                _logger.LogInformation("Calling OpenAI API with messages: {Messages}", payload.messages);

                var response = await httpClient.PostAsync(openaiEndpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var responseObject = System.Text.Json.JsonSerializer.Deserialize<OpenAIResponse>(responseBody);
                    var generatedMessage = responseObject?.choices?.FirstOrDefault()?.message?.content;

                    return generatedMessage ?? "No response from the chatbot.";
                }
                else
                {
                    return "Error communicating with the OpenAI API.";
                }
            }
        }
    }
}
