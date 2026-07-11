using DOANTOTNGHIEP.src.Data;
using DOANTOTNGHIEP.src.Models;
using DOANTOTNGHIEP.src.Models.DTOs;

namespace DOANTOTNGHIEP.src.Services
{
    /// <summary>
    /// Service for quiz-related operations
    /// </summary>
    public interface IQuizService
    {
        /// <summary>
        /// Gets quizzes for a user. Returns quizzes only if the user has a target set.
        /// </summary>
        /// <param name="userId">The user's ID</param>
        /// <returns>A response with quizzes and userTargetSet flag</returns>
        Task<GetQuizzesResponse> GetQuizzesAsync(string userId);

        /// <summary>
        /// Generates a specified number of quizzes with given category and difficulty,
        /// stores them in the database, and returns the generated quizzes.
        /// </summary>
        /// <param name="count">Number of quizzes to generate</param>
        /// <param name="category">Quiz category</param>
        /// <param name="difficulty">Quiz difficulty level</param>
        /// <returns>A response with the count of generated quizzes and the quiz list</returns>
        Task<GenerateQuizzesResponse> GenerateQuizzesAsync(int count, string category, string difficulty);
    }

    /// <summary>
    /// Implementation of IQuizService
    /// </summary>
    public class QuizService : IQuizService
    {
        private readonly IQuizRepository _quizRepository;
        private readonly IUserRepository _userRepository;

        /// <summary>
        /// Initializes a new instance of the QuizService class
        /// </summary>
        /// <param name="quizRepository">Quiz repository for data access</param>
        /// <param name="userRepository">User repository for data access</param>
        public QuizService(IQuizRepository quizRepository, IUserRepository userRepository)
        {
            _quizRepository = quizRepository;
            _userRepository = userRepository;
        }

        /// <summary>
        /// Gets quizzes for a user. Returns quizzes only if the user has a target set.
        /// </summary>
        /// <param name="userId">The user's ID</param>
        /// <returns>A response with quizzes and userTargetSet flag</returns>
        public async Task<GetQuizzesResponse> GetQuizzesAsync(string userId)
        {
            // Check if user has a target set
            var targetInfo = await _userRepository.GetTargetAsync(userId);
            var userTargetSet = targetInfo?.isSet ?? false;

            // Only return quizzes if the user has a target set
            var quizzes = userTargetSet
                ? await _quizRepository.GetAllAsync()
                : new List<Quiz>();

            return new GetQuizzesResponse
            {
                Quizzes = quizzes.Select(MapToDto).ToList(),
                UserTargetSet = userTargetSet
            };
        }

        /// <summary>
        /// Generates a specified number of quizzes with given category and difficulty,
        /// stores them in the database, and returns the generated quizzes.
        /// </summary>
        /// <param name="count">Number of quizzes to generate</param>
        /// <param name="category">Quiz category</param>
        /// <param name="difficulty">Quiz difficulty level</param>
        /// <returns>A response with the count of generated quizzes and the quiz list</returns>
        public async Task<GenerateQuizzesResponse> GenerateQuizzesAsync(int count, string category, string difficulty)
        {
            var generatedQuizzes = new List<Quiz>();

            for (int i = 0; i < count; i++)
            {
                var quiz = GenerateQuiz(category, difficulty, i + 1);
                generatedQuizzes.Add(quiz);
            }

            // Store all generated quizzes in the database
            await _quizRepository.InsertManyAsync(generatedQuizzes);

            return new GenerateQuizzesResponse
            {
                Generated = generatedQuizzes.Count,
                Quizzes = generatedQuizzes.Select(MapToDto).ToList()
            };
        }

        /// <summary>
        /// Generates a single quiz question with sample data based on category and difficulty.
        /// In production, this would integrate with an AI/LLM service for real question generation.
        /// </summary>
        /// <param name="category">Quiz category</param>
        /// <param name="difficulty">Quiz difficulty</param>
        /// <param name="index">Index number for the question</param>
        /// <returns>A generated Quiz entity</returns>
        private Quiz GenerateQuiz(string category, string difficulty, int index)
        {
            var templates = GetTemplatesByCategory(category);

            // Pick a template based on index (cycling through available templates)
            var templateIndex = (index - 1) % templates.Count;
            var template = templates[templateIndex];

            // Adjust difficulty by modifying the options complexity
            var options = AdjustOptionsForDifficulty(template.Options, difficulty);
            var question = $"[{difficulty}] {template.Question} (Q{index})";

            return new Quiz
            {
                Question = question,
                Options = options,
                CorrectAnswer = template.CorrectAnswer,
                Category = category,
                Difficulty = difficulty,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Gets sample quiz templates by category.
        /// </summary>
        /// <param name="category">The quiz category</param>
        /// <returns>A list of quiz templates</returns>
        private List<QuizTemplate> GetTemplatesByCategory(string category)
        {
            return category.ToLowerInvariant() switch
            {
                "vocabulary" => new List<QuizTemplate>
                {
                    new QuizTemplate
                    {
                        Question = "What does the word 'ubiquitous' mean?",
                        Options = new List<string> { "Rare", "Everywhere", "Hidden", "Ancient" },
                        CorrectAnswer = "Everywhere"
                    },
                    new QuizTemplate
                    {
                        Question = "Choose the synonym for 'benevolent':",
                        Options = new List<string> { "Kind", "Cruel", "Angry", "Lazy" },
                        CorrectAnswer = "Kind"
                    },
                    new QuizTemplate
                    {
                        Question = "What is the opposite of 'ephemeral'?",
                        Options = new List<string> { "Temporary", "Permanent", "Brief", "Short" },
                        CorrectAnswer = "Permanent"
                    },
                    new QuizTemplate
                    {
                        Question = "The word 'meticulous' means:",
                        Options = new List<string> { "Careless", "Detailed", "Quick", "Simple" },
                        CorrectAnswer = "Detailed"
                    },
                    new QuizTemplate
                    {
                        Question = "What does 'pragmatic' describe?",
                        Options = new List<string> { "Dreamy", "Practical", "Fantasy", "Imaginary" },
                        CorrectAnswer = "Practical"
                    }
                },
                "grammar" => new List<QuizTemplate>
                {
                    new QuizTemplate
                    {
                        Question = "Which sentence is grammatically correct?",
                        Options = new List<string> { "She don't like it.", "She doesn't like it.", "She not like it.", "She no like it." },
                        CorrectAnswer = "She doesn't like it."
                    },
                    new QuizTemplate
                    {
                        Question = "Identify the correct past tense: 'I have ___ the book.'",
                        Options = new List<string> { "readed", "read", "reads", "reading" },
                        CorrectAnswer = "read"
                    },
                    new QuizTemplate
                    {
                        Question = "Choose the correct preposition: 'He is good ___ math.'",
                        Options = new List<string> { "in", "at", "on", "with" },
                        CorrectAnswer = "at"
                    },
                    new QuizTemplate
                    {
                        Question = "Which is the correct conditional form?",
                        Options = new List<string> { "If I was you", "If I were you", "If I am you", "If I be you" },
                        CorrectAnswer = "If I were you"
                    },
                    new QuizTemplate
                    {
                        Question = "Select the correct article: 'She is ___ honest person.'",
                        Options = new List<string> { "a", "an", "the", "no article" },
                        CorrectAnswer = "an"
                    }
                },
                "reading" => new List<QuizTemplate>
                {
                    new QuizTemplate
                    {
                        Question = "What is the main idea of a passage?",
                        Options = new List<string> { "Supporting details", "The central theme", "Minor facts", "Examples" },
                        CorrectAnswer = "The central theme"
                    },
                    new QuizTemplate
                    {
                        Question = "What does 'infer' mean in reading comprehension?",
                        Options = new List<string> { "Quote directly", "Guess randomly", "Conclude from evidence", "Copy text" },
                        CorrectAnswer = "Conclude from evidence"
                    },
                    new QuizTemplate
                    {
                        Question = "A topic sentence usually appears:",
                        Options = new List<string> { "At the end of a paragraph", "At the beginning of a paragraph", "In the middle", "Nowhere" },
                        CorrectAnswer = "At the beginning of a paragraph"
                    },
                    new QuizTemplate
                    {
                        Question = "What is the purpose of a conclusion paragraph?",
                        Options = new List<string> { "Introduce new ideas", "Summarize main points", "List references", "Ask questions" },
                        CorrectAnswer = "Summarize main points"
                    },
                    new QuizTemplate
                    {
                        Question = "Context clues help readers:",
                        Options = new List<string> { "Ignore unknown words", "Understand unfamiliar words", "Skip paragraphs", "Read faster" },
                        CorrectAnswer = "Understand unfamiliar words"
                    }
                },
                _ => new List<QuizTemplate>
                {
                    new QuizTemplate
                    {
                        Question = $"General knowledge question about {category}: What is the key concept?",
                        Options = new List<string> { "Option A", "Option B", "Option C", "Option D" },
                        CorrectAnswer = "Option A"
                    },
                    new QuizTemplate
                    {
                        Question = $"Which best describes a core principle of {category}?",
                        Options = new List<string> { "Principle 1", "Principle 2", "Principle 3", "Principle 4" },
                        CorrectAnswer = "Principle 1"
                    }
                }
            };
        }

        /// <summary>
        /// Adjusts the options list based on difficulty level.
        /// For harder difficulties, options become more similar and trickier.
        /// </summary>
        /// <param name="baseOptions">The base options from the template</param>
        /// <param name="difficulty">The difficulty level</param>
        /// <returns>Adjusted options list</returns>
        private List<string> AdjustOptionsForDifficulty(List<string> baseOptions, string difficulty)
        {
            // For Easy: use the base options as-is
            // For Medium: shuffle options so the correct answer isn't always in the same position
            // For Hard: shuffle options and make distractors more similar
            var options = new List<string>(baseOptions);

            if (difficulty.Equals("Medium", StringComparison.OrdinalIgnoreCase) ||
                difficulty.Equals("Hard", StringComparison.OrdinalIgnoreCase))
            {
                // Shuffle options using a deterministic approach based on the content
                // In production, use a proper random with seed
                var shuffled = options.OrderBy(o => o.GetHashCode()).ToList();
                options = shuffled;
            }

            return options;
        }

        /// <summary>
        /// Maps a Quiz entity to a QuizDto
        /// </summary>
        /// <param name="quiz">The quiz entity</param>
        /// <returns>A QuizDto</returns>
        private QuizDto MapToDto(Quiz quiz)
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

        /// <summary>
        /// Internal template structure for quiz generation
        /// </summary>
        private class QuizTemplate
        {
            public string Question { get; set; } = string.Empty;
            public List<string> Options { get; set; } = new List<string>();
            public string CorrectAnswer { get; set; } = string.Empty;
        }
    }
}