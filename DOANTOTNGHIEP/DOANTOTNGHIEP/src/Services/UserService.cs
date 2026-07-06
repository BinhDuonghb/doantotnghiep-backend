using DOANTOTNGHIEP.src.Data;
using DOANTOTNGHIEP.src.Models;
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
    }

    /// <summary>
    /// Implementation of IUserService using IUserRepository
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasherService _passwordHasherService;

        /// <summary>
        /// Initializes a new instance of the UserService class
        /// </summary>
        /// <param name="userRepository">User repository for data access</param>
        /// <param name="passwordHasherService">Password hasher service</param>
        public UserService(IUserRepository userRepository, IPasswordHasherService passwordHasherService)
        {
            _userRepository = userRepository;
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
                UpdatedAt = DateTime.UtcNow
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
    }
}