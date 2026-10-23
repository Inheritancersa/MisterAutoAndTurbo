using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MisterAutoAndTurbo.Web.Data;

namespace MisterAutoAndTurbo.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Npgsql 6+ rejects DateTime values of Kind=Local (e.g. DateTime.Today) for "timestamptz".
            // Legacy behaviour maps DateTime to "timestamp without time zone", which the app's dates expect.
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(
                    builder.Configuration.GetConnectionString("Supabase"),
                    npgsql =>
                    {
                        npgsql.CommandTimeout(30);
                        npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(2), null);
                    })
                // Workshop data is small; skip expensive change-detection scans where we control the tracker.
                .EnableSensitiveDataLogging(false)
                .EnableDetailedErrors(builder.Environment.IsDevelopment()));

            // Add services to the container.
            builder.Services.AddControllersWithViews(options =>
            {
                options.Filters.AddService<PersistChangesFilter>();
            });
            builder.Services.AddScoped<PersistChangesFilter>();
            builder.Services.AddSingleton<MisterAutoAndTurbo.Web.Services.IEmailService, MisterAutoAndTurbo.Web.Services.SmtpEmailService>();
            builder.Services.AddSingleton<MisterAutoAndTurbo.Web.Services.PasswordResetService>();

            // Cookie authentication (built on Microsoft.AspNetCore.Authentication.Core)
            builder.Services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                    options.SlidingExpiration = true;
                    options.Cookie.Name = "MisterAuto.Auth";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                });

            builder.Services.AddAuthorization(options =>
            {
                // Require authentication by default; Manager has full access via role claims.
                // Limited roles (Receptionist, Mechanic) are enforced with [Authorize(Roles = "...")] on controllers.
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

            var app = builder.Build();

            // Create tables / seed on first run, otherwise load saved data from Supabase.
            using (var scope = app.Services.CreateScope())
            {
                AppDataStore.Initialize(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
