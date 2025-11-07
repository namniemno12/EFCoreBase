using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using MyProject.Application.Common.Mapping;
using MyProject.Application.Services;
using MyProject.Application.Services.Interfaces;
using MyProject.Helper.ModelHelps;
using MyProject.Helper.Utils;
using MyProject.Helper.Utils.Interfaces;
using MyProject.Infrastructure;
using MyProject.Infrastructure.Persistence.HandleContext;
using MyProject.Infrastructure.Repository;
using System.Reflection;
using System.Security.Claims;

namespace ResfulAPI.Extensions
{
    public static class ServiceExtensions
    {
        /// <summary>
        /// Cấu hình DbContext + Audit Interceptor
        /// </summary>
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("Default");

            // Đăng ký AuditSaveChangesInterceptor (theo dõi CreatedBy, ModifiedBy, ...)
            services.AddScoped<AuditSaveChangesInterceptor>(sp =>
            {
                var accessor = sp.GetRequiredService<IHttpContextAccessor>();

                return new AuditSaveChangesInterceptor(() =>
                {
                    var user = accessor.HttpContext?.User;
                    if (user?.Identity?.IsAuthenticated != true) return (Guid?)null;

                    var idStr = user.FindFirstValue(ClaimTypes.NameIdentifier)
                                ?? user.FindFirstValue("sub")
                                ?? user.FindFirstValue("uid");

                    return Guid.TryParse(idStr, out var id) ? id : (Guid?)null;
                });
            });

            // DbContext chính
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.UseSqlServer(connectionString);
                // Nếu cần bật audit thì bỏ comment
                 options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
            });

            return services;
        }

        /// <summary>
        /// Các service chung: Jwt, HttpClient, Cache...
        /// </summary>
        public static IServiceCollection AddServiceContext(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
            var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>();

            services.AddControllersWithViews();
            services.AddAutoMapper(typeof(MappingProfile));
            services.AddHttpClient();
            //services.AddAuth(jwtSettings);
            services.AddDistributedMemoryCache();

            return services;
        }

        /// <summary>
        /// Đăng ký Service, Repository, UnitOfWork
        /// </summary>
        public static IServiceCollection AddCoreService(this IServiceCollection services)
        {
            // Application Services
            services.AddTransient<ITesttiepService, TesttiepService>();
            services.AddTransient<INguyenService, NguyenService>();
            services.AddTransient<ITokenUtils, TokenUtils>();

            // Repository + UoW
            services.AddScoped<DbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
            services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
            services.AddScoped(typeof(IUnitOfWork<>), typeof(UnitOfWork<>));
            services.AddScoped(typeof(IRepositoryAsync<>), typeof(SingleDbRepositoryAsync<>));

            // Session
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromDays(8);
            });

            // Singleton
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            //services.AddSingleton<ISessionService, SessionService>();
            //services.AddSingleton<IRedisService, RedisService>();

            return services;
        }

        /// <summary>
        /// Cấu hình CORS
        /// </summary>
        public static IServiceCollection AddCORS(this IServiceCollection services, string name)
        {
            services.AddCors(builder =>
            {
                builder.AddPolicy(
                    name: name,
                    policy =>
                    {
                        policy.AllowAnyHeader();
                        policy.AllowAnyMethod();
                        policy.AllowAnyOrigin();
                    }
                );
            });

            return services;
        }

        /// <summary>
        /// FluentValidation
        /// </summary>
        public static IServiceCollection AddCoreExtention(this IServiceCollection services)
        {
            services.AddFluentValidationAutoValidation()
                .AddFluentValidationClientsideAdapters();

            var assemblies = new List<Assembly>
            {
                Assembly.GetExecutingAssembly(),
                Assembly.Load("MyProject.Domain"),
                Assembly.Load("MyProject.Application")
            };

            services.AddValidatorsFromAssemblies(assemblies);
            return services;
        }
    }
}
