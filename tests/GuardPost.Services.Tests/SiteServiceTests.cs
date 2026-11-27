using Microsoft.EntityFrameworkCore;

using GuardPost.Data.Models.Enums;
using GuardPost.Services.Core;
using GuardPost.Services.Tests.Infrastructure;
using GuardPost.Web.ViewModels.Sites;

namespace GuardPost.Services.Tests;

public class SiteServiceTests : IDisposable
{
    private readonly TestDb db = new();
    private readonly SiteService service;

    public SiteServiceTests()
    {
        service = new SiteService(db.Context, db.Clock);
    }

    public void Dispose() => db.Dispose();

    [Fact]
    public async Task GetIndexAsync_ByDefault_ReturnsOnlyActiveSites()
    {
        db.AddSite("Активен");
        db.AddSite("Неактивен", active: false);

        var result = await service.GetIndexAsync(null, null, showInactive: false, page: 1, pageSize: 10);

        Assert.Single(result.Result.Items);
        Assert.Equal("Активен", result.Result.Items[0].Name);
    }

    [Fact]
    public async Task GetIndexAsync_ShowInactive_ReturnsAllSitesActiveFirst()
    {
        db.AddSite("Б неактивен", active: false);
        db.AddSite("А активен");

        var result = await service.GetIndexAsync(null, null, showInactive: true, page: 1, pageSize: 10);

        Assert.Equal(2, result.Result.TotalCount);
        Assert.True(result.Result.Items[0].IsActive);
    }

    [Fact]
    public async Task GetIndexAsync_FiltersByCityAndSearchTerm()
    {
        db.AddSite("Склад Север", city: "Пловдив");
        db.AddSite("Склад Юг", city: "Стара Загора");
        db.AddSite("Офис", city: "Стара Загора", client: "Берое АД");

        var byCity = await service.GetIndexAsync(null, "Стара Загора", false, 1, 10);
        var bySearch = await service.GetIndexAsync("Берое", null, false, 1, 10);

        Assert.Equal(2, byCity.Result.TotalCount);
        Assert.Single(bySearch.Result.Items);
        Assert.Equal("Офис", bySearch.Result.Items[0].Name);
        Assert.Equal(new[] { "Пловдив", "Стара Загора" }, byCity.Cities);
    }

    [Fact]
    public async Task GetIndexAsync_PageOutOfRange_IsClampedToLastPage()
    {
        for (int i = 0; i < 5; i++)
        {
            db.AddSite($"Обект {i}");
        }

        var result = await service.GetIndexAsync(null, null, false, page: 99, pageSize: 2);

        Assert.Equal(3, result.Result.Page);
        Assert.Equal(3, result.Result.TotalPages);
        Assert.Single(result.Result.Items);
        Assert.True(result.Result.HasPreviousPage);
        Assert.False(result.Result.HasNextPage);
    }

    [Fact]
    public async Task GetIndexAsync_CountsUpcomingShiftsAndOpenIncidents()
    {
        var site = db.AddSite();
        var guard = db.AddGuard();
        db.AddShift(site, guard, TestDb.Now.AddDays(-3));
        db.AddShift(site, guard, TestDb.Now.AddDays(1));
        db.AddIncident(site, status: IncidentStatus.Open);
        db.AddIncident(site, status: IncidentStatus.Resolved);

        var item = (await service.GetIndexAsync(null, null, false, 1, 10)).Result.Items.Single();

        Assert.Equal(1, item.UpcomingShiftsCount);
        Assert.Equal(1, item.OpenIncidentsCount);
    }

    [Fact]
    public async Task GetDetailsAsync_UnknownId_ReturnsNull()
    {
        Assert.Null(await service.GetDetailsAsync(42));
    }

    [Fact]
    public async Task GetDetailsAsync_ReturnsUpcomingShiftsIncidentsAndGuardsOnDuty()
    {
        var site = db.AddSite(required: 2);
        var guard = db.AddGuard();
        var other = db.AddGuard("Мария", "Колева");
        db.AddShift(site, guard, TestDb.Now.AddHours(-2));
        db.AddShift(site, other, TestDb.Now.AddDays(1));
        db.AddIncident(site);

        var details = await service.GetDetailsAsync(site.Id);

        Assert.NotNull(details);
        Assert.Equal(1, details!.GuardsOnDutyNow);
        Assert.Equal(2, details.UpcomingShifts.Count());
        Assert.Single(details.RecentIncidents);
    }

    [Fact]
    public async Task CreateAsync_TrimsValuesAndSetsCreatedOn()
    {
        var model = new SiteFormViewModel
        {
            Name = "  Нов обект  ",
            Address = " ул. Първа 1 ",
            City = " Казанлък ",
            ClientName = " Клиент ООД ",
            ContactPhone = " +359 431 00 000 ",
            RequiredGuardsPerShift = 2,
            Description = "   ",
            IsActive = true,
        };

        int id = await service.CreateAsync(model);
        var site = await db.Context.Sites.FirstAsyncById(id);

        Assert.Equal("Нов обект", site.Name);
        Assert.Equal("Казанлък", site.City);
        Assert.Null(site.Description);
        Assert.Equal(TestDb.Now, site.CreatedOn);
    }

    [Fact]
    public async Task GetForEditAsync_ReturnsCurrentValues()
    {
        var site = db.AddSite("Обект за редакция");

        var model = await service.GetForEditAsync(site.Id);

        Assert.NotNull(model);
        Assert.Equal("Обект за редакция", model!.Name);
        Assert.Null(await service.GetForEditAsync(999));
    }

    [Fact]
    public async Task UpdateAsync_ChangesValues_AndReturnsFalseForUnknownSite()
    {
        var site = db.AddSite();
        var model = await service.GetForEditAsync(site.Id);
        model!.Name = "Ново име";
        model.IsActive = false;

        Assert.True(await service.UpdateAsync(model));
        Assert.False(await service.UpdateAsync(new SiteFormViewModel { Id = 999, Name = "x", Address = "x", City = "x", ClientName = "x", ContactPhone = "1" }));

        var updated = await db.Context.Sites.FirstAsyncById(site.Id);
        Assert.Equal("Ново име", updated.Name);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task GetForDeleteAsync_CountsOnlyFutureShifts()
    {
        var site = db.AddSite();
        var guard = db.AddGuard();
        db.AddShift(site, guard, TestDb.Now.AddDays(-1));
        db.AddShift(site, guard, TestDb.Now.AddDays(2));
        db.AddShift(site, guard, TestDb.Now.AddDays(4));

        var model = await service.GetForDeleteAsync(site.Id);

        Assert.Equal(2, model!.FutureShiftsCount);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesSiteAndRemovesFutureShifts()
    {
        var site = db.AddSite();
        var guard = db.AddGuard();
        var past = db.AddShift(site, guard, TestDb.Now.AddDays(-1));
        db.AddShift(site, guard, TestDb.Now.AddDays(2));

        bool deleted = await service.DeleteAsync(site.Id);

        Assert.True(deleted);
        var stored = await db.Context.Sites.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == site.Id);
        Assert.True(stored!.IsDeleted);
        Assert.False(stored.IsActive);
        var remaining = await db.Context.Shifts.IgnoreQueryFilters().Where(s => s.SiteId == site.Id).ToListAsync();
        Assert.Single(remaining);
        Assert.Equal(past.Id, remaining[0].Id);
        Assert.False(await service.DeleteAsync(site.Id));
    }

    [Fact]
    public async Task GetDropdownAsync_OnlyActive_StillIncludesRequestedSite()
    {
        var active = db.AddSite("Активен");
        var inactive = db.AddSite("Неактивен", active: false);

        var onlyActive = (await service.GetDropdownAsync(onlyActive: true)).ToList();
        var withIncluded = (await service.GetDropdownAsync(onlyActive: true, includeId: inactive.Id)).ToList();
        var all = (await service.GetDropdownAsync(onlyActive: false)).ToList();

        Assert.Single(onlyActive);
        Assert.Equal(active.Id, onlyActive[0].Id);
        Assert.Equal(2, withIncluded.Count);
        Assert.Equal(2, all.Count);
        Assert.Contains("(Стара Загора)", onlyActive[0].Text);
    }
}
