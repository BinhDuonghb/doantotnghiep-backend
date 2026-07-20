namespace DOANTOTNGHIEP.src.Models
{
    /// <summary>
    /// Represents a configurable prompt template for AI quiz generation.
    /// Each template defines how to construct system and user prompts based on
    /// the user's target type, value, unit, and desired difficulty/category.
    /// </summary>
    public class PromptTemplate
    {
        /// <summary>
        /// The target type this template applies to (e.g., "vocabulary", "grammar", "reading").
        /// Use "*" for a fallback/default template that matches any target type.
        /// </summary>
        public string TargetType { get; set; } = "*";

        /// <summary>
        /// Human-readable description of when this template is used.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The system prompt that sets the AI's role and behavior.
        /// Supports placeholders:
        ///   {{TargetType}} - the target type (e.g., "vocabulary")
        ///   {{Category}}   - the quiz category
        ///   {{Difficulty}} - the difficulty level
        ///   {{Count}}      - number of quizzes to generate
        /// </summary>
        public string SystemPrompt { get; set; } = string.Empty;

        /// <summary>
        /// The user prompt template that specifies what to generate.
        /// Supports all system prompt placeholders plus:
        ///   {{Title}}          - the target title
        ///   {{TargetValue}}    - the target goal value
        ///   {{TargetUnit}}     - the target unit (e.g., "words", "points")
        ///   {{CurrentProgress}} - the user's current progress
        ///   {{QuestionTypes}}  - comma-separated list of question types
        /// </summary>
        public string UserPromptTemplate { get; set; } = string.Empty;

        /// <summary>
        /// Question types appropriate for this target type.
        /// Examples: "definition", "synonym", "fill-in-blank", "application",
        /// "true-false", "multiple-choice-scenario", "antonym", "translation".
        /// </summary>
        public List<string> QuestionTypes { get; set; } = new();

        /// <summary>
        /// Category suggestions mapped by difficulty level.
        /// The key is the difficulty (Easy, Medium, Hard) and the value is
        /// a list of sub-categories within the target type.
        /// Example for "vocabulary":
        ///   Easy: ["common-words", "daily-life"]
        ///   Medium: ["academic-words", "business-terms"]
        ///   Hard: ["advanced-idioms", "technical-jargon"]
        /// </summary>
        public Dictionary<string, List<string>> CategoryMappings { get; set; } = new();

        /// <summary>
        /// Difficulty-level-specific instruction overrides.
        /// The key is the difficulty (Easy, Medium, Hard) and the value is
        /// additional instructions appended to the prompt.
        /// Example:
        ///   Easy: "Use simple, common vocabulary. Questions should be straightforward."
        ///   Hard: "Use advanced vocabulary. Include nuanced distractors."
        /// </summary>
        public Dictionary<string, string> DifficultyInstructions { get; set; } = new();

        /// <summary>
        /// Expected JSON output format specification.
        /// Appended to the user prompt to instruct the AI on the response format.
        /// Supports {{Fields}} placeholder for the expected JSON fields.
        /// </summary>
        public string OutputFormat { get; set; } =
            "Return the response as a JSON object with a \"quizzes\" array. " +
            "Each quiz object has fields: {{Fields}}. " +
            "Ensure the correct answer exactly matches one of the options.";

        /// <summary>
        /// The response schema fields to expect from the AI.
        /// Defaults to the standard quiz fields.
        /// </summary>
        public List<string> ResponseFields { get; set; } = new()
        {
            "question",
            "options",
            "correct_answer"
        };
    }
}