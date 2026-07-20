using DOANTOTNGHIEP.src.Data;
using DOANTOTNGHIEP.src.Models;
using DOANTOTNGHIEP.src.Models.DTOs;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DOANTOTNGHIEP.src.Services
{
    /// <summary>
    /// Implementation of IAiAssistantService that calls the OpenRouter API
    /// to generate both quiz questions and structured study plans based on
    /// user target details.
    /// Handles HTTP communication, request/response serialization,
    /// retries with exponential backoff, and error handling.
    /// </summary>
    public class AiAssistantService : IAiAssistantService
    {
        private readonly HttpClient _httpClient;
        private readonly IQuizRepository _quizRepository;
        private readonly AiAssistantSettings _settings;
        private readonly IPromptTemplateService _promptTemplateService;
        private readonly ILogger<AiAssistantService> _logger;

        // JSON serializer options configured for the OpenRouter API
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        /// <summary>
        /// Initializes a new instance of the AiAssistantService class.
        /// </summary>
        /// <param name="httpClient">HTTP client for OpenRouter API calls</param>
        /// <param name="quizRepository">Quiz repository for persisting generated quizzes</param>
        /// <param name="settings">AI assistant configuration via IOptions</param>
        /// <param name="promptTemplateService">Prompt template service for building AI prompts</param>
        /// <param name="logger">Logger instance</param>
        public AiAssistantService(
            HttpClient httpClient,
            IQuizRepository quizRepository,
            IOptions<AiAssistantSettings> settings,
            IPromptTemplateService promptTemplateService,
            ILogger<AiAssistantService> logger)
        {
            _httpClient = httpClient;
            _quizRepository = quizRepository;
            _settings = settings.Value;
            _promptTemplateService = promptTemplateService;
            _logger = logger;

            // Configure HttpClient base address and auth header once
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            // Note: Timeout is set via Program.cs AddHttpClient configuration (5 minutes)
            // to handle high-latency AI model responses
        }

        // ==================== Quiz Generation ====================

        /// <inheritdoc />
        public async Task<GenerateQuizzesFromTargetResponse> GenerateQuizzesFromTargetAsync(
            Target target,
            int count,
            string? category,
            string difficulty)
        {
            // Determine effective category: use override if provided, else derive from target title
            var effectiveCategory = category ?? "default";

            // Build the AI prompts using the template service — use effectiveCategory as lookup key
            var (systemPrompt, userPrompt) = _promptTemplateService.BuildPrompts(
                target, count, effectiveCategory, difficulty, effectiveCategory);

            // Call OpenRouter with retries
            var openRouterResponse = await CallOpenRouterWithRetriesAsync(
                systemPrompt, userPrompt, maxTokens: 2000);

            // Parse the AI response into Quiz entities
            var quizzes = ParseQuizzesFromResponse(openRouterResponse, effectiveCategory, difficulty);

            // Persist generated quizzes to database
            if (quizzes.Count > 0)
            {
                await _quizRepository.InsertManyAsync(quizzes);
            }

            // Build the DTO response
            return new GenerateQuizzesFromTargetResponse
            {
                Generated = quizzes.Count,
                Quizzes = quizzes.Select(MapToDto).ToList(),
                TargetUsed = new TargetUsedInfo
                {
                    Id = target.Id,
                    Title = target.Title,
                    Description = target.Description,
                    Status = target.Status
                }
            };
        }

        // ==================== Plan Generation ====================

        /// <inheritdoc />
        public async Task<PlanData> GeneratePlanAsync(Target target)
        {
            // Build the plan-specific prompts
            var (systemPrompt, userPrompt) = _promptTemplateService.BuildPlanPrompt(target);

            // Call OpenRouter with higher token limit for plan generation
            var response = await CallOpenRouterWithRetriesAsync(
                systemPrompt, userPrompt, maxTokens: 8000);

            // Parse the AI response into PlanData
            return ParsePlanFromResponse(response);
        }

        /// <inheritdoc />
        public async Task<PlanData> RegeneratePlanAsync(Target target, string feedback)
        {
            // Build the regenerate plan prompts with user feedback AND the existing plan as context
            var existingPlan = target.Plan;
            var (systemPrompt, userPrompt) = _promptTemplateService.BuildRegeneratePlanPrompt(
                target, feedback, existingPlan);

            // Call OpenRouter with higher token limit (plan regeneration needs even more context)
            var response = await CallOpenRouterWithRetriesAsync(
                systemPrompt, userPrompt, maxTokens: 8000);

            // Parse the AI response into PlanData
            return ParsePlanFromResponse(response);
        }

        // ==================== OpenRouter API Communication ====================

        /// <summary>
        /// Calls the OpenRouter API with retry logic using exponential backoff.
        /// </summary>
        /// <param name="systemPrompt">The system role prompt (sets AI behavior)</param>
        /// <param name="userPrompt">The user role prompt (specifies what to generate)</param>
        /// <param name="maxTokens">Maximum tokens for the response (higher for plan generation)</param>
        /// <returns>The parsed API response content</returns>
        private async Task<OpenRouterChatResponse> CallOpenRouterWithRetriesAsync(
            string systemPrompt, string userPrompt, int maxTokens)
        {
            var requestBody = new OpenRouterChatRequest
            {
                Model = $"{_settings.Model}",
                Messages = new List<OpenRouterMessage>
                {
                    new()
                    {
                        Role = "system",
                        Content = systemPrompt
                    },
                    new()
                    {
                        Role = "user",
                        Content = userPrompt
                    }
                },
                Temperature = 0.7,
                MaxTokens = maxTokens,
                ResponseFormat = new OpenRouterResponseFormat { Type = "json_object" }
            };

            var maxRetries = _settings.MaxRetries;
            var lastException = (Exception?)null;

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation(
                        "Calling OpenRouter API (attempt {Attempt}/{MaxRetries}) with {MaxTokens} max tokens",
                        attempt + 1, maxRetries + 1, maxTokens);

                    var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await _httpClient.PostAsync("/api/v1/chat/completions", content);

                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        var statusCode = (int)response.StatusCode;

                        // Don't retry client errors (4xx) except 429 (rate limit) and 408 (timeout)
                        if (statusCode is >= 400 and < 500 && statusCode != 429 && statusCode != 408)
                        {
                            throw new AiAssistantApiException(
                                $"OpenRouter API returned client error {statusCode}: {responseBody}",
                                response.StatusCode);
                        }

                        // Server errors (5xx) and 429/408 are retryable
                        throw new AiAssistantApiException(
                            $"OpenRouter API returned {statusCode}: {responseBody}",
                            response.StatusCode);
                    }

                    var result = JsonSerializer.Deserialize<OpenRouterChatResponse>(responseBody, _jsonOptions);

                    if (result?.Choices == null || result.Choices.Count == 0)
                    {
                        throw new AiAssistantApiException(
                            "OpenRouter response contained no choices",
                            System.Net.HttpStatusCode.InternalServerError);
                    }

                    _logger.LogInformation(
                        "OpenRouter API call successful on attempt {Attempt}",
                        attempt + 1);

                    return result;
                }
                catch (AiAssistantApiException)
                {
                    // Re-throw API exceptions directly — they already represent the failure
                    throw;
                }
                catch (HttpRequestException ex)
                {
                    lastException = ex;
                    _logger.LogWarning(ex,
                        "HTTP request to OpenRouter failed (attempt {Attempt}/{MaxRetries}): {Message}",
                        attempt + 1, maxRetries + 1, ex.Message);
                }
                catch (TaskCanceledException ex)
                {
                    lastException = ex;
                    _logger.LogWarning(ex,
                        "OpenRouter request timed out (attempt {Attempt}/{MaxRetries})",
                        attempt + 1, maxRetries + 1);
                }
                catch (JsonException ex)
                {
                    lastException = ex;
                    _logger.LogWarning(ex,
                        "Failed to deserialize OpenRouter response (attempt {Attempt}/{MaxRetries})",
                        attempt + 1, maxRetries + 1);
                }

                if (attempt < maxRetries)
                {
                    // Exponential backoff: 1s, 2s, 4s, ...
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    _logger.LogInformation("Retrying in {DelaySeconds}s...", delay.TotalSeconds);
                    await Task.Delay(delay);
                }
            }

            throw new AiAssistantServiceUnavailableException(
                $"OpenRouter API unavailable after {maxRetries + 1} attempts",
                lastException);
        }

        // ==================== Response Parsing ====================

        /// <summary>
        /// Parses the AI-generated response text into Quiz entities.
        /// Handles various response formats the AI might produce.
        /// </summary>
        private List<Quiz> ParseQuizzesFromResponse(
            OpenRouterChatResponse response,
            string category,
            string difficulty)
        {
            var rawContent = response.Choices[0].Message.Content?.Trim();

            if (string.IsNullOrWhiteSpace(rawContent))
            {
                _logger.LogWarning("OpenRouter returned empty content");
                return new List<Quiz>();
            }

            try
            {
                // Strip markdown code fences if present
                var jsonContent = StripMarkdownCodeFences(rawContent);

                List<OpenRouterGeneratedQuestion> questions;

                // Try parsing as a wrapper object first
                try
                {
                    using var doc = JsonDocument.Parse(jsonContent);
                    var root = doc.RootElement;

                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        questions = JsonSerializer.Deserialize<List<OpenRouterGeneratedQuestion>>(
                            jsonContent, _jsonOptions) ?? new List<OpenRouterGeneratedQuestion>();
                    }
                    else if (root.TryGetProperty("quizzes", out var quizzesProp))
                    {
                        questions = JsonSerializer.Deserialize<List<OpenRouterGeneratedQuestion>>(
                            quizzesProp.GetRawText(), _jsonOptions) ?? new List<OpenRouterGeneratedQuestion>();
                    }
                    else if (root.TryGetProperty("questions", out var questionsProp))
                    {
                        questions = JsonSerializer.Deserialize<List<OpenRouterGeneratedQuestion>>(
                            questionsProp.GetRawText(), _jsonOptions) ?? new List<OpenRouterGeneratedQuestion>();
                    }
                    else
                    {
                        var single = JsonSerializer.Deserialize<OpenRouterGeneratedQuestion>(
                            jsonContent, _jsonOptions);
                        questions = single != null
                            ? new List<OpenRouterGeneratedQuestion> { single }
                            : new List<OpenRouterGeneratedQuestion>();
                    }
                }
                catch (JsonException)
                {
                    questions = ExtractQuizArrayByScanning(rawContent);
                }

                // Map parsed questions to Quiz entities
                var quizzes = new List<Quiz>();
                foreach (var q in questions)
                {
                    if (string.IsNullOrWhiteSpace(q.Question) || q.Options == null || q.Options.Count < 2)
                    {
                        _logger.LogWarning("Skipping malformed question: {Question}", q.Question);
                        continue;
                    }

                    // Ensure exactly 4 options
                    while (q.Options.Count < 4)
                    {
                        q.Options.Add($"Option {q.Options.Count + 1}");
                    }
                    if (q.Options.Count > 4)
                    {
                        q.Options = q.Options.Take(4).ToList();
                    }

                    // Validate the correct answer is among the options
                    if (!string.IsNullOrWhiteSpace(q.CorrectAnswer) &&
                        !q.Options.Contains(q.CorrectAnswer, StringComparer.OrdinalIgnoreCase))
                    {
                        var letterMatch = q.CorrectAnswer.Trim().ToUpperInvariant() switch
                        {
                            "A" or "A." or "(A)" => q.Options[0],
                            "B" or "B." or "(B)" => q.Options[1],
                            "C" or "C." or "(C)" => q.Options[2],
                            "D" or "D." or "(D)" => q.Options[3],
                            _ => null
                        };

                        if (letterMatch != null)
                        {
                            q.CorrectAnswer = letterMatch;
                        }
                        else
                        {
                            _logger.LogWarning(
                                "CorrectAnswer '{Answer}' not found in options for question '{Question}'. Using first option.",
                                q.CorrectAnswer, q.Question);
                            q.CorrectAnswer = q.Options[0];
                        }
                    }

                    quizzes.Add(new Quiz
                    {
                        Question = q.Question,
                        Options = q.Options,
                        CorrectAnswer = q.CorrectAnswer ?? q.Options.FirstOrDefault() ?? string.Empty,
                        Category = category,
                        Difficulty = difficulty,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                _logger.LogInformation(
                    "Parsed {Count} valid quizzes from OpenRouter response", quizzes.Count);

                return quizzes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse OpenRouter response for quizzes: {Content}",
                    rawContent.Length > 500 ? rawContent[..500] + "..." : rawContent);
                return new List<Quiz>();
            }
        }

        /// <summary>
        /// Parses the AI-generated response text into a PlanData entity.
        /// Handles markdown code fences and various JSON nesting formats.
        /// </summary>
        private PlanData ParsePlanFromResponse(OpenRouterChatResponse response)
        {
            var rawContent = response.Choices[0].Message.Content?.Trim();

            if (string.IsNullOrWhiteSpace(rawContent))
            {
                _logger.LogWarning("OpenRouter returned empty content for plan generation");
                return BuildFallbackPlan();
            }

            try
            {
                var jsonContent = StripMarkdownCodeFences(rawContent);

                PlanData plan;

                using var doc = JsonDocument.Parse(jsonContent);
                var root = doc.RootElement;

                // Try to find the plan structure — the AI may wrap it in a "plan" key or return it directly
                JsonElement planElement;
                if (root.TryGetProperty("plan", out var planProp))
                {
                    planElement = planProp;
                }
                else
                {
                    planElement = root;
                }

                plan = JsonSerializer.Deserialize<PlanData>(
                    planElement.GetRawText(), _jsonOptions) ?? BuildFallbackPlan();

                // Set generation timestamp
                plan.GeneratedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Parsed plan: {MilestoneCount} milestones, {QuizCount} quizzes",
                    plan.Milestones.Count, plan.Quizzes.Count);

                return plan;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse OpenRouter plan response: {Content}",
                    rawContent.Length > 500 ? rawContent[..500] + "..." : rawContent);
                return BuildFallbackPlan();
            }
        }

        /// <summary>
        /// Returns a minimal fallback plan when parsing fails.
        /// </summary>
        private static PlanData BuildFallbackPlan()
        {
            return new PlanData
            {
                Summary = "Plan generation failed. Please try again.",
                GeneratedAt = DateTime.UtcNow,
                Milestones = new List<MilestoneItem>(),
                Quizzes = new List<PlanQuizItem>()
            };
        }

        /// <summary>
        /// Strips markdown code fences (```json ... ```) from a string.
        /// </summary>
        private static string StripMarkdownCodeFences(string text)
        {
            var trimmed = text.Trim();

            // Remove leading code fence
            if (trimmed.StartsWith("```"))
            {
                var firstNewline = trimmed.IndexOf('\n');
                if (firstNewline > 0)
                {
                    trimmed = trimmed[(firstNewline + 1)..];
                }
                else
                {
                    trimmed = trimmed[3..];
                }
            }

            // Remove trailing code fence
            if (trimmed.EndsWith("```"))
            {
                trimmed = trimmed[..^3].TrimEnd();
            }

            return trimmed;
        }

        /// <summary>
        /// Fallback method: scans raw text for JSON array of question objects.
        /// </summary>
        private static List<OpenRouterGeneratedQuestion> ExtractQuizArrayByScanning(string text)
        {
            var questions = new List<OpenRouterGeneratedQuestion>();

            var start = text.IndexOf('[');
            var end = text.LastIndexOf(']');

            if (start >= 0 && end > start)
            {
                var arrayText = text[start..(end + 1)];
                try
                {
                    questions = JsonSerializer.Deserialize<List<OpenRouterGeneratedQuestion>>(
                        arrayText, _jsonOptions) ?? new List<OpenRouterGeneratedQuestion>();
                }
                catch (JsonException)
                {
                    // Cannot parse, return empty
                }
            }

            return questions;
        }

        /// <summary>
        /// Maps a Quiz entity to a QuizDto.
        /// </summary>
        private static QuizDto MapToDto(Quiz quiz)
        {
            return new QuizDto
            {
                Id = quiz.Id,
                Question = quiz.Question,
                Options = quiz.Options ?? new List<string>(),
                CorrectAnswer = quiz.CorrectAnswer,
                Category = quiz.Category,
                Difficulty = quiz.Difficulty
            };
        }

        // ==================== OpenRouter API Models ====================

        /// <summary>
        /// Request body for the OpenRouter chat completions endpoint.
        /// </summary>
        private class OpenRouterChatRequest
        {
            [JsonPropertyName("model")]
            public string Model { get; set; } = string.Empty;

            [JsonPropertyName("messages")]
            public List<OpenRouterMessage> Messages { get; set; } = new();

            [JsonPropertyName("temperature")]
            public double Temperature { get; set; } = 0.7;

            [JsonPropertyName("max_tokens")]
            public int MaxTokens { get; set; } = 2000;

            [JsonPropertyName("response_format")]
            public OpenRouterResponseFormat? ResponseFormat { get; set; }
        }

        /// <summary>
        /// A single message in the chat completions request.
        /// </summary>
        private class OpenRouterMessage
        {
            [JsonPropertyName("role")]
            public string Role { get; set; } = string.Empty;

            [JsonPropertyName("content")]
            public string Content { get; set; } = string.Empty;
        }

        /// <summary>
        /// Response format specification for the model.
        /// </summary>
        private class OpenRouterResponseFormat
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = "json_object";
        }

        /// <summary>
        /// Response from the OpenRouter chat completions endpoint.
        /// </summary>
        private class OpenRouterChatResponse
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonPropertyName("model")]
            public string Model { get; set; } = string.Empty;

            [JsonPropertyName("choices")]
            public List<OpenRouterChoice> Choices { get; set; } = new();

            [JsonPropertyName("usage")]
            public OpenRouterUsage? Usage { get; set; }
        }

        /// <summary>
        /// A single choice in the chat completions response.
        /// </summary>
        private class OpenRouterChoice
        {
            [JsonPropertyName("index")]
            public int Index { get; set; }

            [JsonPropertyName("message")]
            public OpenRouterResponseMessage Message { get; set; } = new();

            [JsonPropertyName("finish_reason")]
            public string? FinishReason { get; set; }
        }

        /// <summary>
        /// The message content inside a choice.
        /// </summary>
        private class OpenRouterResponseMessage
        {
            [JsonPropertyName("role")]
            public string Role { get; set; } = string.Empty;

            [JsonPropertyName("content")]
            public string? Content { get; set; }
        }

        /// <summary>
        /// Token usage information from the API.
        /// </summary>
        private class OpenRouterUsage
        {
            [JsonPropertyName("prompt_tokens")]
            public int PromptTokens { get; set; }

            [JsonPropertyName("completion_tokens")]
            public int CompletionTokens { get; set; }

            [JsonPropertyName("total_tokens")]
            public int TotalTokens { get; set; }
        }

        /// <summary>
        /// A single quiz question parsed from the AI response.
        /// </summary>
        private class OpenRouterGeneratedQuestion
        {
            [JsonPropertyName("question")]
            public string Question { get; set; } = string.Empty;

            [JsonPropertyName("options")]
            public List<string> Options { get; set; } = new();

            [JsonPropertyName("correct_answer")]
            public string? CorrectAnswer { get; set; }
        }
    }

    /// <summary>
    /// Exception thrown when the OpenRouter API returns an error response.
    /// </summary>
    public class AiAssistantApiException : Exception
    {
        /// <summary>
        /// The HTTP status code returned by the API.
        /// </summary>
        public System.Net.HttpStatusCode StatusCode { get; }

        /// <summary>
        /// Initializes a new instance of the AiAssistantApiException class.
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="statusCode">HTTP status code</param>
        public AiAssistantApiException(string message, System.Net.HttpStatusCode statusCode)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }

    /// <summary>
    /// Exception thrown when the OpenRouter API is unavailable after all retries.
    /// Maps to HTTP 503 Service Unavailable in the controller.
    /// </summary>
    public class AiAssistantServiceUnavailableException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the AiAssistantServiceUnavailableException class.
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="innerException">The underlying exception</param>
        public AiAssistantServiceUnavailableException(string message, Exception? innerException)
            : base(message, innerException)
        {
        }
    }
}