using MyProject.Domain.DTOs.Auth.Req;
using MyProject.Domain.DTOs.Auth.Res;
using MyProject.Helper.Utils;

namespace MyProject.Application.Services.Interfaces
{
    public interface IAuthServices
    {
        Task<CommonResponse<string>> Register(RegisterReq req);
        Task<CommonResponse<string>> AddLoginHistory(AddLoginHistoryReq req);
        Task<CommonResponse<GetLoginRequestRes>> AddLoginRequest(AddLoginRequestReq req);
        Task<CommonResponse<LoginCmsResponse>> LoginByAdmin(LoginCmsRequest req);
        Task<CommonPagination<List<GetLoginRequestRes>>> GetLoginRequest(int? Status, string? UserName, int CurrentPage, int RecordPerPage);
        Task<CommonResponse<string>> AcceptLoginRequest(Guid AdminId, AcceptLoginRequestReq req);
    }
}
