using DOANTOTNGHIEP.src.Models;
using DOANTOTNGHIEP.src.Models.DTOs;

namespace DOANTOTNGHIEP.src.Services
{
    /// <summary>
    /// Service interface for AI-powered assistant features via OpenRouter API.
    /// Handles quiz generation, study plan generation, and plan refinement,
    /// all based on a user's target.
    /// </summary>
    public interface IAiAssistantService
    {
        /// <summary>
        /// Generates quizzes from a user target by calling the OpenRouter AI API.
        /// </summary>
        /// <param name="target">The target entity to base quiz generation on</param>
        /// <param name="count">Number of quizzes to generate</param>
        /// <param name="category">Category override (optional). Used as the prompt template lookup key when provided.</param>
        /// <param name="difficulty">Difficulty level (Easy|Medium|Hard)</param>
        /// <returns>A response with generated quizzes and the target used</returns>
        Task<GenerateQuizzesFromTargetResponse> GenerateQuizzesFromTargetAsync(
            Target target,
            int count,
            string? category,
            string difficulty);

        /// <summary>
        /// Generates a structured AI study plan for the given target.
        /// The plan includes a summary, ordered milestones with checklist items,
        /// and quiz questions associated with specific milestones.
        /// </summary>
        /// <param name="target">The target to generate a plan for</param>
        /// <returns>The generated plan data</returns>
        Task<PlanData> GeneratePlanAsync(Target target);

        /// <summary>
        /// Regenerates/refines an AI plan for the given target based on user feedback.
        /// The existing plan is sent to the AI as context so it can refine rather than start over.
        /// </summary>
        /// <param name="target">The target the plan belongs to (must have an existing Plan)</param>
        /// <param name="feedback">User feedback to guide the refinement</param>
        /// <returns>The refined plan data</returns>
        Task<PlanData> RegeneratePlanAsync(Target target, string feedback);
    }
}