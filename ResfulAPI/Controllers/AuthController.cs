using Microsoft.AspNetCore.Mvc;
using ResfulAPI.Services;

namespace ResfulAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ITcpClientService _tcpClientService;

        public AuthController(ITcpClientService tcpClientService)
        {
            _tcpClientService = tcpClientService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var response = await _tcpClientService.SendLoginRequest(request.Username, request.Password);
                var parts = response.Split('|');

                if (parts[0] == "success")
                {
                    return Ok(new { message = parts[1] });
                }

                return BadRequest(new { message = parts[1] });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        public class LoginRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }
    }
}