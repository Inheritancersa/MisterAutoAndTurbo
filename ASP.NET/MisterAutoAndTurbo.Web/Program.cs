using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MisterAutoAndTurbo.Web.Data;

namespace MisterAutoAndTurbo.Web
{
    /// <summary>
    /// Application entry point and composition root.
    /// Configures the database, MVC, cookie authentication, authorization and the request pipeline, then starts the web server.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Builds and runs the web application.
        /// Step 1: set Npgsql switches. Step 2: register services (database, filters, email, password reset, auth). Step 3: build the app and load data into memory. Step 4: configure middleware and routing, then run.
        /// </summary>
        public static void Main(string[] args)
        {
            // Npgsql 6+ rejects DateTime values of Kind=Local (e.g. DateTime.Today) for "timestamptz".
            // Legacy behaviour maps DateTime to "timestamp without time zone", which the app's dates expect.
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);

            var builder = WebApplication.CreateBuilder(args);

            // Register the EF Core database context against the Supabase PostgreSQL connection string.
            // A 30 second command timeout and up to 3 automatic retries protect against slow or dropped connections.
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

            // Register MVC controllers and views. PersistChangesFilter runs around every action to sync data with the database.
            // Add services to the container.
            builder.Services.AddControllersWithViews(options =>
            {
                options.Filters.AddService<PersistChangesFilter>();
            });
            // Filter is scoped per request; the email service and reset-code store are singletons so they live for the whole app lifetime.
            builder.Services.AddScoped<PersistChangesFilter>();
            builder.Services.AddSingleton<MisterAutoAndTurbo.Web.Services.IEmailService, MisterAutoAndTurbo.Web.Services.SmtpEmailService>();
            builder.Services.AddSingleton<MisterAutoAndTurbo.Web.Services.PasswordResetService>();

            // Cookie authentication: users are sent to the login page when not signed in, and the cookie lasts 8 hours (sliding).
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

            // Authorization: every endpoint requires a signed-in user unless it is marked [AllowAnonymous].
            builder.Services.AddAuthorization(options =>
            {
                // Require authentication by default; Manager has full access via role claims.
                // Limited roles (Receptionist, Mechanic) are enforced with [Authorize(Roles = "...")] on controllers.
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

            var app = builder.Build();

            // On startup, create tables and seed data on first run, otherwise load the saved data from Supabase into the in-memory store.
            // Create tables / seed on first run, otherwise load saved data from Supabase.
            using (var scope = app.Services.CreateScope())
            {
                AppDataStore.Initialize(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
            }

            // Request pipeline (order matters): error handling, HTTPS, static files, routing, authentication, then authorization.
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

            // Default route: /Controller/Action/Id. Home/Index is used when nothing is specified.
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
