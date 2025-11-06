using ResfulAPI.Extensions;
using Microsoft.OpenApi.Models;
using FluentValidation;
using FluentValidation.Results;
using Serilog;
using Microsoft.AspNetCore.WebSockets;
using ResfulAPI.Services;

var builder = WebApplication.CreateBuilder(args);

var builderLog = new ConfigurationBuilder()
       .SetBasePath(Directory.GetCurrentDirectory())
      .AddJsonFile("logsettings.json", optional: true, reloadOnChange: true);
IConfigurationRoot configuration = builderLog.Build();
Log.Logger = new LoggerConfiguration()
 .ReadFrom.Configuration(configuration)
    .CreateLogger();
builder.Host.UseSerilog();

var _services = builder.Services;
var _configuration = builder.Configuration;

// Add services to the container.
var MyAllowSpecificOrigins = "_myAllowOrigins";
_services.AddCoreExtention();
_services.AddDatabase(_configuration);
_services.AddServiceContext(_configuration);
_services.AddCORS(MyAllowSpecificOrigins);
_services.AddCoreService();
_services.AddMemoryCache();

// Register TcpClientService
_services.AddScoped<ITcpClientService, TcpClientService>();

// Configure WebSocket options
_services.AddWebSockets(options =>
{
    options.KeepAliveInterval = TimeSpan.FromMinutes(2);
    options.ReceiveBufferSize = 4 * 1024; // 4KB
});

_services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
    var errors = context.ModelState
       .Where(ms => ms.Value.Errors.Count > 0)
     .ToDictionary(
         ms => ms.Key,
          ms => ms.Value.Errors.Select(e => e.ErrorMessage).ToArray()
       );

      throw new ValidationException(
       errors.SelectMany(kv => kv.Value.Select(error => new ValidationFailure(kv.Key, error)))
      );
    };
});

_services.AddEndpointsApiExplorer();
_services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
   Version = "v1.0.0",
     Title = "My Project",
        Description = "My Project api documents"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
    In = ParameterLocation.Header,
     Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
  {
       new OpenApiSecurityScheme
         {
     Reference = new OpenApiReference
       {
     Type=ReferenceType.SecurityScheme,
         Id="Bearer"
       }
         },
       new string[]{}
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Enable CORS first
app.UseCors(MyAllowSpecificOrigins);

app.UseHttpsRedirection();
app.UseStaticFiles();

// Enable WebSockets
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
});

app.UseAuthorization();
app.UseMiddleware<ErrorHandlerMiddleware>();
app.UseMiddleware<ResfulAPI.Extensions.WebSocketMiddleware>();
app.MapControllers();

app.Run();
