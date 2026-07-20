namespace DOANTOTNGHIEP.src.Models
{
    /// <summary>
    /// Configuration section for prompt templates used in AI quiz generation.
    /// Holds a collection of PromptTemplate entries, keyed by target type.
    /// Bound from appsettings.json section "PromptTemplateSettings".
    /// </summary>
    public class PromptTemplateSettings
    {
        /// <summary>
        /// The configuration section name in appsettings.json.
        /// </summary>
        public const string SectionName = "PromptTemplateSettings";

        /// <summary>
        /// Collection of prompt templates. The key is the target type
        /// (e.g., "vocabulary", "grammar") or "*" for the default fallback.
        /// </summary>
        public Dictionary<string, PromptTemplate> Templates { get; set; } = new();
    }
}