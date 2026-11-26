using GuardPost.Web.ViewModels.Common;
using GuardPost.Web.ViewModels.Users;

namespace GuardPost.Services.Core.Contracts;

public interface IUserAdminService
{
    Task<UserIndexViewModel> GetUsersAsync(string? searchTerm, int page, int pageSize);

    Task<OperationResult> SetAdministratorAsync(string userId, bool isAdministrator, string currentUserId);

    Task<OperationResult> SetLockedAsync(string userId, bool locked, string currentUserId);

    Task<UserLinkGuardViewModel?> GetLinkGuardAsync(string userId);

    Task<OperationResult> LinkGuardAsync(string userId, int? guardId);
}
