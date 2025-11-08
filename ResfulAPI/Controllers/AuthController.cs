using Microsoft.AspNetCore.Mvc;
using MyProject.Application.Services;
using MyProject.Application.Services.Interfaces;
using MyProject.Application.TcpSocket.Interfaces;
using MyProject.Domain.DTOs.Auth.Req;
using MyProject.Domain.DTOs.Auth.Res;
using MyProject.Helper.Utils;
using ResfulAPI.Extensions;

namespace ResfulAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthServices _authServices;
        public AuthController(IAuthServices authServices)
        {
            _authServices = authServices;
        }

        [HttpPost("login")]
        public async Task<CommonResponse<string>> Login([FromBody] LoginCmsRequest request)
        {
            var result = await _authServices.LoginByUser(request);
            return result;
        }
        [HttpPost]
        [Route("Register")]
        public async Task<CommonResponse<string>> Register(RegisterReq req)
        {
            var result = await _authServices.Register(req);
            return result;
        }
        [HttpGet]
        [Route("GetListLoginRequest")]
        [BAuthorize]
        public async Task<CommonPagination<List<GetLoginRequestRes>>> GetLoginRequest(int? Status, string? UserName, int CurrentPage, int RecordPerPage)
        {
            var result = await _authServices.GetLoginRequest(Status, UserName, CurrentPage, RecordPerPage);
            return result;
        }
        [HttpPost]
        [Route("AcceptLoginRequest")]
        [BAuthorize]
        public async Task<CommonResponse<string>> AcceptLoginRequest(AcceptLoginRequestReq req)
        {
            var userId = HttpContextHelper.GetUserId();
            var result = await _authServices.AcceptLoginRequest(userId, req);
            return result;
        }
        [HttpPost]
        [Route("AdminLogin")]
        public async Task<CommonResponse<LoginCmsResponse>> LoginByAdmin(LoginCmsRequest req)
        {
            var result = await _authServices.LoginByAdmin(req);
            return result;
        }
    }
}