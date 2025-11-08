using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MyProject.Domain.DTOs.Auth.Req;
using MyProject.Helper.Constants.Globals;
using MyProject.Helper.Utils;
using MyProject.Helper.Utils.Interfaces;
using System.Text;

namespace ResfulAPI.Extensions
{
    public class BAuthorizeAttribute : TypeFilterAttribute
    {

        public BAuthorizeAttribute() : base(typeof(AuthorizeAttributeImpl))
        {

        }

        private class AuthorizeAttributeImpl : Attribute, IActionFilter, IAsyncActionFilter
        {
            private readonly ITokenUtils _ultils;

            private readonly string _langCode;

            public AuthorizeAttributeImpl(ITokenUtils ultils, IConfiguration configuration)
            {
                _langCode = configuration["ProjectSettings:LanguageCode"] ?? "vi";
                _ultils = ultils;
            }

            public void OnActionExecuting(ActionExecutingContext context)
            {

            }

            public void OnActionExecuted(ActionExecutedContext context)
            {
                // Do something after the action executes.
            }

            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                var check = CheckUserPemission(context);
                if (!check) return;

                await next();
            }

            #region Private method
            private bool CheckUserPemission(ActionExecutingContext context)
            {
                HttpRequest httpRequest = context.HttpContext.Request;
                string path = context.ActionDescriptor.AttributeRouteInfo.Template;
                string action = httpRequest.Method;

                var bearerToken = httpRequest.Headers["Authorization"];
                var token = !string.IsNullOrEmpty(bearerToken) ? bearerToken.ToString().Substring("bearer ".Length) : null;
                var userId = _ultils.ValidateToken(token);
                if (userId == null)
                {
                    var res = new CommonResponse<UserLoginResponse>();
                    res.ResponseCode = (int)ResponseCodeEnum.ERR_INVALID_TOKEN;
                    res.Message = ResourceUtil.GetMessage(res.ResponseCode, _langCode);
                    
                    context.Result = new UnauthorizedObjectResult(res);
                    return false;
                }

                context.HttpContext.Items["UserId"] = userId;

                return true;
            }
            #endregion
        }
    }

    public class DDOSCheckAttribute : TypeFilterAttribute
    {
        public DDOSCheckAttribute() : base(typeof(AuthorizeAttributeImpl))
        {

        }

        private class AuthorizeAttributeImpl : Attribute, IActionFilter, IAsyncActionFilter
        {

            private readonly string _langCode;
            private readonly string _apiKey;

            public AuthorizeAttributeImpl(IConfiguration configuration)
            {
                _langCode = configuration["ProjectSettings:LanguageCode"] ?? "vi";
                _apiKey = configuration["ProjectSettings:ApiKey"] ?? "6ca13d52";
            }

            public void OnActionExecuting(ActionExecutingContext context)
            {

            }

            public void OnActionExecuted(ActionExecutedContext context)
            {
                // Do something after the action executes.
            }

            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                var check = await CheckDDOS(context);
                if (!check) return;

                await next();
            }

            #region Private method
            private async Task<bool> CheckDDOS(ActionExecutingContext context)
            {
                HttpRequest httpRequest = context.HttpContext.Request;

                var sign = httpRequest.Headers["Sign"];

                // TO DO: bypass api test (Remove when deploy product)
                var requestTime = httpRequest.Headers["Request-Time"];
                if (sign == "5d28b9987aa2757ae162dee2a8315df1" && !string.IsNullOrEmpty(requestTime))
                    return true;

                string signCheck = await GenerateSignCheck(httpRequest);

                if (string.IsNullOrEmpty(sign) || string.IsNullOrEmpty(signCheck) || sign != signCheck)
                {
                    var res = new CommonResponse<dynamic>();
                    res.ResponseCode = (int)ResponseCodeEnum.ERR_DDOS;
                    res.Message = ResourceUtil.GetMessage(res.ResponseCode, _langCode);

                    context.Result = new UnauthorizedObjectResult(res);
                    return false;
                }

                return true;
            }
            private async Task<string> GenerateSignCheck(HttpRequest httpRequest)
            {
                try
                {
                    string path = httpRequest.Path;
                    string httpMethod = httpRequest.Method;
                    var requestTime = httpRequest.Headers["Request-Time"];

                    string contentMD5 = string.Empty;
                    if (httpMethod == HttpMethod.Get.ToString())
                    {
                        var queryParams = httpRequest.Query;
                        string paramData = BuildParamData(queryParams);
                        contentMD5 = EncryptUtil.GetMD5(StringUtil.MinifyBody(paramData));
                    }
                    else
                    {
                        string body = await ReadRequestBodyAsync(httpRequest);
                        contentMD5 = EncryptUtil.GetMD5(StringUtil.MinifyBody(body));
                    }

                    string stringToSign = $"{httpMethod}\n{path}\n{contentMD5}\n{requestTime}\n{_apiKey}";
                    return EncryptUtil.GetMD5(stringToSign);
                }
                catch (Exception)
                {
                    throw;
                }
            }
            private async Task<string> ReadRequestBodyAsync(HttpRequest httpRequest)
            {
                if (!httpRequest.Body.CanSeek)
                {
                    httpRequest.EnableBuffering();
                }

                httpRequest.Body.Position = 0;

                using (var reader = new StreamReader(httpRequest.Body, Encoding.UTF8, leaveOpen: true))
                {
                    var body = await reader.ReadToEndAsync();
                    httpRequest.Body.Position = 0;
                    return body;
                }
            }
            private string BuildParamData(IQueryCollection qr)
            {
                List<string> list = new List<string>();

                foreach (var kvp in qr)
                {
                    string key = kvp.Key;
                    string value = kvp.Value;
                    list.Add($"{key}={value}");
                }
                string rawData = string.Join("&", list.ToArray());

                return rawData;
            }
            #endregion
        }
    }
}
