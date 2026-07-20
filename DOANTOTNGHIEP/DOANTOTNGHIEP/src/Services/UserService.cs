using DOANTOTNGHIEP.src.Data;
using DOANTOTNGHIEP.src.Models;
using DOANTOTNGHIEP.src.Models.DTOs;
using DOANTOTNGHIEP.src.Services;

namespace DOANTOTNGHIEP.src.Services
{
    /// <summary>
    /// Service for user-related operations using UserRepository
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Creates a new user
        /// </summary>
        /// <param name="email">User's email</param>
        /// <param name="password">User's password</param>
        /// <param name="fullName">User's full name</param>
        /// <returns>The created user</returns>
        Task<User> CreateUserAsync(string email, string password, string fullName);

        /// <summary>
        /// Gets a user by email
        /// </summary>
        /// <param name="email">User's email</param>
        /// <returns>The user if found, null otherwise</returns>
        Task<User?> GetUserByEmailAsync(string email);

        /// <summary>
        /// Updates a user's refresh token
        /// </summary>
        /// <param name="userId">User's ID</param>
        /// <param name="refreshToken">Refresh token</param>
        /// <param name="expiryDate">Refresh token expiry date</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> UpdateRefreshTokenAsync(string userId, string refreshToken, DateTime expiryDate);

        /// <summary>
        /// Validates a refresh token
        /// </summary>
        /// <param name="userId">User's ID</param>
        /// <param name="refreshToken">Refresh token to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken);

        /// <summary>
        /// Finds a user by their refresh token value
        /// </summary>
        /// <param name="refreshToken">Refresh token value</param>
        /// <returns>The user if found, null otherwise</returns>
        Task<User?> GetUserByRefreshTokenAsync(string refreshToken);

        /// <summary>
        /// Revokes a user's refresh token (clears it from the database)
        /// </summary>
        /// <param name="userId">User's ID</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> RevokeRefreshTokenAsync(string userId);

        /// <summary>
        /// Gets a user by their ID
        /// </summary>
        /// <param name="userId">User's ID</param>
        /// <returns>The user if found, null otherwise</returns>
        Task<User?> GetUserByIdAsync(string userId);

        /// <summary>
        /// Gets all targets for a user
        /// </summary>
        /// <param name="userId">User's ID</param>
        /// <returns>List of targets</returns>
        Task<List<Target>> GetUserTargetsAsync(string userId);

        /// <summary>
        /// Adds a new target for a user
        /// </summary>
        /// <param name="userId">User's ID</param>
        /// <param name="target">Target to add</param>
        /// <returns>The created target</returns>
        Task<Target> AddUserTargetAsync(string userId, Target target);

        /// <summary>
        /// Updates a user's target
        /// </summary>
        /// <param name="userId">User's ID</param>
        /// <param name="targetId">Target ID</param>
        /// <param name="target">Updated target</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> UpdateUserTargetAsync(string userId, string targetId, Target target);

        /// <summary>
        /// Removes a target from a user
        /// </summary>
        /// <param name="userId">User's ID</param>
        /// <param name="targetId">Target ID</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> RemoveUserTargetAsync(string userId, string targetId);
    }

    /// <summary>
    /// Implementation of IUserService using IUserRepository
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITargetService _targetService;
        private readonly IPasswordHasherService _passwordHasherService;

        /// <summary>
        /// Initializes a new instance of the UserService class
        /// </summary>
        /// <param name="userRepository">User repository for data access</param>
        /// <param name="targetService">Target service for target operations</param>
        /// <param name="passwordHasherService">Password hasher service</param>
        public UserService(IUserRepository userRepository, ITargetService targetService, IPasswordHasherService passwordHasherService)
        {
            _userRepository = userRepository;
            _targetService = targetService;
            _passwordHasherService = passwordHasherService;
        }

        /// <summary>
        /// Creates a new user
        /// </summary>
        /// <param name="email">User's email</param>
        /// <param name="password">User's password</param>
        /// <param name="fullName">User's full name</param>
        /// <returns>The created user</returns>
        public async Task<User> CreateUserAsync(string email, string password, string fullName)
        {
            var user = new User
            {
                Email = email,
                PasswordHash = _passwordHasherService.HashPassword(password),
                FullName = fullName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Targets = new List<Target>(),
                TargetIds = new List<string>()
            };

            return await _userRepository.CreateAsync(user);
        }

        /// <summary>
        /// Gets a user by email
        /// </summary>
        /// <param name="email">User's email</param>
        /// <returns>The user if found, null otherwise</returns>
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _userRepository.FindByEmailAsync(email);
        }

        /// <summary>
        /// Updates a user's refresh token
        /// </summary>
        /// <param name="userId">User's ID</param>
        /// <param name="refreshToken">Refresh token</param>
        /// <param name="expiryDate">Refresh token expiry date</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> UpdateRefreshTokenAsync(string userId, string refreshToken, DateTime expiryDate)
        {
            return await _userRepository.StoreRefreshTokenAsync(userId, refreshToken, expiryDate);
        }

        /// <summary>
        /// Validates a refresh token
        /// </summary>
        /// <param name="userId">User's ID</param>
        /// <param name="refreshToken">Refresh token to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public async Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken)
        {
            return await _userRepository.ValidateRefreshTokenAsync(userId, refreshToken);
        }

        /// <summary>
        /// Finds a user by their refresh token value
        /// </summary>
        /// <param name="refreshToken">Refresh token value</param>
        /// <returns>The user if found, null otherwise</returns>
        public async Task<User?> GetUserByRefreshTokenAsync(string refreshToken)
        {
            // Walk all users looking for a matching non-expired refresh token
            // For production, consider adding an index on refresh_token field
            return await _userRepository.FindByRefreshTokenAsync(refreshToken);
        }

        /// <summary>
        /// Revokes a user's refresh token (clears it from the database)
        /// </summary>
        /// <param name="userId">User's ID</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> RevokeRefreshTokenAsync(string userId)
        {
            return await _userRepository.RevokeRefreshTokenAsync(userId);
        }

        /// <summary>
        /// Gets a user by their ID
        /// </summary>
        /// <param name="userId">User's ID</param>
        /// <returns>The user if found, null otherwise</returns>
        public async Task<User?> GetUserByIdAsync(string userId)
        {
            return await _userRepository.FindByIdAsync(userId);
        }

        /// <summary>
        /// Gets all targets for a user
        /// </summary>
        /// <param name="userId">User's ID</param>
        /// <returns>List of targets</returns>
        public async Task<List<Target>> GetUserTargetsAsync(string userId)
        {
            return await _targetService.GetUserTargetsAsync(userId);
        }

        /// <summary>
        /// Adds a new target for a user
        /// </summary>
        /// <param name="userId">User's ID</param>
        /// <param name="target">Target to add</param>
        /// <returns>The created target</returns>
        public async Task<Target> AddUserTargetAsync(string userId, Target target)
        {
            // Ensure the target belongs to the user
            target.UserId = userId;
            target.CreatedAt = DateTime.UtcNow;
            target.UpdatedAt = DateTime.UtcNow;

            // Create the target through TargetService using the simplified DTO
            return await _targetService.CreateTargetAsync(userId, new CreateTargetRequestDto
            {
                Title = target.Title,
                Description = target.Description,
                Duration = target.Duration > 0 ? target.Duration : null,
                DurationUnit = !string.IsNullOrWhiteSpace(target.DurationUnit) ? target.DurationUnit : null
            });
        }

        /// <summary>
        /// Updates a user's target
        /// </summary>
        /// <param name="userId">User's ID</param>
        /// <param name="targetId">Target ID</param>
        /// <param name="target">Updated target</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> UpdateUserTargetAsync(string userId, string targetId, Target target)
        {
            // Update the target through TargetService
            try
            {
                var updateDto = new UpdateTargetRequestDto
                {
                    Title = target.Title,
                    Description = target.Description,
                    Duration = target.Duration > 0 ? target.Duration : null,
                    DurationUnit = !string.IsNullOrWhiteSpace(target.DurationUnit) ? target.DurationUnit : null,
                    Status = !string.IsNullOrWhiteSpace(target.Status) ? target.Status : null
                };

                await _targetService.UpdateTargetAsync(targetId, updateDto);
                return true;
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
        }

        /// <summary>
        /// Removes a target from a user
        /// </summary>
        /// <param name="userId">User's ID</param>
        /// <param name="targetId">Target ID</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> RemoveUserTargetAsync(string userId, string targetId)
        {
            return await _targetService.DeleteTargetAsync(targetId, userId);
        }
    }
}