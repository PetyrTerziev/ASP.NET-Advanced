using Microsoft.EntityFrameworkCore;

using GuardPost.Common;
using GuardPost.Common.Providers;
using GuardPost.Data;
using GuardPost.Data.Models;
using GuardPost.Data.Models.Enums;
using GuardPost.Services.Core.Contracts;
using GuardPost.Services.Core.Mapping;
using GuardPost.Web.ViewModels.Common;
using GuardPost.Web.ViewModels.LeaveRequests;

using static GuardPost.Common.EntityValidationConstants.LeaveRequest;

namespace GuardPost.Services.Core;

public class LeaveRequestService : ILeaveRequestService
{
    private readonly GuardPostDbContext dbContext;
    private readonly IDateTimeProvider clock;

    public LeaveRequestService(GuardPostDbContext dbContext, IDateTimeProvider clock)
    {
        this.dbContext = dbContext;
        this.clock = clock;
    }

    public async Task<IReadOnlyList<LeaveRequestListItemViewModel>> GetForGuardAsync(int guardId)
        => await dbContext.LeaveRequests
            .AsNoTracking()
            .Where(l => l.GuardId == guardId)
            .OrderByDescending(l => l.StartDate)
            .Select(Projections.LeaveToListItem)
            .ToListAsync();

    public async Task<LeaveRequestIndexViewModel> GetAllAsync(LeaveStatus? status, int? guardId, int page, int pageSize)
    {
        IQueryable<LeaveRequest> query = dbContext.LeaveRequests.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(l => l.Status == status.Value);
        }

        if (guardId.HasValue)
        {
            query = query.Where(l => l.GuardId == guardId.Value);
        }

        int totalCount = await query.CountAsync();
        page = Paging.Normalize(page, totalCount, pageSize);

        var items = await query
            .OrderBy(l => l.Status == LeaveStatus.Pending ? 0 : 1)
            .ThenByDescending(l => l.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(Projections.LeaveToListItem)
            .ToListAsync();

        return new LeaveRequestIndexViewModel
        {
            Status = status,
            GuardId = guardId,
            PendingCount = await CountPendingAsync(),
            Result = new PagedResult<LeaveRequestListItemViewModel>(items, totalCount, page, pageSize),
        };
    }

    public async Task<int> CountPendingAsync()
        => await dbContext.LeaveRequests.CountAsync(l => l.Status == LeaveStatus.Pending);

    public async Task<IReadOnlyList<ValidationError>> ValidateAsync(LeaveRequestFormViewModel model, int guardId)
    {
        var errors = new List<ValidationError>();

        if (model.StartDate is null || model.EndDate is null)
        {
            return errors;
        }

        var start = model.StartDate.Value.Date;
        var end = model.EndDate.Value.Date;
        var today = clock.Today;

        if (end < start)
        {
            errors.Add(new ValidationError(nameof(model.EndDate), ValidationMessages.LeaveEndBeforeStart));
            return errors;
        }

        if (start < today)
        {
            errors.Add(new ValidationError(nameof(model.StartDate), ValidationMessages.LeaveInPast));
        }

        if (start > today.AddMonths(MaxMonthsAhead))
        {
            errors.Add(new ValidationError(nameof(model.StartDate), string.Format(ValidationMessages.LeaveTooFarAhead, MaxMonthsAhead)));
        }

        if ((end - start).Days + 1 > MaxDays)
        {
            errors.Add(new ValidationError(nameof(model.EndDate), string.Format(ValidationMessages.LeaveTooLong, MaxDays)));
        }

        var overlapping = await dbContext.LeaveRequests
            .AsNoTracking()
            .Where(l => l.GuardId == guardId
                        && (l.Status == LeaveStatus.Pending || l.Status == LeaveStatus.Approved)
                        && l.StartDate <= end
                        && l.EndDate >= start)
            .Select(l => new { l.StartDate, l.EndDate })
            .FirstOrDefaultAsync();

        if (overlapping != null)
        {
            errors.Add(new ValidationError(
                string.Empty,
                string.Format(
                    ValidationMessages.LeaveOverlaps,
                    overlapping.StartDate.ToString(ApplicationConstants.DateFormat),
                    overlapping.EndDate.ToString(ApplicationConstants.DateFormat))));
        }

        return errors;
    }

    public async Task<int> CreateAsync(LeaveRequestFormViewModel model, int guardId, string requestedById)
    {
        var request = new LeaveRequest
        {
            GuardId = guardId,
            StartDate = model.StartDate!.Value.Date,
            EndDate = model.EndDate!.Value.Date,
            Type = model.Type!.Value,
            Reason = string.IsNullOrWhiteSpace(model.Reason) ? null : model.Reason.Trim(),
            Status = LeaveStatus.Pending,
            CreatedOn = clock.Now,
            RequestedById = requestedById,
        };

        await dbContext.LeaveRequests.AddAsync(request);
        await dbContext.SaveChangesAsync();

        return request.Id;
    }

    public async Task<OperationResult> CancelAsync(int id, int guardId)
    {
        // Filtering by guard as well prevents cancelling somebody else's request (parameter tampering).
        var request = await dbContext.LeaveRequests.FirstOrDefaultAsync(l => l.Id == id && l.GuardId == guardId);
        if (request is null)
        {
            return OperationResult.Failure(ValidationMessages.UserNotFound);
        }

        if (request.Status != LeaveStatus.Pending)
        {
            return OperationResult.Failure(ValidationMessages.LeaveNotPending);
        }

        request.Status = LeaveStatus.Cancelled;
        await dbContext.SaveChangesAsync();

        return OperationResult.Success();
    }

    public async Task<LeaveRequestReviewViewModel?> GetForReviewAsync(int id)
    {
        var request = await dbContext.LeaveRequests
            .AsNoTracking()
            .Where(l => l.Id == id)
            .Select(Projections.LeaveToListItem)
            .FirstOrDefaultAsync();

        if (request is null)
        {
            return null;
        }

        return new LeaveRequestReviewViewModel
        {
            Request = request,
            ConflictingShifts = await ConflictingShifts(request.GuardId, request.StartDate, request.EndDate)
                .OrderBy(s => s.StartTime)
                .Select(Projections.ShiftToListItem)
                .ToListAsync(),
            Input = new LeaveReviewInputModel { Id = id, ReviewNote = request.ReviewNote },
        };
    }

    public async Task<OperationResult> ApproveAsync(int id, string reviewerId, string? note)
    {
        var request = await dbContext.LeaveRequests.FirstOrDefaultAsync(l => l.Id == id);
        if (request is null || request.Status != LeaveStatus.Pending)
        {
            return OperationResult.Failure(ValidationMessages.LeaveNotPending);
        }

        int conflicts = await ConflictingShifts(request.GuardId, request.StartDate, request.EndDate).CountAsync();
        if (conflicts > 0)
        {
            return OperationResult.Failure(string.Format(ValidationMessages.LeaveHasShifts, conflicts));
        }

        Review(request, LeaveStatus.Approved, reviewerId, note);
        await dbContext.SaveChangesAsync();

        return OperationResult.Success();
    }

    public async Task<OperationResult> RejectAsync(int id, string reviewerId, string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return OperationResult.Failure(ValidationMessages.ReviewNoteRequiredForReject);
        }

        var request = await dbContext.LeaveRequests.FirstOrDefaultAsync(l => l.Id == id);
        if (request is null || request.Status != LeaveStatus.Pending)
        {
            return OperationResult.Failure(ValidationMessages.LeaveNotPending);
        }

        Review(request, LeaveStatus.Rejected, reviewerId, note);
        await dbContext.SaveChangesAsync();

        return OperationResult.Success();
    }

    public async Task<string?> GetRequesterIdAsync(int id)
        => await dbContext.LeaveRequests
            .AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => l.RequestedById)
            .FirstOrDefaultAsync();

    private IQueryable<Shift> ConflictingShifts(int guardId, DateTime startDate, DateTime endDate)
    {
        var periodEnd = endDate.Date.AddDays(1);
        var periodStart = startDate.Date;

        return dbContext.Shifts
            .AsNoTracking()
            .Where(s => s.GuardId == guardId && s.StartTime < periodEnd && s.EndTime > periodStart);
    }

    private void Review(LeaveRequest request, LeaveStatus status, string reviewerId, string? note)
    {
        request.Status = status;
        request.ReviewedById = reviewerId;
        request.ReviewedOn = clock.Now;
        request.ReviewNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }
}
