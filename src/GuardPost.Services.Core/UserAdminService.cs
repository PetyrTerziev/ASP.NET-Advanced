using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using GuardPost.Common;
using GuardPost.Common.Providers;
using GuardPost.Data;
using GuardPost.Data.Models;
using GuardPost.Services.Core.Contracts;
using GuardPost.Web.ViewModels.Common;
using GuardPost.Web.ViewModels.Users;

namespace GuardPost.Services.Core;

/// <summary>User management for administrators – built on top of ASP.NET Core Identity's UserManager.</summary>
public class UserAdminService : IUserAdminService
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly GuardPostDbContext dbContext;
    private readonly IDateTimeProvider clock;

    public UserAdminService(UserManager<ApplicationUser> userManager, GuardPostDbContext dbContext, IDateTimeProvider clock)
    {
        this.userManager = userManager;
        this.dbContext = dbContext;
        this.clock = clock;
    }

    public async Task<UserIndexViewModel> GetUsersAsync(string? searchTerm, int page, int pageSize)
    {
        var query = userManager.Users;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim();
            query = query.Where(u => u.FirstName.Contains(term)
                                     || u.LastName.Contains(term)
                                     || (u.Email != null && u.Email.Contains(term)));
        }

        int totalCount = await query.CountAsync();
        page = Paging.Normalize(page, totalCount, pageSize);

        var users = await query
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var guardIds = users.Where(u => u.GuardId.HasValue).Select(u => u.GuardId!.Value).ToList();
        var guardNames = await dbContext.Guards
            .AsNoTracking()
            .Where(g => guardIds.Contains(g.Id))
            .Select(g => new { g.Id, Name = g.FirstName + " " + g.LastName })
            .ToListAsync();

        var items = new List<UserListItemViewModel>();
        foreach (var user in users)
        {
            items.Add(new UserListItemViewModel
            {
                Id = user.Id,
                FullName = $"{user.FirstName} {user.LastName}",
                Email = user.Email ?? string.Empty,
                IsAdministrator = await userManager.IsInRoleAsync(user, RoleNames.Administrator),
                IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.Now,
                GuardId = user.GuardId,
                GuardName = guardNames.FirstOrDefault(g => g.Id == user.GuardId)?.Name,
                CreatedOn = user.CreatedOn,
            });
        }

        return new UserIndexViewModel
        {
            SearchTerm = searchTerm,
            Result = new PagedResult<UserListItemViewModel>(items, totalCount, page, pageSize),
        };
    }

    public async Task<OperationResult> SetAdministratorAsync(string userId, bool isAdministrator, string currentUserId)
    {
        if (userId == currentUserId && !isAdministrator)
        {
            return OperationResult.Failure(ValidationMessages.CannotChangeOwnRole);
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return OperationResult.Failure(ValidationMessages.UserNotFound);
        }

        bool isInRole = await userManager.IsInRoleAsync(user, RoleNames.Administrator);
        if (isInRole == isAdministrator)
        {
            return OperationResult.Success();
        }

        var result = isAdministrator
            ? await userManager.AddToRoleAsync(user, RoleNames.Administrator)
            : await userManager.RemoveFromRoleAsync(user, RoleNames.Administrator);

        return ToOperationResult(result);
    }

    public async Task<OperationResult> SetLockedAsync(string userId, bool locked, string currentUserId)
    {
        if (userId == currentUserId)
        {
            return OperationResult.Failure(ValidationMessages.CannotLockSelf);
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return OperationResult.Failure(ValidationMessages.UserNotFound);
        }

        await userManager.SetLockoutEnabledAsync(user, true);
        var result = await userManager.SetLockoutEndDateAsync(user, locked ? DateTimeOffset.MaxValue : null);

        return ToOperationResult(result);
    }

    public async Task<UserLinkGuardViewModel?> GetLinkGuardAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return null;
        }

        var linkedToOthers = await userManager.Users
            .Where(u => u.GuardId != null && u.Id != userId)
            .Select(u => u.GuardId!.Value)
            .ToListAsync();

        var guards = await dbContext.Guards
            .AsNoTracking()
            .Where(g => !linkedToOthers.Contains(g.Id))
            .OrderBy(g => g.FirstName)
            .ThenBy(g => g.LastName)
            .Select(g => new DropdownItemViewModel
            {
                Id = g.Id,
                Text = g.FirstName + " " + g.LastName + " (" + g.LicenseNumber + ")",
            })
            .ToListAsync();

        return new UserLinkGuardViewModel
        {
            UserId = user.Id,
            FullName = $"{user.FirstName} {user.LastName}",
            Email = user.Email ?? string.Empty,
            GuardId = user.GuardId,
            Guards = guards,
        };
    }

    public async Task<OperationResult> LinkGuardAsync(string userId, int? guardId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return OperationResult.Failure(ValidationMessages.UserNotFound);
        }

        if (guardId.HasValue)
        {
            bool guardExists = await dbContext.Guards.AnyAsync(g => g.Id == guardId.Value);
            if (!guardExists)
            {
                return OperationResult.Failure(ValidationMessages.GuardNotAvailable);
            }

            bool takenByOther = await userManager.Users.AnyAsync(u => u.GuardId == guardId && u.Id != userId);
            if (takenByOther)
            {
                return OperationResult.Failure(ValidationMessages.GuardAlreadyLinked);
            }
        }

        user.GuardId = guardId;
        return ToOperationResult(await userManager.UpdateAsync(user));
    }

    private static OperationResult ToOperationResult(IdentityResult result)
        => result.Succeeded
            ? OperationResult.Success()
            : OperationResult.Failure(string.Join(" ", result.Errors.Select(e => e.Description)));
}
