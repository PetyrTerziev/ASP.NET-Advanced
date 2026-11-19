using GuardPost.Common;
using GuardPost.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GuardPost.Data.Seeding;

/// <summary>
/// Creates the two roles and the demo accounts (one administrator and three guards).
/// Safe to run on every start – existing roles and users are not touched.
/// </summary>
public static class IdentitySeeder
{
    public const string AdminEmail = "admin@gardpost.bg";
    public const string AdminPassword = "Admin123!";
    public const string GuardPassword = "Guard123!";

    /// <summary>Demo guard accounts: e-mail, license number of the linked guard.</summary>
    public static readonly IReadOnlyList<(string Email, string LicenseNumber)> GuardAccounts =
    [
        ("ivan.petrov@gardpost.bg", "ОХ-104233"),
        ("maria.koleva@gardpost.bg", "ОХ-120017"),
        ("georgi.dimitrov@gardpost.bg", "ОХ-118540"),
    ];

    public static async Task SeedAsync(
        GuardPostDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        DateTime now)
    {
        await SeedRolesAsync(roleManager);

        if (await userManager.FindByEmailAsync(AdminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                EmailConfirmed = true,
                FirstName = "Петя",
                LastName = "Иванова",
                CreatedOn = now,
            };

            EnsureSucceeded(await userManager.CreateAsync(admin, AdminPassword), AdminEmail);
            EnsureSucceeded(await userManager.AddToRolesAsync(admin, new[] { RoleNames.Administrator, RoleNames.User }), AdminEmail);
        }

        foreach (var (email, license) in GuardAccounts)
        {
            if (await userManager.FindByEmailAsync(email) is not null)
            {
                continue;
            }

            var guard = await context.Guards.FirstOrDefaultAsync(g => g.LicenseNumber == license);
            if (guard is null)
            {
                continue;
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = guard.FirstName,
                LastName = guard.LastName,
                GuardId = guard.Id,
                CreatedOn = now,
            };

            EnsureSucceeded(await userManager.CreateAsync(user, GuardPassword), email);
            EnsureSucceeded(await userManager.AddToRoleAsync(user, RoleNames.User), email);
        }
    }

    public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in new[] { RoleNames.Administrator, RoleNames.User })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                EnsureSucceeded(await roleManager.CreateAsync(new IdentityRole(role)), $"role {role}");
            }
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string what)
    {
        if (!result.Succeeded)
        {
            string errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Seeding {what} failed: {errors}");
        }
    }
}
