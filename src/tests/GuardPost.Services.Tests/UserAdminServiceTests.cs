using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using GuardPost.Common;
using GuardPost.Data;
using GuardPost.Data.Models;
using GuardPost.Services.Core;
using GuardPost.Services.Tests.Infrastructure;

namespace GuardPost.Services.Tests;

public class UserAdminServiceTests : IDisposable
{
    private readonly TestDb db = new();
    private readonly UserManager<ApplicationUser> userManager;
    private readonly RoleManager<IdentityRole> roleManager;
    private readonly UserAdminService service;

    public UserAdminServiceTests()
    {
        // Real Identity managers on top of the InMemory database.
        userManager = new UserManager<ApplicationUser>(
            new UserStore<ApplicationUser, IdentityRole, GuardPostDbContext>(db.Context),
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            new IUserValidator<ApplicationUser>[] { new UserValidator<ApplicationUser>() },
            new IPasswordValidator<ApplicationUser>[] { new PasswordValidator<ApplicationUser>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<ApplicationUser>>.Instance);

        roleManager = new RoleManager<IdentityRole>(
            new RoleStore<IdentityRole, GuardPostDbContext>(db.Context),
            new IRoleValidator<IdentityRole>[] { new RoleValidator<IdentityRole>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            NullLogger<RoleManager<IdentityRole>>.Instance);

        service = new UserAdminService(userManager, db.Context, db.Clock);
    }

    public void Dispose()
    {
        userManager.Dispose();
        roleManager.Dispose();
        db.Dispose();
    }

    private async Task<ApplicationUser> CreateUserAsync(string email, string firstName, bool admin = false, Guard? guard = null)
    {
        if (!await roleManager.RoleExistsAsync(RoleNames.Administrator))
        {
            await roleManager.CreateAsync(new IdentityRole(RoleNames.Administrator));
            await roleManager.CreateAsync(new IdentityRole(RoleNames.User));
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = "Тестов",
            GuardId = guard?.Id,
            CreatedOn = TestDb.Now,
        };

        var result = await userManager.CreateAsync(user, "Parola123!");
        Assert.True(result.Succeeded);
        await userManager.AddToRoleAsync(user, admin ? RoleNames.Administrator : RoleNames.User);

        return user;
    }

    [Fact]
    public async Task GetUsersAsync_ReturnsRolesGuardNamesAndSupportsSearch()
    {
        var guard = db.AddGuard();
        await CreateUserAsync("admin@test.bg", "Админ", admin: true);
        await CreateUserAsync("ivan@test.bg", "Иван", guard: guard);

        var all = await service.GetUsersAsync(null, 1, 10);
        var search = await service.GetUsersAsync("ivan@", 1, 10);

        Assert.Equal(2, all.Result.TotalCount);
        Assert.True(all.Result.Items.Single(u => u.Email == "admin@test.bg").IsAdministrator);
        Assert.Equal("Иван Петров", all.Result.Items.Single(u => u.Email == "ivan@test.bg").GuardName);
        Assert.Single(search.Result.Items);
    }

    [Fact]
    public async Task SetAdministratorAsync_AddsAndRemovesRole_ButNotFromYourself()
    {
        var admin = await CreateUserAsync("admin@test.bg", "Админ", admin: true);
        var user = await CreateUserAsync("user@test.bg", "Потребител");

        Assert.True((await service.SetAdministratorAsync(user.Id, true, admin.Id)).Succeeded);
        Assert.True(await userManager.IsInRoleAsync(user, RoleNames.Administrator));

        Assert.True((await service.SetAdministratorAsync(user.Id, true, admin.Id)).Succeeded); // already admin – no-op
        Assert.True((await service.SetAdministratorAsync(user.Id, false, admin.Id)).Succeeded);
        Assert.False(await userManager.IsInRoleAsync(user, RoleNames.Administrator));

        Assert.False((await service.SetAdministratorAsync(admin.Id, false, admin.Id)).Succeeded);
        Assert.False((await service.SetAdministratorAsync("missing", true, admin.Id)).Succeeded);
    }

    [Fact]
    public async Task SetLockedAsync_LocksAndUnlocks_ButNotYourself()
    {
        var admin = await CreateUserAsync("admin@test.bg", "Админ", admin: true);
        var user = await CreateUserAsync("user@test.bg", "Потребител");

        Assert.True((await service.SetLockedAsync(user.Id, true, admin.Id)).Succeeded);
        Assert.True(await userManager.IsLockedOutAsync(user));
        Assert.True((await service.GetUsersAsync(null, 1, 10)).Result.Items.Single(u => u.Id == user.Id).IsLockedOut);

        Assert.True((await service.SetLockedAsync(user.Id, false, admin.Id)).Succeeded);
        Assert.False(await userManager.IsLockedOutAsync(user));

        Assert.False((await service.SetLockedAsync(admin.Id, true, admin.Id)).Succeeded);
        Assert.False((await service.SetLockedAsync("missing", true, admin.Id)).Succeeded);
    }

    [Fact]
    public async Task LinkGuardAsync_LinksFreeGuard_AndRejectsTakenOrUnknown()
    {
        var guard = db.AddGuard();
        var free = db.AddGuard("Мария", "Колева");
        await CreateUserAsync("ivan@test.bg", "Иван", guard: guard);
        var newcomer = await CreateUserAsync("new@test.bg", "Нов");

        var form = await service.GetLinkGuardAsync(newcomer.Id);
        Assert.Equal(free.Id, form!.Guards.Single().Id);   // guards linked to others are not offered
        Assert.Null(await service.GetLinkGuardAsync("missing"));

        Assert.False((await service.LinkGuardAsync(newcomer.Id, guard.Id)).Succeeded);
        Assert.False((await service.LinkGuardAsync(newcomer.Id, 999)).Succeeded);
        Assert.False((await service.LinkGuardAsync("missing", free.Id)).Succeeded);

        Assert.True((await service.LinkGuardAsync(newcomer.Id, free.Id)).Succeeded);
        Assert.Equal(free.Id, (await userManager.FindByIdAsync(newcomer.Id))!.GuardId);

        Assert.True((await service.LinkGuardAsync(newcomer.Id, null)).Succeeded);
        Assert.Null((await userManager.FindByIdAsync(newcomer.Id))!.GuardId);
    }
}
