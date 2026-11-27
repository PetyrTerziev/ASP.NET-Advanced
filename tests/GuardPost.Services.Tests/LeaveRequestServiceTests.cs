using Microsoft.EntityFrameworkCore;

using GuardPost.Data.Models.Enums;
using GuardPost.Services.Core;
using GuardPost.Services.Tests.Infrastructure;
using GuardPost.Web.ViewModels.LeaveRequests;

namespace GuardPost.Services.Tests;

public class LeaveRequestServiceTests : IDisposable
{
    private readonly TestDb db = new();
    private readonly LeaveRequestService service;

    public LeaveRequestServiceTests()
    {
        service = new LeaveRequestService(db.Context, db.Clock);
    }

    public void Dispose() => db.Dispose();

    private LeaveRequestFormViewModel Form(int fromDays, int toDays)
        => new()
        {
            StartDate = db.Today.AddDays(fromDays),
            EndDate = db.Today.AddDays(toDays),
            Type = LeaveType.Paid,
            Reason = " Почивка ",
        };

    [Fact]
    public async Task ValidateAsync_ValidRequest_HasNoErrors()
    {
        var guard = db.AddGuard();

        Assert.Empty(await service.ValidateAsync(Form(5, 9), guard.Id));
        Assert.Empty(await service.ValidateAsync(Form(0, 0), guard.Id));
        Assert.Empty(await service.ValidateAsync(new LeaveRequestFormViewModel(), guard.Id));
    }

    [Fact]
    public async Task ValidateAsync_RejectsPastStartEndBeforeStartTooLongAndTooFar()
    {
        var guard = db.AddGuard();

        Assert.Contains(await service.ValidateAsync(Form(-1, 2), guard.Id), e => e.Message.Contains("миналото"));
        Assert.Contains(await service.ValidateAsync(Form(5, 3), guard.Id), e => e.Message.Contains("преди първия"));
        Assert.Contains(await service.ValidateAsync(Form(1, 61), guard.Id), e => e.Message.Contains("60 дни"));
        Assert.Contains(await service.ValidateAsync(Form(400, 401), guard.Id), e => e.Message.Contains("12 месеца"));
    }

    [Fact]
    public async Task ValidateAsync_OverlapWithPendingOrApproved_IsRejected_RejectedAndCancelledAreIgnored()
    {
        var guard = db.AddGuard();
        db.AddLeave(guard, db.Today.AddDays(10), db.Today.AddDays(12), LeaveStatus.Approved);
        db.AddLeave(guard, db.Today.AddDays(20), db.Today.AddDays(22), LeaveStatus.Rejected);
        db.AddLeave(guard, db.Today.AddDays(30), db.Today.AddDays(32), LeaveStatus.Cancelled);

        Assert.Contains(await service.ValidateAsync(Form(12, 14), guard.Id), e => e.Message.Contains("застъпва"));
        Assert.Empty(await service.ValidateAsync(Form(20, 22), guard.Id));
        Assert.Empty(await service.ValidateAsync(Form(31, 31), guard.Id));
    }

    [Fact]
    public async Task CreateAsync_CreatesPendingRequest()
    {
        var guard = db.AddGuard();
        var user = db.AddUser(guard: guard);

        int id = await service.CreateAsync(Form(3, 5), guard.Id, user.Id);
        var stored = await db.Context.LeaveRequests.SingleAsync(l => l.Id == id);

        Assert.Equal(LeaveStatus.Pending, stored.Status);
        Assert.Equal("Почивка", stored.Reason);
        Assert.Equal(user.Id, stored.RequestedById);
        Assert.Equal(1, await service.CountPendingAsync());
        Assert.Equal(user.Id, await service.GetRequesterIdAsync(id));
    }

    [Fact]
    public async Task CancelAsync_OnlyOwnPendingRequests()
    {
        var guard = db.AddGuard();
        var other = db.AddGuard("Мария", "Колева");
        var pending = db.AddLeave(guard, db.Today.AddDays(5), db.Today.AddDays(6));
        var approved = db.AddLeave(guard, db.Today.AddDays(10), db.Today.AddDays(11), LeaveStatus.Approved);

        Assert.False((await service.CancelAsync(pending.Id, other.Id)).Succeeded);
        Assert.False((await service.CancelAsync(approved.Id, guard.Id)).Succeeded);

        var result = await service.CancelAsync(pending.Id, guard.Id);
        Assert.True(result.Succeeded);
        Assert.Equal(LeaveStatus.Cancelled, (await db.Context.LeaveRequests.SingleAsync(l => l.Id == pending.Id)).Status);
    }

    [Fact]
    public async Task ApproveAsync_IsBlockedByShiftsInThePeriod()
    {
        var site = db.AddSite();
        var guard = db.AddGuard();
        var admin = db.AddUser();
        var leave = db.AddLeave(guard, db.Today.AddDays(5), db.Today.AddDays(6));
        db.AddShift(site, guard, db.Today.AddDays(6).AddHours(20));

        var review = await service.GetForReviewAsync(leave.Id);
        var result = await service.ApproveAsync(leave.Id, admin.Id, null);

        Assert.Single(review!.ConflictingShifts);
        Assert.False(result.Succeeded);
        Assert.Contains("1 смени", result.Error);
    }

    [Fact]
    public async Task ApproveAsync_WithoutConflicts_SetsReviewer()
    {
        var guard = db.AddGuard();
        var admin = db.AddUser("Петя", "Иванова");
        var leave = db.AddLeave(guard, db.Today.AddDays(5), db.Today.AddDays(6));

        var result = await service.ApproveAsync(leave.Id, admin.Id, "  Приятна почивка ");
        var review = await service.GetForReviewAsync(leave.Id);

        Assert.True(result.Succeeded);
        Assert.Equal(LeaveStatus.Approved, review!.Request.Status);
        Assert.Equal("Петя Иванова", review.Request.ReviewedBy);
        Assert.Equal("Приятна почивка", review.Request.ReviewNote);
        Assert.Equal(TestDb.Now, review.Request.ReviewedOn);
        Assert.False((await service.ApproveAsync(leave.Id, admin.Id, null)).Succeeded);
        Assert.False((await service.ApproveAsync(999, admin.Id, null)).Succeeded);
    }

    [Fact]
    public async Task RejectAsync_RequiresNote()
    {
        var guard = db.AddGuard();
        var admin = db.AddUser();
        var leave = db.AddLeave(guard, db.Today.AddDays(5), db.Today.AddDays(6));

        Assert.False((await service.RejectAsync(leave.Id, admin.Id, " ")).Succeeded);

        var result = await service.RejectAsync(leave.Id, admin.Id, "Няма заместник");
        Assert.True(result.Succeeded);
        Assert.Equal(LeaveStatus.Rejected, (await db.Context.LeaveRequests.SingleAsync(l => l.Id == leave.Id)).Status);
        Assert.False((await service.RejectAsync(leave.Id, admin.Id, "Отново")).Succeeded);
    }

    [Fact]
    public async Task GetAllAsync_FiltersAndShowsPendingFirst()
    {
        var guard = db.AddGuard();
        var other = db.AddGuard("Мария", "Колева");
        db.AddLeave(guard, db.Today.AddDays(40), db.Today.AddDays(41), LeaveStatus.Approved);
        db.AddLeave(guard, db.Today.AddDays(5), db.Today.AddDays(6));
        db.AddLeave(other, db.Today.AddDays(8), db.Today.AddDays(9), LeaveStatus.Rejected);

        var all = await service.GetAllAsync(null, null, 1, 10);
        var pending = await service.GetAllAsync(LeaveStatus.Pending, null, 1, 10);
        var forOther = await service.GetAllAsync(null, other.Id, 1, 10);

        Assert.Equal(3, all.Result.TotalCount);
        Assert.Equal(LeaveStatus.Pending, all.Result.Items[0].Status);
        Assert.Equal(1, all.PendingCount);
        Assert.Single(pending.Result.Items);
        Assert.Equal("Мария Колева", forOther.Result.Items.Single().GuardName);
    }

    [Fact]
    public async Task GetForGuardAsync_ReturnsOnlyThatGuardNewestFirst()
    {
        var guard = db.AddGuard();
        var other = db.AddGuard("Мария", "Колева");
        db.AddLeave(guard, db.Today.AddDays(5), db.Today.AddDays(6));
        db.AddLeave(guard, db.Today.AddDays(30), db.Today.AddDays(31));
        db.AddLeave(other, db.Today.AddDays(5), db.Today.AddDays(6));

        var list = await service.GetForGuardAsync(guard.Id);

        Assert.Equal(2, list.Count);
        Assert.True(list[0].StartDate > list[1].StartDate);
        Assert.Equal(2, list[0].Days);
        Assert.Null(await service.GetForReviewAsync(999));
    }
}
