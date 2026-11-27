using Microsoft.EntityFrameworkCore;

using GuardPost.Data.Models.Enums;
using GuardPost.Services.Core;
using GuardPost.Services.Tests.Infrastructure;
using GuardPost.Web.ViewModels.Guards;

namespace GuardPost.Services.Tests;

public class GuardServiceTests : IDisposable
{
    private readonly TestDb db = new();
    private readonly GuardService service;

    public GuardServiceTests()
    {
        service = new GuardService(db.Context, db.Clock);
    }

    public void Dispose() => db.Dispose();

    [Fact]
    public async Task GetIndexAsync_FiltersByRankSearchAndActivity()
    {
        db.AddGuard("Иван", "Петров", rank: GuardRank.Supervisor);
        db.AddGuard("Мария", "Колева", rank: GuardRank.SeniorGuard);
        db.AddGuard("Тодор", "Маринов", active: false);

        var all = await service.GetIndexAsync(null, null, showInactive: true, 1, 10);
        var active = await service.GetIndexAsync(null, null, showInactive: false, 1, 10);
        var seniors = await service.GetIndexAsync(null, GuardRank.SeniorGuard, false, 1, 10);
        var search = await service.GetIndexAsync("Петр", null, false, 1, 10);

        Assert.Equal(3, all.Result.TotalCount);
        Assert.Equal(2, active.Result.TotalCount);
        Assert.Equal("Мария Колева", seniors.Result.Items.Single().FullName);
        Assert.Equal("Иван Петров", search.Result.Items.Single().FullName);
    }

    [Fact]
    public async Task GetIndexAsync_HoursThisMonth_ClipsShiftsCrossingTheMonthBoundary()
    {
        var site = db.AddSite();
        var guard = db.AddGuard();
        db.AddShift(site, guard, new DateTime(2026, 9, 30, 20, 0, 0));   // 8 h of it fall in October
        db.AddShift(site, guard, new DateTime(2026, 10, 5, 8, 0, 0));    // 12 h
        db.AddShift(site, guard, new DateTime(2026, 9, 10, 8, 0, 0));    // previous month

        var item = (await service.GetIndexAsync(null, null, false, 1, 10)).Result.Items.Single();

        Assert.Equal(20, item.HoursThisMonth, 3);
    }

    [Fact]
    public async Task GetDetailsAsync_ReturnsMonthlyStatsUpcomingShiftsAndReportedIncidents()
    {
        var site = db.AddSite();
        var guard = db.AddGuard();
        db.AddShift(site, guard, TestDb.Now.AddDays(-2));
        db.AddShift(site, guard, TestDb.Now.AddDays(1));
        db.AddIncident(site, reportedBy: guard);

        var details = await service.GetDetailsAsync(guard.Id);

        Assert.NotNull(details);
        Assert.Equal(2, details!.ShiftsThisMonth);
        Assert.Equal(24, details.HoursThisMonth, 3);
        Assert.Single(details.UpcomingShifts);
        Assert.Single(details.ReportedIncidents);
        Assert.Equal("Иван Петров", details.FullName);
        Assert.Null(await service.GetDetailsAsync(999));
    }

    [Fact]
    public async Task CreateAsync_NormalizesLicenseNumberAndDate()
    {
        var model = new GuardFormViewModel
        {
            FirstName = " Асен ",
            LastName = " Тодоров ",
            LicenseNumber = " ох-555555 ",
            PhoneNumber = "+359 888 111 222",
            Email = "  ",
            HireDate = new DateTime(2024, 5, 1, 15, 30, 0),
            Rank = GuardRank.Trainee,
            IsActive = true,
        };

        int id = await service.CreateAsync(model);
        var guard = await db.Context.Guards.FirstAsyncById(id);

        Assert.Equal("ОХ-555555", guard.LicenseNumber);
        Assert.Equal("Асен", guard.FirstName);
        Assert.Null(guard.Email);
        Assert.Equal(new DateTime(2024, 5, 1), guard.HireDate);
    }

    [Fact]
    public async Task GetForEditAndUpdateAsync_RoundTrip()
    {
        var guard = db.AddGuard();
        var model = await service.GetForEditAsync(guard.Id);
        model!.Rank = GuardRank.Supervisor;
        model.PhoneNumber = "+359 899 999 999";

        Assert.True(await service.UpdateAsync(model));
        Assert.False(await service.UpdateAsync(new GuardFormViewModel { Id = 999, FirstName = "a", LastName = "b", LicenseNumber = "ОХ-000000", PhoneNumber = "1", HireDate = DateTime.Today, Rank = GuardRank.Guard }));
        Assert.Null(await service.GetForEditAsync(999));

        var updated = await db.Context.Guards.FirstAsyncById(guard.Id);
        Assert.Equal(GuardRank.Supervisor, updated.Rank);
        Assert.Equal("+359 899 999 999", updated.PhoneNumber);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesGuardAndRemovesFutureShifts()
    {
        var site = db.AddSite();
        var guard = db.AddGuard();
        db.AddShift(site, guard, TestDb.Now.AddDays(-1));
        db.AddShift(site, guard, TestDb.Now.AddDays(3));

        var deleteModel = await service.GetForDeleteAsync(guard.Id);
        Assert.Equal(1, deleteModel!.FutureShiftsCount);

        Assert.True(await service.DeleteAsync(guard.Id));
        Assert.False(await service.DeleteAsync(guard.Id));

        var stored = await db.Context.Guards.FirstAsyncById(guard.Id);
        Assert.True(stored.IsDeleted);
        Assert.Equal(1, await db.Context.Shifts.IgnoreQueryFilters().CountAsync(s => s.GuardId == guard.Id));
    }

    [Fact]
    public async Task IsLicenseNumberTakenAsync_ChecksAlsoDeletedGuardsAndIgnoresCurrentGuard()
    {
        var guard = db.AddGuard(license: "ОХ-123456");
        var deleted = db.AddGuard(license: "ОХ-654321");
        await service.DeleteAsync(deleted.Id);

        Assert.True(await service.IsLicenseNumberTakenAsync("ох-123456"));
        Assert.True(await service.IsLicenseNumberTakenAsync("ОХ-654321"));
        Assert.False(await service.IsLicenseNumberTakenAsync("ОХ-123456", guard.Id));
        Assert.False(await service.IsLicenseNumberTakenAsync("ОХ-000001"));
    }

    [Fact]
    public async Task GetGuardIdForUserAsync_ReturnsLinkedGuard()
    {
        var guard = db.AddGuard();
        var linked = db.AddUser(guard: guard);
        var notLinked = db.AddUser();

        Assert.Equal(guard.Id, await service.GetGuardIdForUserAsync(linked.Id));
        Assert.Null(await service.GetGuardIdForUserAsync(notLinked.Id));
        Assert.Null(await service.GetGuardIdForUserAsync("missing"));
    }

    [Fact]
    public async Task GetDropdownAsync_ContainsLicenseAndRespectsActivity()
    {
        db.AddGuard("Иван", "Петров", license: "ОХ-111111");
        var inactive = db.AddGuard("Тодор", "Маринов", active: false);

        var active = (await service.GetDropdownAsync(onlyActive: true)).ToList();
        var included = (await service.GetDropdownAsync(onlyActive: true, includeId: inactive.Id)).ToList();

        Assert.Single(active);
        Assert.Equal("Иван Петров (ОХ-111111)", active[0].Text);
        Assert.Equal(2, included.Count);
    }
}
