using Application.Services;
using Core.DTOs.Mobile;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Infrastructure.Data;
using Infrastructure.ITop;
using Infrastructure.Notifications;
using Infrastructure.Reporting;
using Infrastructure.Repositories;
using Infrastructure.Seeds;
using Infrastructure.Storage;
using API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var contentRoot = Directory.GetCurrentDirectory();
            var portalPath = Path.GetFullPath(Path.Combine(contentRoot, "..", "web-portal"));
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = args,
                ContentRootPath = contentRoot,
                WebRootPath = Directory.Exists(portalPath) ? portalPath : contentRoot
            });

            // Database
            builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Repositories
            builder.Services.AddScoped<ILocationRepository, LocationRepository>();
            builder.Services.AddScoped<ICitizenRepository, CitizenRepository>();
            builder.Services.AddScoped<IComplaintRepository, ComplaintRepository>();
            builder.Services.AddScoped<IInternalUserRepository, InternalUserRepository>();
            builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
            builder.Services.AddScoped<IEscalationRepository, EscalationRepository>();

            // Services
            builder.Services.AddScoped<ILocationService, LocationService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IComplaintService, ComplaintService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<IEscalationService, EscalationService>();
            builder.Services.AddScoped<IReportingService, ReportingService>();
            builder.Services.AddScoped<IStorageService, LocalStorageService>();
            builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();
            builder.Services.AddHttpClient<ISmsService, SmsService>();
            builder.Services.AddHttpClient<IITopTicketAdapter, ITopTicketAdapter>();
            builder.Services.AddHostedService<SlaEscalationBackgroundService>();
            builder.Services.Configure<SmsProviderOptions>(builder.Configuration.GetSection("SmsProvider"));
            builder.Services.Configure<OtpSettings>(builder.Configuration.GetSection("OtpSettings"));

            // JWT Authentication
            var jwtKey = builder.Configuration["Jwt:Key"]!;
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                    };
                });

            builder.Services.AddAuthorization();
            builder.Services.AddControllers();

            // Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "CitizenConnect API", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header. Enter: Bearer {token}",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                    { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
            });

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await db.Database.MigrateAsync();
                await DatabaseSeeder.SeedAsync(db);
            }

            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/api/v1", out var remaining))
                    context.Request.Path = $"/api{remaining}";
                await next();
            });
            app.UseAuthentication();
            app.UseAuthorization();

            // Serve uploaded files
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                    Path.Combine(Directory.GetCurrentDirectory(), "uploads")
                ),
                RequestPath = "/uploads"
            });

            app.MapControllers();
            app.MapFallbackToFile("index.html");
            await app.RunAsync();
        }
    }
}
