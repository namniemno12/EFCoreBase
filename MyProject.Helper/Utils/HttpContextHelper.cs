using Microsoft.AspNetCore.Http;

namespace MyProject.Helper.Utils
{
    public static class HttpContextHelper
    {
        private static IHttpContextAccessor _httpContextAccessor;

        public static void Configure(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public static Guid GetUserId()
        {
            if (_httpContextAccessor?.HttpContext == null)
            {
                return Guid.Empty;
            }

            return (Guid)(_httpContextAccessor.HttpContext.Items["UserId"] ?? Guid.Empty);
        }
    }
}
