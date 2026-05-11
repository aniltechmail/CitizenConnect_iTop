using Core.DTOs.Auth;
using Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService) => _authService = authService;

        [HttpPost("citizen/register")]
        public async Task<IActionResult> RegisterCitizen([FromBody] CitizenRegisterDto dto)
        {
            try
            {
                var result = await _authService.RegisterCitizenAsync(dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPost("citizen/login")]
        public async Task<IActionResult> LoginCitizen([FromBody] LoginDto dto)
        {
            try
            {
                var result = await _authService.LoginCitizenAsync(dto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("internal/login")]
        public async Task<IActionResult> LoginInternal([FromBody] LoginDto dto)
        {
            try
            {
                var result = await _authService.LoginInternalUserAsync(dto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }
    }
}
