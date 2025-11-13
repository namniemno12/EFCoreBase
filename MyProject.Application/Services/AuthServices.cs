using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MyProject.Application.Services.Interfaces;
using MyProject.Domain.DTOs.Auth.Req;
using MyProject.Domain.DTOs.Auth.Res;
using MyProject.Domain.Entities;
using MyProject.Helper.Constants.Globals;
using MyProject.Helper.Utils;
using MyProject.Helper.Utils.Interfaces;
using MyProject.Infrastructure;
using Npgsql.Replication.PgOutput.Messages;

namespace MyProject.Application.Services
{
    public class AuthServices : IAuthServices
    {
        private readonly IRepositoryAsync<Users> _userRepository;
        private readonly IRepositoryAsync<Roles> _roleRepository;
        private readonly IRepositoryAsync<LoginHistory> _loginHistoryRepository;
        private readonly IRepositoryAsync<LoginRequest> _loginRequestRepository;
        private readonly CryptoHelperUtil _cryptoHelperUtil;
        private readonly IHttpContextAccessor? _httpContextAccessor;
        private readonly ITokenUtils _tokenUtils;

        /// <summary>
        /// Constructor for AuthServices with optional TCP Socket and HTTP Context
        /// - TCP Server: Only needs repositories, crypto, and token utils
        /// - Web API: Needs all dependencies including HTTP context and TCP socket
        /// </summary>
        public AuthServices(
          IRepositoryAsync<Users> userRepository,
     IRepositoryAsync<Roles> roleRepository,
 CryptoHelperUtil cryptoHelperUtil,
     IRepositoryAsync<LoginHistory> loginHistoryRepository,
            IRepositoryAsync<LoginRequest> loginRequestRepository,
        ITokenUtils tokenUtils,
            IHttpContextAccessor? httpContextAccessor = null)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _cryptoHelperUtil = cryptoHelperUtil;
            _loginHistoryRepository = loginHistoryRepository;
            _loginRequestRepository = loginRequestRepository;
            _httpContextAccessor = httpContextAccessor;
            _tokenUtils = tokenUtils;
        }

        /// <summary>
        /// Add Login History - Được gọi từ TCP Server khi admin approve
        /// </summary>
        public async Task<CommonResponse<string>> AddLoginHistory(AddLoginHistoryReq req)
        {
            if (req.UserId == Guid.Empty)
                return CommonResponse<string>.Fail(ResponseCodeEnum.ERR_WRONG_INPUT, "Thiếu UserId");

            var user = await _userRepository.AsQueryable().FirstOrDefaultAsync(u => u.Id == req.UserId);
            if (user == null)
                return CommonResponse<string>.Fail(ResponseCodeEnum.ERR_USER_NOT_EXIST, "User không tồn tại");

            var loginHistory = new LoginHistory
            {
                Id = Guid.NewGuid(),
                UserId = req.UserId,
                LoginTime = req.LoginTime,
                IpAddress = req.IpAddress,
                DeviceInfo = req.DeviceInfo,
                IsSuccessful = req.IsSuccessful
            };
            await _loginHistoryRepository.AddAsync(loginHistory);
            await _loginHistoryRepository.SaveChangesAsync();

            return CommonResponse<string>.Success(loginHistory.Id.ToString(), ResponseCodeEnum.SUCCESS, "Thêm lịch sử đăng nhập thành công");
        }

        /// <summary>
        /// Add Login Request - Được gọi từ TCP Server khi client gửi login request
        /// </summary>
        public async Task<CommonResponse<GetLoginRequestRes>> AddLoginRequest(AddLoginRequestReq req)
        {
            if (req.UserId == Guid.Empty)
                return CommonResponse<GetLoginRequestRes>.Fail(ResponseCodeEnum.ERR_WRONG_INPUT, "Thiếu UserId");

            var user = await _userRepository.AsQueryable()
      .FirstOrDefaultAsync(u => u.Id == req.UserId);
            if (user == null)
                return CommonResponse<GetLoginRequestRes>.Fail(ResponseCodeEnum.ERR_USER_NOT_EXIST, "User không tồn tại");

            var loginRequest = new LoginRequest
            {
                Id = Guid.NewGuid(),
                UserId = req.UserId,
                Status = req.Status,
                IpAddress = req.IpAddress,
                DeviceInfo = req.DeviceInfo,
                AdminNote = req.AdminNote,
                LoginHistoryId = req.LoginHistoryId,
                RequestedAt = req.RequestedAt
            };

            await _loginRequestRepository.AddAsync(loginRequest);
            await _loginRequestRepository.SaveChangesAsync();

            var res = new GetLoginRequestRes
            {
                LoginRequestId = loginRequest.Id,
                UserId = loginRequest.UserId,
                UserName = user.UserName,
                FullName = user.FullName,
                RequestedAt = loginRequest.RequestedAt,
                ReviewedAt = loginRequest.ReviewedAt,
                Status = loginRequest.Status,
                IpAddress = loginRequest.IpAddress,
                DeviceInfo = loginRequest.DeviceInfo,
                ReviewedByAdminId = loginRequest.ReviewedByAdminId,
                LoginHistoryId = loginRequest.LoginHistoryId
            };

            return CommonResponse<GetLoginRequestRes>.Success(res, ResponseCodeEnum.SUCCESS, "Thêm yêu cầu đăng nhập thành công");
        }

        private async Task<(CommonResponse<bool> response, Users? user)> CheckAcc(LoginCmsRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.UserName) || string.IsNullOrWhiteSpace(req.Password))
                return (CommonResponse<bool>.Fail(ResponseCodeEnum.ERR_WRONG_INPUT, "Thiếu thông tin đăng nhập"), null);

            var userList = await _userRepository.AsQueryable()
           .Where(x => x.UserName == req.UserName)
         .ToListAsync();

            var user = userList.FirstOrDefault(x => _cryptoHelperUtil.Decrypt(x.PasswordHash) == req.Password);

            if (user == null)
                return (CommonResponse<bool>.Fail(ResponseCodeEnum.ERR_WRONG_USERNAME_PASS, "Sai tài khoản hoặc mật khẩu"), null);

            if (!user.IsActive)
                return (CommonResponse<bool>.Fail(ResponseCodeEnum.ERR_BLOCK_LOGIN, "Tài khoản đã bị khóa"), null);

            return (CommonResponse<bool>.Success(true, ResponseCodeEnum.SUCCESS, "Đăng nhập thành công"), user);
        }

       

        public async Task<CommonResponse<string>> Register(RegisterReq req)
        {
            if (string.IsNullOrWhiteSpace(req.UserName) || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            {
                return CommonResponse<string>.Fail(ResponseCodeEnum.ERR_WRONG_INPUT, "Thiếu thông tin bắt buộc");
            }

            var existUser = await _userRepository.AsQueryable()
        .AnyAsync(u => u.UserName == req.UserName || u.Email == req.Email);
            if (existUser)
            {
                return CommonResponse<string>.Fail(ResponseCodeEnum.ERR_EXISTUSER, "Tài khoản hoặc email đã tồn tại");
            }

            var user = new Users
            {
                Id = Guid.NewGuid(),
                UserName = req.UserName,
                Email = req.Email,
                PasswordHash = _cryptoHelperUtil.Encrypt(req.Password),
                FullName = req.FullName,
                PhoneNumber = req.PhoneNumber,
                Address = req.Address,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                RoleId = await GetDefaultRoleIdAsync()
            };
            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            return CommonResponse<string>.Success(null, ResponseCodeEnum.SUCCESS, "Đăng ký thành công");
        }

        private async Task<Guid> GetDefaultRoleIdAsync()
        {
            var role = await _roleRepository.AsQueryable().FirstOrDefaultAsync(r => r.Name == "User");
            if (role != null) return role.Id;

            var newRole = new Roles
            {
                Id = Guid.NewGuid(),
                Name = "User",
                CreatedAt = DateTime.UtcNow
            };
            await _roleRepository.AddAsync(newRole);
            await _roleRepository.SaveChangesAsync();
            return newRole.Id;
        }

        /// <summary>
        /// Get Login Requests - Có thể gọi từ cả Web API và TCP Server
        /// </summary>
        public async Task<CommonPagination<List<GetLoginRequestRes>>> GetLoginRequest(int? Status, string? UserName, int CurrentPage, int RecordPerPage)
        {
            var query = from lr in _loginRequestRepository.AsQueryable()
                        join u in _userRepository.AsQueryable() on lr.UserId equals u.Id
                        select new GetLoginRequestRes
                        {
                            LoginRequestId = lr.Id,
                            UserId = lr.UserId,
                            UserName = u.UserName,
                            FullName = u.FullName,
                            RequestedAt = lr.RequestedAt,
                            ReviewedAt = lr.ReviewedAt,
                            Status = lr.Status,
                            IpAddress = lr.IpAddress,
                            DeviceInfo = lr.DeviceInfo,
                            ReviewedByAdminId = lr.ReviewedByAdminId,
                            LoginHistoryId = lr.LoginHistoryId
                        };

            if (Status.HasValue)
                query = query.Where(x => x.Status == Status.Value);

            if (!string.IsNullOrWhiteSpace(UserName))
                query = query.Where(x => x.UserName.Contains(UserName));

            query = query.OrderByDescending(x => x.RequestedAt);

            var totalCount = await query.CountAsync();

            var items = await query
      .Skip((CurrentPage - 1) * RecordPerPage)
 .Take(RecordPerPage)
  .ToListAsync();

            return new CommonPagination<List<GetLoginRequestRes>>
            {
                ResponseCode = (int)ResponseCodeEnum.SUCCESS,
                Message = "Lấy danh sách yêu cầu đăng nhập thành công",
                Data = items,
                TotalRecord = totalCount
            };
        }

        /// <summary>
        /// Accept/Reject Login Request - Được gọi từ TCP Server khi admin approve/reject
        /// KHÔNG gửi message qua TCP Socket vì TCP Server tự handle việc này
        /// </summary>
        public async Task<CommonResponse<string>> AcceptLoginRequest(Guid AdminId, AcceptLoginRequestReq req)
        {
            if (req.LoginRequestId == Guid.Empty)
                return CommonResponse<string>.Fail(ResponseCodeEnum.ERR_WRONG_INPUT, "Thiếu LoginRequestId");

            // Join giữa LoginRequest và User
            var loginRequestWithUser = await (from lr in _loginRequestRepository.AsQueryable()
                                              join u in _userRepository.AsQueryable() on lr.UserId equals u.Id
                                              where lr.Id == req.LoginRequestId
                                              select new
                                              {
                                                  LoginRequest = lr,
                                                  User = u
                                              }).FirstOrDefaultAsync();

            if (loginRequestWithUser == null)
                return CommonResponse<string>.Fail(ResponseCodeEnum.ERR_NOT_FOUND, "Login request không tồn tại");

            var loginRequest = loginRequestWithUser.LoginRequest;
            var user = loginRequestWithUser.User;

            // Kiểm tra trạng thái hiện tại
            if (loginRequest.Status != 0) // 0 = pending
                return CommonResponse<string>.Fail(ResponseCodeEnum.ERR_WRONG_INPUT, "Login request đã được xử lý");

            // Cập nhật trạng thái
            loginRequest.Status = req.Status;
            loginRequest.ReviewedByAdminId = AdminId;
            loginRequest.ReviewedAt = DateTime.UtcNow;

            await _loginRequestRepository.UpdateAsync(loginRequest);
            await _loginRequestRepository.SaveChangesAsync();

            // KHÔNG gửi message qua TCP Socket ở đây
            // TCP Server sẽ tự handle việc broadcast đến clients

            string statusText = req.Status == 1 ? "chấp nhận" : "từ chối";
            return CommonResponse<string>.Success(loginRequest.Id.ToString(), ResponseCodeEnum.SUCCESS, $"Login request đã được {statusText}");
        }

        /// <summary>
        /// Login By Admin - Dùng cho admin login vào CMS/Admin panel
        /// Không liên quan đến TCP Socket
        /// </summary>
        public async Task<CommonResponse<LoginCmsResponse>> LoginByAdmin(LoginCmsRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.UserName) || string.IsNullOrWhiteSpace(req.Password))
                return CommonResponse<LoginCmsResponse>.Fail(ResponseCodeEnum.ERR_WRONG_INPUT, "Thiếu thông tin đăng nhập");

            var userQuery = from u in _userRepository.AsQueryable()
                            join r in _roleRepository.AsQueryable() on u.RoleId equals r.Id
                            where u.UserName == req.UserName
                            select new
                            {
                                User = u,
                                RoleName = r.Name
                            };

            var userData = await userQuery.FirstOrDefaultAsync();

            if (userData == null)
                return CommonResponse<LoginCmsResponse>.Fail(ResponseCodeEnum.ERR_WRONG_USERNAME_PASS, "Sai tài khoản hoặc mật khẩu");

            // ✅ Check PasswordHash is not null or empty
            if (string.IsNullOrWhiteSpace(userData.User.PasswordHash))
                return CommonResponse<LoginCmsResponse>.Fail(ResponseCodeEnum.ERR_WRONG_USERNAME_PASS, "Sai tài khoản hoặc mật khẩu");

            string decryptedPassword;
            try
            {
                decryptedPassword = _cryptoHelperUtil.Decrypt(userData.User.PasswordHash);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to decrypt password: {ex.Message}");
                return CommonResponse<LoginCmsResponse>.Fail(ResponseCodeEnum.ERR_WRONG_USERNAME_PASS, "Sai tài khoản hoặc mật khẩu");
            }

            if (decryptedPassword != req.Password)
                return CommonResponse<LoginCmsResponse>.Fail(ResponseCodeEnum.ERR_WRONG_USERNAME_PASS, "Sai tài khoản hoặc mật khẩu");

            if (!userData.User.IsActive)
                return CommonResponse<LoginCmsResponse>.Fail(ResponseCodeEnum.ERR_BLOCK_LOGIN, "Tài khoản đã bị khóa");

            bool isAdmin = userData.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase);

            var accessToken = _tokenUtils.GenerateToken(userData.User.Id);
            var refreshToken = _tokenUtils.GenerateRefreshToken(userData.User.Id);

            var loginResponse = new LoginCmsResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AdminId = userData.User.Id
            };

            return CommonResponse<LoginCmsResponse>.Success(
                     loginResponse,
                         ResponseCodeEnum.SUCCESS,
              "Đăng nhập thành công"
                  );
        }
    }
}
