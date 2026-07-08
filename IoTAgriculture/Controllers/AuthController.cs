using IoTAgriculture.DTOs.Auth;
using IoTAgriculture.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IoTAgriculture.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
        {
            try
            {
                var result = await _authService.RegisterAsync(dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            var result = await _authService.LoginAsync(dto);
            if (result == null)
            {
                return Unauthorized(new { message = "Phone number or password is incorrect" });
            }

            return Ok(result);
        }

        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var token = ReadBearerToken();
            var profile = await _authService.GetProfileAsync(token);
            if (profile == null)
            {
                return Unauthorized();
            }

            return Ok(profile);
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequestDto dto)
        {
            try
            {
                var profile = await _authService.UpdateProfileAsync(ReadBearerToken(), dto);
                if (profile == null)
                {
                    return Unauthorized();
                }

                return Ok(profile);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("account")]
        public async Task<IActionResult> Account()
        {
            var summary = await _authService.GetAccountSummaryAsync(ReadBearerToken());
            if (summary == null)
            {
                return Unauthorized();
            }

            return Ok(summary);
        }

        [HttpGet("activities")]
        public async Task<IActionResult> Activities([FromQuery] int limit = 50)
        {
            var profile = await _authService.GetProfileAsync(ReadBearerToken());
            if (profile == null)
            {
                return Unauthorized();
            }

            return Ok(await _authService.GetActivitiesAsync(ReadBearerToken(), limit));
        }

        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto dto)
        {
            try
            {
                var changed = await _authService.ChangePasswordAsync(ReadBearerToken(), dto);
                if (!changed)
                {
                    return BadRequest(new { message = "Current password is incorrect" });
                }

                return Ok(new { message = "Password changed" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync(ReadBearerToken());
            return Ok(new { message = "Logged out" });
        }

        private string ReadBearerToken()
        {
            var header = Request.Headers.Authorization.ToString();
            return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? header["Bearer ".Length..].Trim()
                : string.Empty;
        }
    }
}
