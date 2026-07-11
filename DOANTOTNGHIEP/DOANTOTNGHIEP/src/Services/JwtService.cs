using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DOANTOTNGHIEP.src.Services
{
    /// <summary>
    /// Service for JWT token generation and validation
    /// </summary>
    public interface IJwtService
    {
        /// <summary>
        /// Generates an access token for a user
        /// </summary>
        /// <param name="userId">User's ID</param>
        /// <param name="email">User's email</param>
        /// <returns>JWT access token</returns>
        string GenerateAccessToken(string userId, string email);

        /// <summary>
        /// Generates an access token for a user with a specific role
        /// </summary>
        /// <param name="userId">User's ID</param>
        /// <param name="email">User's email</param>
        /// <param name="role">User's role (e.g., "User", "Admin")</param>
        /// <returns>JWT access token with role claim</returns>
        string GenerateAccessToken(string userId, string email, string role);

        /// <summary>
        /// Generates a refresh token
        /// </summary>
        /// <returns>Refresh token</returns>
        string GenerateRefreshToken();

        /// <summary>
        /// Validates a JWT token
        /// </summary>
        /// <param name="token">JWT token to validate</param>
        /// <returns>Claims principal if valid, null otherwise</returns>
        ClaimsPrincipal? ValidateToken(string token);

        /// <summary>
        /// Gets the access token expiry time in minutes
        /// </summary>
        int AccessTokenExpiryMinutes { get; }

        /// <summary>
        /// Gets the refresh token expiry time in days
        /// </summary>
        int RefreshTokenExpiryDays { get; }
    }

    /// <summary>
    /// Implementation of IJwtService
    /// </summary>
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _accessTokenExpiryMinutes;
        private readonly int _refreshTokenExpiryDays;

        /// <summary>
        /// Initializes a new instance of the JwtService class
        /// </summary>
        /// <param name="configuration">Application configuration</param>
        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
            _secretKey = _configuration["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key is missing in configuration");
            _issuer = _configuration["Jwt:Issuer"] ?? throw new ArgumentNullException("Jwt:Issuer is missing in configuration");
            _audience = _configuration["Jwt:Audience"] ?? throw new ArgumentNullException("Jwt:Audience is missing in configuration");
            _accessTokenExpiryMinutes = int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "30");
            _refreshTokenExpiryDays = int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");
        }

        /// <summary>
        /// Gets the access token expiry time in minutes
        /// </summary>
        public int AccessTokenExpiryMinutes => _accessTokenExpiryMinutes;

        /// <summary>
        /// Gets the refresh token expiry time in days
        /// </summary>
        public int RefreshTokenExpiryDays => _refreshTokenExpiryDays;

        /// <summary>
        /// Generates an access token for a user
        /// </summary>
        /// <param name="userId">User's ID</param>
        /// <param name="email">User's email</param>
        /// <returns>JWT access token</returns>
        public string GenerateAccessToken(string userId, string email)
        {
            return GenerateAccessToken(userId, email, "User");
        }

        /// <summary>
        /// Generates an access token for a user with a specific role
        /// </summary>
        /// <param name="userId">User's ID</param>
        /// <param name="email">User's email</param>
        /// <param name="role">User's role (e.g., "User", "Admin")</param>
        /// <returns>JWT access token</returns>
        public string GenerateAccessToken(string userId, string email, string role)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Email, email),
                    new Claim(ClaimTypes.Role, role)
                }),
                Expires = DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes),
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// Generates a refresh token
        /// </summary>
        /// <returns>Refresh token</returns>
        public string GenerateRefreshToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }

        /// <summary>
        /// Validates a JWT token
        /// </summary>
        /// <param name="token">JWT token to validate</param>
        /// <returns>Claims principal if valid, null otherwise</returns>
        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_secretKey);
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                return tokenHandler.ValidateToken(token, validationParameters, out _);
            }
            catch
            {
                return null;
            }
        }
    }
}