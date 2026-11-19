using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using GuardPost.Common.Providers;
using GuardPost.Data;
using GuardPost.Data.Models;
using GuardPost.Data.Seeding;

namespace GuardPost.Web.Infrastructure.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations and (optionally) seeds demo data, roles and accounts.
    /// Controlled by "Database:ApplyMigrationsOnStartup" and "Database:SeedSampleData" in appsettings.
    /// </summary>
    public static async Task PrepareDatabaseAsync(this WebApplication app)
    {
        bool applyMigrations = app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", true);
        bool seedData = app.Configuration.GetValue("Database:SeedSampleData", true);

        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<GuardPostDbContext>();

        if (applyMigrations)
        {
            await dbContext.Database.MigrateAsync();
        }

        var clock = services.GetRequiredService<IDateTimeProvider>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        if (seedData)
        {
            await DbSeeder.SeedAsync(dbContext, clock.Today);
            await IdentitySeeder.SeedAsync(dbContext, userManager, roleManager, clock.Now);
            await ActivitySeeder.SeedAsync(dbContext, clock.Today);
        }
        else
        {
            // Roles are required for the application to work even without demo data.
            await IdentitySeeder.SeedRolesAsync(roleManager);
        }
    }
}
