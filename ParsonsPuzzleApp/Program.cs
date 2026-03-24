using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ParsonsPuzzleApp.Data;
using ParsonsPuzzleApp.Interfaces;
using ParsonsPuzzleApp.Models;
using ParsonsPuzzleApp.Services;

namespace ParsonsPuzzleApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(connectionString));

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<ApplicationDbContext>();

            builder.Services.Configure<IdentityOptions>(options =>
            {
                // Деактивиране на изискването за потвърден имейл
                options.SignIn.RequireConfirmedAccount = false;
                options.SignIn.RequireConfirmedEmail = false;

                // Облекчаване на изискванията за паролата
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6; // Минимална дължина, например 6 символа
                options.Password.RequiredUniqueChars = 1;
            });

            // Configure session for bundle access control
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(24);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                // Use None for LTI 1.3 support - cross-site POSTs need cookies
                // This requires Secure=true (HTTPS)
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });

            builder.Services.AddHttpContextAccessor();

            // Disable X-Frame-Options: SAMEORIGIN so LMS platforms can embed the app in an iframe
            builder.Services.AddAntiforgery(options =>
            {
                options.SuppressXFrameOptionsHeader = true;
            });

            builder.Services.AddRazorPages();
            builder.Services.AddControllers();

            builder.Services.AddScoped<IPuzzleBlockService, PuzzleBlockService>();
            builder.Services.AddScoped<IMultilineBlockParser, MultilineBlockParser>();
            builder.Services.AddScoped<IBundleAccessService, BundleAccessService>();
            builder.Services.AddScoped<IHtmlSanitizerService, HtmlSanitizerService>();
            builder.Services.AddScoped<IPuzzleSolutionService, PuzzleSolutionService>();
            builder.Services.AddScoped<ILanguageService, LanguageService>();
            builder.Services.AddScoped<ILanguageCategoryService, LanguageCategoryService>();
            builder.Services.AddScoped<IBundleAnalysisService, BundleAnalysisService>();

            // Memory cache for JWKS caching
            builder.Services.AddMemoryCache();

            // LTI 1.3 configuration
            builder.Services.Configure<LtiOptions>(builder.Configuration.GetSection("Lti"));
            builder.Services.AddHttpClient("LtiPlatform", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            }).ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler();
                // For development with self-signed certificates
                if (builder.Environment.IsDevelopment())
                {
                    handler.ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                }
                return handler;
            });
            builder.Services.AddSingleton<ILtiKeyProvider, LtiKeyProvider>();
            builder.Services.AddScoped<ILtiService, LtiService>();
            builder.Services.AddHostedService<LtiStateCleanupService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            // Allow LMS platforms to embed this app in an iframe via Content-Security-Policy
            app.Use(async (context, next) =>
            {
                context.Response.Headers["Content-Security-Policy"] = "frame-ancestors 'self' *";
                await next();
            });

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // Add session middleware before endpoints
            app.UseSession();

            app.MapRazorPages();
            app.MapControllers();

            app.Run();
        }
    }
}