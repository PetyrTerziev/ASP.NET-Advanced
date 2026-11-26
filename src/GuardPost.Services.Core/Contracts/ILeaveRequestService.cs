using GuardPost.Data.Models.Enums;
using GuardPost.Web.ViewModels.Common;
using GuardPost.Web.ViewModels.LeaveRequests;

namespace GuardPost.Services.Core.Contracts;

public interface ILeaveRequestService
{
    Task<IReadOnlyList<LeaveRequestListItemViewModel>> GetForGuardAsync(int guardId);

    Task<LeaveRequestIndexViewModel> GetAllAsync(LeaveStatus? status, int? guardId, int page, int pageSize);

    Task<int> CountPendingAsync();

    /// <summary>Dates, maximum length and overlaps with the guard's other pending/approved requests.</summary>
    Task<IReadOnlyList<ValidationError>> ValidateAsync(LeaveRequestFormViewModel model, int guardId);

    Task<int> CreateAsync(LeaveRequestFormViewModel model, int guardId, string requestedById);

    /// <summary>A guard can cancel only his/her own requests that are still pending.</summary>
    Task<OperationResult> CancelAsync(int id, int guardId);

    Task<LeaveRequestReviewViewModel?> GetForReviewAsync(int id);

    Task<OperationResult> ApproveAsync(int id, string reviewerId, string? note);

    Task<OperationResult> RejectAsync(int id, string reviewerId, string? note);

    /// <summary>Returns the account that requested the leave – used to notify it about the decision.</summary>
    Task<string?> GetRequesterIdAsync(int id);
}
