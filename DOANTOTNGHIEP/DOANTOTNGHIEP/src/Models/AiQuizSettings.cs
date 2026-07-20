namespace DOANTOTNGHIEP.src.Models
{
    /// <summary>
    /// Configuration settings for the AI Assistant service.
    /// Used for both quiz generation and plan generation via OpenRouter API.
    /// </summary>
    public class AiAssistantSettings
    {
        public const string SectionName = "AiAssistantSettings";

        public string BaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "quizMaker";
        public int TimeoutSeconds { get; set; } = 30;
        public int MaxRetries { get; set; } = 3;
    }
}