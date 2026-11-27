using Microsoft.EntityFrameworkCore;

using GuardPost.Data.Models.Enums;
using GuardPost.Services.Core;
using GuardPost.Services.Tests.Infrastructure;
using GuardPost.Web.ViewModels.Shifts;

namespace GuardPost.Services.Tests;

public class ShiftServiceTests : IDisposable
{
    private readonly TestDb db = new();
    private readonly ShiftService service;

    public ShiftServiceTests()
    {
        service = new ShiftService(db.Context, db.Clock);
    }

    public void Dispose() => db.Dispose();

    private static ShiftFormViewModel Form(int siteId, int guardId, DateTime start, double hours, int id = 0)
        => new() { Id = id, SiteId = siteId, GuardId = guardId, StartTime = start, EndTime = start.AddHours(hours) };

    [Fact]
    public async Task GetWeekAsync_StartsOnMondayAndGroupsShiftsByDay()
    {
        var site = db.AddSite();
        var guard = db.AddGuard();
        db.AddShift(site, guard, new DateTime(2026, 10, 11, 20, 0, 0));  // Sunday night of the previous week
        db.AddShift(site, guard, new DateTime(2026, 10, 14, 8, 0, 0));   // Wednesday
        db.AddShift(site, guard, new DateTime(2026, 10, 20, 8, 0, 0));   // next week

        var week = await service.GetWeekAsync(null, null, null);

        Assert.Equal(new DateTime(2026, 10, 12), week.WeekStart);
        Assert.Equal(7, week.Days.Count);
        Assert.Single(week.Days[0].Shifts);   // the Sunday-night shift is shown on Monday
        Assert.Single(week.Days[2].Shifts);
        Assert.Equal(2, week.TotalShifts);
        Assert.Equal(24, week.TotalHours, 3);
    }

    [Fact]
    public async Task GetWeekAsync_FiltersBySiteAndGuard()
    {
        var site = db.AddSite();
        var otherSite = db.AddSite("Офис");
        var guard = db.AddGuard();
        var otherGuard = db.AddGuard("Мария", "Колева");
        db.AddShift(site, guard, new DateTime(2026, 10, 13, 8, 0, 0));
        db.AddShift(otherSite, otherGuard, new DateTime(2026, 10, 13, 8, 0, 0));

        var bySite = await service.GetWeekAsync(new DateTime(2026, 10, 15), site.Id, null);
        var byGuard = await service.GetWeekAsync(new DateTime(2026, 10, 15), null, otherGuard.Id);

        Assert.Equal(1, bySite.TotalShifts);
        Assert.Equal(site.Id, bySite.Days[1].Shifts[0].SiteId);
        Assert.Equal(otherGuard.Id, byGuard.Days[1].Shifts.Single().GuardId);
    }

    [Fact]
    public async Task ValidateAsync_ValidShift_ReturnsNoErrors()
    {
        var site = db.AddSite();
        var guard = db.AddGuard();

        var errors = await service.ValidateAsync(Form(site.Id, guard.Id, TestDb.Now.AddDays(1), 12));

        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateAsync_MissingValues_AreLeftToDataAnnotations()
    {
        var errors = await service.ValidateAsync(new ShiftFormViewModel());

        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateAsync_InactiveSiteAndGuard_AreRejected()
    {
        var site = db.AddSite(active: false);
        var guard = db.AddGuard(active: false);

        var errors = await service.ValidateAsync(Form(site.Id, guard.Id, TestDb.Now.AddDays(1), 8));

        Assert.Contains(errors, e => e.PropertyName == nameof(ShiftFormViewModel.SiteId));
        Assert.Contains(errors, e => e.PropertyName == nameof(ShiftFormViewModel.GuardId));
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(25)]
    public async Task ValidateAsync_DurationOutsideLimits_IsRejected(double hours)
    {
        var site = db.AddSite();
        var guard = db.AddGuard();

        var errors = await service.ValidateAsync(Form(site.Id, guard.Id, TestDb.Now.AddDays(1), hours));

        Assert.Contains(errors, e => e.PropertyName == nameof(ShiftFormViewModel.EndTime));
    }

    [Fact]
    public async Task ValidateAsync_OverlappingShift_IsRejected()
    {
        var site = db.AddSite();
        var guard = db.AddGuard();
        db.AddShift(site, guard, TestDb.Now.AddDays(1));

        var errors = await service.ValidateAsync(Form(site.Id, guard.Id, TestDb.Now.AddDays(1).AddHours(6), 12));

        var error = Assert.Single(errors);
        Assert.Contains("застъпва", error.Message);
    }

    [Fact]
    public async Task ValidateAsync_LessThanTwelveHoursRest_IsRejected()
    {
        var site = db.AddSite();
        var guard = db.AddGuard();
        db.AddShift(site, guard, new DateTime(2026, 10, 20, 8, 0, 0));    // ends 20:00

        var errors = await service.ValidateAsync(Form(site.Id, guard.Id, new DateTime(2026, 10, 21, 4, 0, 0), 8));

        var error = Assert.Single(errors);
        Assert.Contains("почивка", error.Message);
    }

    [Fact]
    public async Task ValidateAsync_ExactlyTwelveHoursRest_IsAllowed()
    {
        var site = db.AddSite();
        var guard = db.AddGuard();
        db.AddShift(site, guard, new DateTime(2026, 10, 20, 8, 0, 0));

        var errors = await service.ValidateAsync(Form(site.Id, guard.Id, new DateTime(2026, 10, 21, 8, 0, 0), 12));

        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateAsync_GuardOnApprovedLeave_IsRejected_ButPendingLeaveIsIgnored()
    {
        var site = db.AddSite();
        var onLeave = db.AddGuard();
        var pending = db.AddGuard("Мария", "Колева");
        db.AddLeave(onLeave, new DateTime(2026, 10, 25), new DateTime(2026, 10, 27), LeaveStatus.Approved);
        db.AddLeave(pending, new DateTime(2026, 10, 25), new DateTime(2026, 10, 27), LeaveStatus.Pending);

        var blocked = await service.ValidateAsync(Form(site.Id, onLeave.Id, new DateTime(2026, 10, 27, 20, 0, 0), 12));
        var allowed = await service.ValidateAsync(Form(site.Id, pending.Id, new DateTime(2026, 10, 27, 20, 0, 0), 12));

        Assert.Contains(blocked, e => e.Message.Contains("отпуск"));
        Assert.Empty(allowed);
    }

    [Fact]
    public async Task ValidateAsync_EditingAShift_DoesNotConflictWithItself_AndKeepsDeactivatedSite()
    {
        var site = db.AddSite();
        var guard = db.AddGuard();
        var shift = db.AddShift(site, guard, TestDb.Now.AddDays(1));
        site.IsActive = false;
        db.Context.SaveChanges();

        var errors = await service.ValidateAsync(Form(site.Id, guard.Id, shift.StartTime.AddHours(1), 10, shift.Id));

        Assert.Empty(errors);
    }

    [Fact]
    public async Task CreateUpdateDelete_WorkAndTrimSeconds()
    {
        var site = db.AddSite();
        var guard = db.AddGuard();
        var start = new DateTime(2026, 10, 22, 8, 0, 37);

        int id = await service.CreateAsync(new ShiftFormViewModel
        {
            SiteId = site.Id,
            GuardId = guard.Id,
            StartTime = start,
            EndTime = start.AddHours(12),
            Notes = "  ",
        });

        var created = await service.GetDetailsAsync(id);
        Assert.Equal(new DateTime(2026, 10, 22, 8, 0, 0), created!.StartTime);
        Assert.Null(created.Notes);
        Assert.Equal("Иван Петров", created.GuardName);

        var form = await service.GetForEditAsync(id);
        form!.Notes = "Носете фенер";
        Assert.True(await service.UpdateAsync(form));
        Assert.Equal("Носете фенер", (await service.GetDetailsAsync(id))!.Notes);

        Assert.True(await service.DeleteAsync(id));
        Assert.False(await service.DeleteAsync(id));
        Assert.False(await service.UpdateAsync(form));
        Assert.Null(await service.GetForEditAsync(id));
        Assert.Equal(0, await db.Context.Shifts.CountAsync());
    }

    [Fact]
    public void ShiftListItem_ComputesDurationNightAndOngoing()
    {
        var item = new ShiftListItemViewModel
        {
            StartTime = new DateTime(2026, 10, 14, 20, 0, 0),
            EndTime = new DateTime(2026, 10, 15, 8, 0, 0),
        };

        Assert.Equal(12, item.DurationHours);
        Assert.True(item.IsNightShift);
        Assert.True(item.IsOngoing(new DateTime(2026, 10, 15, 1, 0, 0)));
        Assert.False(item.IsOngoing(new DateTime(2026, 10, 15, 8, 0, 0)));
    }
}
