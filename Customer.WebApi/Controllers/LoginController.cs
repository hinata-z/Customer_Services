using Customer.WebApi.Request;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Customer.WebApi.Controllers
{
    /// <summary>
    /// Authentication controller for handling user login and JWT token generation
    /// </summary>
    [ApiController]
    [Route("/api/v1/[controller]")] // Simplify route (avoid duplicate action route conflicts)
    [Produces("application/json")] // Specify default response format
    public class LoginController : ControllerBase // Inherit ControllerBase for IActionResult support
    {
        // Inject configuration service (readonly to prevent accidental modification)
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Constructor for dependency injection
        /// </summary>
        /// <param name="configuration">Configuration provider for JWT settings</param>
        public LoginController(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration),
                "Configuration service cannot be null");
        }

        /// <summary>
        /// User login interface to get JWT token
        /// </summary>
        /// <param name="request">Login request parameters (username/password)</param>
        /// <returns>JWT token or authentication failure message</returns>
        /// <response code="200">Login success, return JWT token</response>
        /// <response code="400">Invalid request parameters</response>
        /// <response code="401">Invalid username/password</response>
        /// <response code="500">Server internal error</response>
        [HttpPost("login")] // Standard route: /api/v1/Login/login
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult Login([FromBody, Required] LoginRequest request)
        {
            // Step 1: Validate request parameters (automatically triggered by [ApiController], but explicit check for clarity)
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new
                {
                    Code = 400,
                    Message = "Invalid request parameters",
                    Details = errors
                });
            }

            try
            {
                // Step 2: Simulate user authentication (replace with database query in production)
                if (!ValidateCredentials(request.Username, request.Password))
                {
                    // Return standardized 401 response
                    return Unauthorized(new
                    {
                        Code = 401,
                        Message = "Invalid username or password",
                        Timestamp = DateTime.UtcNow
                    });
                }

                // Step 3: Generate JWT token
                var token = GenerateJwtToken(request.Username);

                // Step 4: Return success response with standardized format
                return Ok(new
                {
                    Code = 200,
                    Message = "Login successful",
                    Data = new { Token = token, ExpiresIn = 1800 }, // ExpiresIn: 30 minutes = 1800 seconds
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                // Step 5: Handle unexpected exceptions (log in production environment)
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Code = 500,
                    Message = "Internal server error",
                    Details = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Validate user credentials (simulated logic, replace with DB/identity service in production)
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password (should be hashed in production)</param>
        /// <returns>True if credentials are valid</returns>
        private bool ValidateCredentials(string username, string password)
        {
            // TODO: Replace with real authentication logic (e.g., query database, verify hashed password)
            // Production note: Never store plain text passwords! Use BCrypt/MD5/SHA256 hashing
            return username == "admin" && password == "password";
        }

        /// <summary>
        /// Generate JWT token with configured settings
        /// </summary>
        /// <param name="username">Authenticated username</param>
        /// <returns>Serialized JWT token string</returns>
        /// <exception cref="InvalidOperationException">Thrown when JWT configuration is missing</exception>
        private string GenerateJwtToken(string username)
        {
            // Get JWT configuration from appsettings.json (with null check)
            var jwtKey = _configuration["Jwt:Key"];
            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];

            // Validate JWT configuration
            if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
            {
                throw new InvalidOperationException("JWT configuration is incomplete (check appsettings.json)");
            }
            // Create claims (add more claims like role/ID in production)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Unique token ID
                new Claim(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString()) // Token issuance time
            };

            // Create symmetric security key
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Create JWT token
            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30), // Use UTC time to avoid time zone issues
                signingCredentials: signingCredentials
            );

            // Serialize token to string
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}