using GuardPost.Data.Models.Enums;
using GuardPost.Services.Core;
using GuardPost.Services.Tests.Infrastructure;

namespace GuardPost.Services.Tests;

public class DashboardServiceTests : IDisposable
{
    private readonly TestDb db = new();
    private readonly DashboardService service;

    public DashboardServiceTests()
    {
        service = new DashboardService(db.Context, db.Clock);
    }

    public void Dispose() => db.Dispose();

    [Fact]
    public async Task GetDashboardAsync_ReturnsCountsCoverageShiftsAndIncidents()
    {
        var covered = db.AddSite("А Склад", required: 1);
        var understaffed = db.AddSite("Б Магазин", required: 2);
        db.AddSite("В Неактивен", active: false);
        var guard = db.AddGuard();
        var other = db.AddGuard("Мария", "Колева");
        db.AddGuard("Тодор", "Маринов", active: false);

        db.AddShift(covered, guard, TestDb.Now.AddHours(-2));          // on duty now
        db.AddShift(understaffed, other, TestDb.Now.AddHours(-1));     // on duty now
        db.AddShift(covered, other, db.Today.AddDays(-1).AddHours(20)); // night shift that ended this morning
        db.AddShift(covered, guard, db.Today.AddDays(2));               // not today

        db.AddIncident(covered, "Ниска", IncidentSeverity.Low);
        db.AddIncident(covered, "Критична", IncidentSeverity.Critical);
        db.AddIncident(covered, "Решена", status: IncidentStatus.Resolved);

        var model = await service.GetDashboardAsync();

        Assert.Equal(2, model.ActiveSites);
        Assert.Equal(2, model.ActiveGuards);
        Assert.Equal(3, model.ShiftsToday);
        Assert.Equal(2, model.OpenIncidents);
        Assert.Equal("Критична", model.LatestOpenIncidents.First().Title);

        var coverage = model.Coverage.ToList();
        Assert.True(coverage[0].IsCovered);
        Assert.False(coverage[1].IsCovered);
        Assert.Equal(1, coverage[1].OnDuty);
    }

    [Fact]
    public async Task GetCoverageAsync_EmptyDatabase_ReturnsEmptyList()
    {
        Assert.Empty(await service.GetCoverageAsync());
    }
}
