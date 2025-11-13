using System;
using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyProject.Application.Services;
using MyProject.Application.Services.Interfaces;
using MyProject.Domain.Entities;
using MyProject.Helper.Utils;
using MyProject.Helper.Utils.Interfaces;
using MyProject.Infrastructure;
using MyProject.Infrastructure.Persistence.HandleContext;

namespace AdminUI
{
    public partial class App : Application
    {
        public static ServiceProvider ServiceProvider { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                Console.WriteLine("🚀 App: OnStartup called");

                // ✅ Setup DI Container
                var services = new ServiceCollection();
                ConfigureServices(services);
                ServiceProvider = services.BuildServiceProvider();

                Console.WriteLine("✅ App: ServiceProvider built successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ App: Fatal exception in OnStartup!");
                Console.WriteLine($"❌ App: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"❌ App: Stack Trace:\n{ex.StackTrace}");

                MessageBox.Show(
                    $"Fatal error during startup:\n\n{ex.Message}",
                    "Fatal Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown();
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            Console.WriteLine("⚙️ App: Configuring services...");

            // Configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            services.AddSingleton<IConfiguration>(configuration);

            // DbContext
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // Register DbContext for RepositoryAsync
            services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

            // Repositories
            services.AddScoped<IRepositoryAsync<Users>, RepositoryAsync<Users>>();
            services.AddScoped<IRepositoryAsync<Roles>, RepositoryAsync<Roles>>();
            services.AddScoped<IRepositoryAsync<LoginHistory>, RepositoryAsync<LoginHistory>>();
            services.AddScoped<IRepositoryAsync<LoginRequest>, RepositoryAsync<LoginRequest>>();

            // JWT Settings
            services.Configure<MyProject.Helper.ModelHelps.JwtSettings>(
                configuration.GetSection("Jwt"));

            // Utilities
            services.AddSingleton<CryptoHelperUtil>();
            services.AddSingleton<ITokenUtils, TokenUtils>();

            // Services
            services.AddScoped<IAuthServices, AuthServices>();

            Console.WriteLine("✅ App: DI Container configured successfully");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            ServiceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}
