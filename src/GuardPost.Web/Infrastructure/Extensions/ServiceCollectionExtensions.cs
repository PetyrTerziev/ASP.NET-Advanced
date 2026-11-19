using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using GuardPost.Common.Providers;
using GuardPost.Data;
using GuardPost.Data.Models;
using GuardPost.Services.Core;
using GuardPost.Services.Core.Contracts;
using GuardPost.Web.Infrastructure.Notifications;

using static GuardPost.Common.EntityValidationConstants.User;

namespace GuardPost.Web.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<GuardPostDbContext>(options => options.UseSqlServer(connectionString));

        return services;
    }

    /// <summary>ASP.NET Core Identity with roles, stored in the application database.</summary>
    public static IServiceCollection AddApplicationIdentity(this IServiceCollection services)
    {
        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = PasswordMinLength;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddEntityFrameworkStores<GuardPostDbContext>()
            .AddDefaultTokenProviders()
            .AddErrorDescriber<BulgarianIdentityErrorDescriber>();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.Cookie.HttpOnly = true;
            options.SlidingExpiration = true;
        });

        return services;
    }

    /// <summary>Registers the business services – controllers depend only on the interfaces.</summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddScoped<ISiteService, SiteService>();
        services.AddScoped<IGuardService, GuardService>();
        services.AddScoped<IShiftService, ShiftService>();
        services.AddScoped<IIncidentService, IncidentService>();
        services.AddScoped<IIncidentUpdateService, IncidentUpdateService>();
        services.AddScoped<ILeaveRequestService, LeaveRequestService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<IDashboardService, DashboardService>();

        services.AddScoped<INotificationService, SignalRNotificationService>();

        return services;
    }
}
