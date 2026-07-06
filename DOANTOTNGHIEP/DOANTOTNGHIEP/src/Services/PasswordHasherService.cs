using BCrypt.Net;

namespace DOANTOTNGHIEP.src.Services
{
    /// <summary>
    /// Service for password hashing and verification using BCrypt
    /// </summary>
    public interface IPasswordHasherService
    {
        /// <summary>
        /// Hashes a password using BCrypt
        /// </summary>
        /// <param name="password">The plain text password to hash</param>
        /// <returns>The hashed password</returns>
        string HashPassword(string password);

        /// <summary>
        /// Verifies a password against a hash
        /// </summary>
        /// <param name="password">The plain text password</param>
        /// <param name="hash">The hashed password</param>
        /// <returns>True if the password matches the hash, false otherwise</returns>
        bool VerifyPassword(string password, string hash);
    }

    /// <summary>
    /// Implementation of IPasswordHasherService using BCrypt.Net
    /// </summary>
    public class PasswordHasherService : IPasswordHasherService
    {
        /// <summary>
        /// Hashes a password using BCrypt
        /// </summary>
        /// <param name="password">The plain text password to hash</param>
        /// <returns>The hashed password</returns>
        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        /// <summary>
        /// Verifies a password against a hash
        /// </summary>
        /// <param name="password">The plain text password</param>
        /// <param name="hash">The hashed password</param>
        /// <returns>True if the password matches the hash, false otherwise</returns>
        public bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}