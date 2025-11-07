using Microsoft.AspNetCore.Mvc;
using MyProject.Application.TcpSocket.Interfaces;

namespace ResfulAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ITcpSocketService _tcpSocketService;

        public AuthController(ITcpSocketService tcpSocketService)
        {
            _tcpSocketService = tcpSocketService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {

            var response = await _tcpSocketService.SendMessageAsync(request.Username);
            return Ok();
        }
        public class LoginRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }
    }
}