using DOANTOTNGHIEP.src.Models;
using Microsoft.Extensions.Options;
using System.Text;

namespace DOANTOTNGHIEP.src.Services
{
    /// <summary>
    /// Implementation of IPromptTemplateService that builds AI prompts
    /// from configurable templates stored in appsettings.json.
    ///
    /// Templates are resolved by matching the lookup key (target type, category,
    /// or a named plan template like "plan") against the template dictionary key.
    /// Falls back to the "*" default template when no specific match exists.
    ///
    /// Placeholder substitution replaces tokens like {{Title}}, {{Description}},
    /// {{Difficulty}}, etc. with actual values from the target and generation parameters.
    /// </summary>
    public class PromptTemplateService : IPromptTemplateService
    {
        private readonly PromptTemplateSettings _settings;
        private readonly ILogger<PromptTemplateService> _logger;

        /// <summary>
        /// Initializes a new instance of PromptTemplateService.
        /// </summary>
        /// <param name="settings">Prompt template configuration via IOptions</param>
        /// <param name="logger">Logger instance</param>
        public PromptTemplateService(
            IOptions<PromptTemplateSettings> settings,
            ILogger<PromptTemplateService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        /// <inheritdoc />
        public (string SystemPrompt, string UserPrompt) BuildPrompts(
            Target target,
            int count,
            string category,
            string difficulty,
            string templateKey)
        {
            // Resolve the best matching template
            var template = ResolveTemplate(templateKey);

            _logger.LogInformation(
                "Using prompt template '{TemplateType}' for key '{TemplateKey}'",
                template.TargetType,
                templateKey);

            // Determine question types for this generation
            var questionTypes = template.QuestionTypes.Count > 0
                ? template.QuestionTypes
                : GetDefaultQuestionTypes(templateKey);

            // Get difficulty-specific instructions
            var difficultyInstruction = template.DifficultyInstructions.TryGetValue(difficulty, out var instr)
                ? instr
                : string.Empty;

            // Build the placeholder substitution dictionary
            var placeholders = BuildTargetPlaceholders(target, category, difficulty, count, questionTypes, difficultyInstruction, template);

            // Apply placeholder substitution to system and user prompts
            var systemPrompt = ReplacePlaceholders(template.SystemPrompt, placeholders);
            var userPrompt = BuildFullUserPrompt(template, placeholders, difficultyInstruction);

            _logger.LogDebug(
                "Built prompts: SystemPrompt length={SysLen}, UserPrompt length={UserLen}",
                systemPrompt.Length,
                userPrompt.Length);

            return (systemPrompt, userPrompt);
        }

        /// <inheritdoc />
        public (string SystemPrompt, string UserPrompt) BuildPlanPrompt(Target target)
        {
            var template = ResolveTemplate("plan");

            _logger.LogInformation(
                "Using plan prompt template '{TemplateType}' for target '{TargetId}'",
                template.TargetType,
                target.Id);

            var placeholders = BuildTargetPlaceholders(
                target,
                category: string.Empty,
                difficulty: string.Empty,
                count: 0,
                questionTypes: template.QuestionTypes,
                difficultyInstruction: string.Empty,
                template);

            // Add feedback placeholder (empty for initial generation)
            placeholders["{{Feedback}}"] = string.Empty;
            placeholders["{{ExistingPlan}}"] = string.Empty;
            placeholders["{{UserProgress}}"] = "No progress yet — this is a new plan.";

            var systemPrompt = ReplacePlaceholders(template.SystemPrompt, placeholders);
            var userPrompt = BuildFullUserPrompt(template, placeholders, string.Empty);

            return (systemPrompt, userPrompt);
        }

        /// <inheritdoc />
        public (string SystemPrompt, string UserPrompt) BuildRegeneratePlanPrompt(
            Target target, string feedback, PlanData? existingPlan)
        {
            var template = ResolveTemplate("plan_regenerate");

            _logger.LogInformation(
                "Using plan_regenerate template '{TemplateType}' for target '{TargetId}' with feedback and existing plan context",
                template.TargetType,
                target.Id);

            var placeholders = BuildTargetPlaceholders(
                target,
                category: string.Empty,
                difficulty: string.Empty,
                count: 0,
                questionTypes: template.QuestionTypes,
                difficultyInstruction: string.Empty,
                template);

            placeholders["{{Feedback}}"] = feedback ?? string.Empty;

            // Serialize the existing plan as JSON context for the AI to refine
            if (existingPlan != null)
            {
                var existingPlanJson = System.Text.Json.JsonSerializer.Serialize(
                    existingPlan,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                        WriteIndented = true
                    });
                placeholders["{{ExistingPlan}}"] = existingPlanJson;

                // Build a human-readable progress summary so the AI knows what's done
                placeholders["{{UserProgress}}"] = BuildProgressSummary(existingPlan);
            }
            else
            {
                placeholders["{{ExistingPlan}}"] = "No existing plan available. Create a new plan.";
                placeholders["{{UserProgress}}"] = "No progress yet — this is a new plan.";
            }

            var systemPrompt = ReplacePlaceholders(template.SystemPrompt, placeholders);
            var userPrompt = BuildFullUserPrompt(template, placeholders, string.Empty);

            return (systemPrompt, userPrompt);
        }

        /// <inheritdoc />
        public PromptTemplate ResolveTemplate(string templateKey)
        {
            // Handle empty key — fall back to default
            if (string.IsNullOrWhiteSpace(templateKey))
            {
                templateKey = "*";
            }

            // Try exact match first (case-insensitive)
            var normalizedKey = templateKey.Trim().ToLowerInvariant();

            foreach (var kvp in _settings.Templates)
            {
                if (kvp.Key.Trim().ToLowerInvariant() == normalizedKey)
                {
                    _logger.LogDebug("Resolved exact template match for '{TemplateKey}'", templateKey);
                    return kvp.Value;
                }
            }

            // Fall back to the default template
            if (_settings.Templates.TryGetValue("*", out var defaultTemplate))
            {
                _logger.LogInformation(
                    "No specific template for '{TemplateKey}', using default '*' template",
                    templateKey);
                return defaultTemplate;
            }

            // Ultimate fallback: build a minimal template on the fly
            _logger.LogWarning(
                "No templates configured at all. Using built-in minimal template for '{TemplateKey}'",
                templateKey);

            return BuildMinimalFallbackTemplate(templateKey);
        }

        /// <summary>
        /// Builds the common placeholder dictionary from a Target entity.
        /// Used by both quiz and plan prompt construction.
        /// </summary>
        private static Dictionary<string, string> BuildTargetPlaceholders(
            Target target,
            string category,
            string difficulty,
            int count,
            List<string> questionTypes,
            string difficultyInstruction,
            PromptTemplate template)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["{{TargetType}}"] = category, // backward-compat for templates still referencing it
                ["{{Category}}"] = category,
                ["{{Difficulty}}"] = difficulty,
                ["{{Count}}"] = count.ToString(),
                ["{{Title}}"] = target.Title,
                ["{{Description}}"] = target.Description ?? string.Empty,
                ["{{Duration}}"] = target.Duration.ToString(),
                ["{{DurationUnit}}"] = target.DurationUnit,
                ["{{Status}}"] = target.Status,
                ["{{QuestionTypes}}"] = string.Join(", ", questionTypes),
                ["{{DifficultyInstruction}}"] = difficultyInstruction,
                ["{{Fields}}"] = string.Join(", ", template.ResponseFields)
            };
        }

        /// <summary>
        /// Applies placeholder substitution to a template string.
        /// Replaces all {{Key}} tokens with their corresponding values.
        /// </summary>
        private static string ReplacePlaceholders(string template, Dictionary<string, string> placeholders)
        {
            var result = template;
            foreach (var (key, value) in placeholders)
            {
                result = result.Replace(key, value, StringComparison.OrdinalIgnoreCase);
            }
            return result;
        }

        /// <summary>
        /// Builds the full user prompt by combining the template's user prompt,
        /// difficulty instructions, and output format specification.
        /// </summary>
        private string BuildFullUserPrompt(
            PromptTemplate template,
            Dictionary<string, string> placeholders,
            string difficultyInstruction)
        {
            var sb = new StringBuilder();

            // Main user prompt from the template
            var userPrompt = ReplacePlaceholders(template.UserPromptTemplate, placeholders);
            sb.AppendLine(userPrompt);

            // Append difficulty-specific instruction if present
            if (!string.IsNullOrWhiteSpace(difficultyInstruction))
            {
                sb.AppendLine();
                sb.AppendLine(difficultyInstruction);
            }

            // Append output format specification
            var outputFormat = ReplacePlaceholders(template.OutputFormat, placeholders);
            sb.AppendLine();
            sb.AppendLine(outputFormat);

            // Append question type variation reminder (only relevant for quiz templates)
            if (!string.IsNullOrWhiteSpace(placeholders["{{QuestionTypes}}"]))
            {
                sb.AppendLine();
                sb.Append("Vary the question types among: ");
                sb.AppendLine(placeholders["{{QuestionTypes}}"]);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Returns default question types for a known category/target type.
        /// Used when the template doesn't specify question types.
        /// </summary>
        private static List<string> GetDefaultQuestionTypes(string category)
        {
            var normalized = (category ?? string.Empty).Trim().ToLowerInvariant();

            return normalized switch
            {
                "vocabulary" => new List<string>
                {
                    "definition", "synonym", "antonym",
                    "fill-in-blank", "word-in-context"
                },
                "grammar" => new List<string>
                {
                    "fill-in-blank", "error-correction",
                    "sentence-transformation", "tense-identification",
                    "multiple-choice-rule"
                },
                "reading" => new List<string>
                {
                    "comprehension", "inference",
                    "vocabulary-in-context", "main-idea",
                    "detail-recall"
                },
                "listening" => new List<string>
                {
                    "comprehension", "detail-identification",
                    "inference", "speaker-intent",
                    "vocabulary-in-context"
                },
                "speaking" => new List<string>
                {
                    "pronunciation", "response-formulation",
                    "situational-dialogue", "opinion-expression",
                    "fluency-practice"
                },
                _ => new List<string>
                {
                    "multiple-choice", "fill-in-blank",
                    "true-false", "definition",
                    "application"
                }
            };
        }

        /// <summary>
        /// Builds a minimal fallback template when no templates are configured.
        /// Ensures the system still works even with empty configuration.
        /// </summary>
        private static PromptTemplate BuildMinimalFallbackTemplate(string templateKey)
        {
            // For plan templates, build a plan-oriented fallback
            if (string.Equals(templateKey, "plan", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(templateKey, "plan_regenerate", StringComparison.OrdinalIgnoreCase))
            {
                return new PromptTemplate
                {
                    TargetType = "*",
                    Description = "Auto-generated fallback plan template",
                    SystemPrompt = "You are an expert study plan designer. Always respond with valid JSON. Do not include any text outside the JSON object.",
                    UserPromptTemplate = "Generate a comprehensive study plan for the learning target: {{Title}}. " +
                        "Description: {{Description}}. " +
                        "Timeframe: {{Duration}} {{DurationUnit}}. " +
                        "Produce a summary, ordered milestones each with a duration estimate and checklist items, " +
                        "and a few quiz questions tied to milestones.{{Feedback}}",
                    QuestionTypes = new List<string>(),
                    OutputFormat = "Return JSON with fields: summary, milestones (each: title, description, order, duration_estimate, checklist_items [{title}]), " +
                        "quizzes (each: question, options, correct_answer, milestone_title)."
                };
            }

            return new PromptTemplate
            {
                TargetType = "*",
                Description = $"Auto-generated fallback template for {templateKey}",
                SystemPrompt = "You are a quiz generator. Always respond with valid JSON containing the requested quiz questions. Do not include any text outside the JSON object.",
                UserPromptTemplate = "Generate {{Count}} {{Difficulty}} difficulty quiz questions about {{Category}}. " +
                    "The user's learning target is: {{Title}}. " +
                    "Description: {{Description}}. " +
                    "Timeframe: {{Duration}} {{DurationUnit}}. " +
                    "Current status: {{Status}}. " +
                    "Make questions relevant to the user's target.",
                QuestionTypes = new List<string> { "multiple-choice", "fill-in-blank", "definition", "application", "true-false" },
                OutputFormat = "Return the response as a JSON object with a \"quizzes\" array. " +
                    "Each quiz object has fields: question, options, correct_answer. " +
                    "Ensure the correct answer exactly matches one of the options."
            };
        }

        /// <summary>
        /// Builds a human-readable progress summary from the existing plan.
        /// Tells the AI exactly which milestones and checklist items the user has completed.
        /// </summary>
        private static string BuildProgressSummary(PlanData plan)
        {
            var sb = new StringBuilder();

            var totalChecklistItems = plan.Milestones.SelectMany(m => m.ChecklistItems).Count();
            var completedChecklistItems = plan.Milestones.SelectMany(m => m.ChecklistItems).Count(c => c.IsCompleted);
            var totalMilestones = plan.Milestones.Count;
            var completedMilestones = plan.Milestones.Count(m =>
                m.ChecklistItems.Count > 0 && m.ChecklistItems.All(c => c.IsCompleted));

            sb.AppendLine($"Overall progress: {completedChecklistItems}/{totalChecklistItems} checklist items completed ({completedMilestones}/{totalMilestones} milestones fully done).");
            sb.AppendLine();

            foreach (var milestone in plan.Milestones)
            {
                var done = milestone.ChecklistItems.Count(c => c.IsCompleted);
                var total = milestone.ChecklistItems.Count;
                if (done == total && total > 0)
                {
                    sb.AppendLine($"✅ MILESTONE {milestone.Order}: '{milestone.Title}' — FULLY COMPLETED ({done}/{total} tasks done)");
                }
                else
                {
                    sb.AppendLine($"🔲 MILESTONE {milestone.Order}: '{milestone.Title}' — {done}/{total} tasks done");
                    foreach (var item in milestone.ChecklistItems)
                    {
                        sb.AppendLine(item.IsCompleted
                            ? $"   ✅ Completed: \"{item.Title}\""
                            : $"   ⬜ Not done: \"{item.Title}\"");
                    }
                }
            }

            return sb.ToString();
        }
    }
}