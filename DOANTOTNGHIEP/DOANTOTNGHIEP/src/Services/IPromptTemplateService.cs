using DOANTOTNGHIEP.src.Models;

namespace DOANTOTNGHIEP.src.Services
{
    /// <summary>
    /// Service interface for building AI prompts from configurable templates.
    /// Resolves the appropriate template based on the provided lookup key
    /// (the target type, category, or a named plan template), applies placeholder
    /// substitution, and produces the system + user prompts for the AI API call.
    /// </summary>
    public interface IPromptTemplateService
    {
        /// <summary>
        /// Builds the complete prompt pair (system + user) for AI quiz generation
        /// based on the user's target and requested generation parameters.
        /// </summary>
        /// <param name="target">The user's target entity</param>
        /// <param name="count">Number of quizzes to generate</param>
        /// <param name="category">The effective category for the quizzes</param>
        /// <param name="difficulty">The difficulty level (Easy|Medium|Hard)</param>
        /// <param name="templateKey">
        /// The lookup key for resolving the prompt template (target category or "*" for default).
        /// </param>
        /// <returns>A tuple of (systemPrompt, userPrompt) ready to send to the AI API</returns>
        (string SystemPrompt, string UserPrompt) BuildPrompts(
            Target target,
            int count,
            string category,
            string difficulty,
            string templateKey);

        /// <summary>
        /// Builds the complete prompt pair for AI plan generation.
        /// Produces prompts that ask the AI to generate a structured study plan
        /// (summary, milestones with checklists, and quizzes) for the given target.
        /// </summary>
        /// <param name="target">The user's target entity</param>
        /// <returns>A tuple of (systemPrompt, userPrompt)</returns>
        (string SystemPrompt, string UserPrompt) BuildPlanPrompt(Target target);

        /// <summary>
        /// Builds the prompt pair for regenerating/refining an AI plan based on user feedback.
        /// The existing plan is sent as context so the AI knows what to refine.
        /// </summary>
        /// <param name="target">The user's target entity</param>
        /// <param name="feedback">User feedback to guide the plan refinement</param>
        /// <param name="existingPlan">The current plan to refine (sent to the AI as context)</param>
        /// <returns>A tuple of (systemPrompt, userPrompt)</returns>
        (string SystemPrompt, string UserPrompt) BuildRegeneratePlanPrompt(Target target, string feedback, PlanData? existingPlan);

        /// <summary>
        /// Gets the resolved template for a given lookup key.
        /// Falls back to the "*" default template if no specific match exists.
        /// </summary>
        /// <param name="templateKey">The lookup key (target type, category, or named template)</param>
        /// <returns>The matching PromptTemplate</returns>
        PromptTemplate ResolveTemplate(string templateKey);
    }
}