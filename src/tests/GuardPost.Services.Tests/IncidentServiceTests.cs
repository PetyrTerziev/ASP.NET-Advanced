using Microsoft.EntityFrameworkCore;

using GuardPost.Data.Models.Enums;
using GuardPost.Services.Core;
using GuardPost.Services.Tests.Infrastructure;
using GuardPost.Web.ViewModels.Incidents;

namespace GuardPost.Services.Tests;

public class IncidentServiceTests : IDisposable
{
    private readonly TestDb db = new();
    private readonly IncidentService service;

    public IncidentServiceTests()
    {
        service = new IncidentService(db.Context, db.Clock);
    }

    public void Dispose() => db.Dispose();

    [Fact]
    public async Task GetIndexAsync_FiltersAndListsOpenIncidentsFirst()
    {
        var site = db.AddSite();
        var other = db.AddSite("Офис");
        db.AddIncident(site, "Стар решен", IncidentSeverity.Low, IncidentStatus.Resolved, TestDb.Now.AddHours(-1));
        db.AddIncident(site, "Отворен", IncidentSeverity.High, IncidentStatus.Open, TestDb.Now.AddDays(-2));
        db.AddIncident(other, "Аларма в офиса", IncidentSeverity.Critical, IncidentStatus.InProgress);

        var all = await service.GetIndexAsync(null, null, null, null, 1, 10);
        var critical = await service.GetIndexAsync(null, IncidentSeverity.Critical, null, null, 1, 10);
        var resolved = await service.GetIndexAsync(null, null, IncidentStatus.Resolved, null, 1, 10);
        var bySite = await service.GetIndexAsync(null, null, null, other.Id, 1, 10);
        var search = await service.GetIndexAsync("Аларма", null, null, null, 1, 10);

        Assert.Equal(3, all.Result.TotalCount);
        Assert.Equal(IncidentStatus.Resolved, all.Result.Items.Last().Status);
        Assert.Single(critical.Result.Items);
        Assert.Equal("Стар решен", resolved.Result.Items.Single().Title);
        Assert.Single(bySite.Result.Items);
        Assert.Equal("Аларма в офиса", search.Result.Items.Single().Title);
    }

    [Fact]
    public async Task GetDetailsAsync_ReturnsSiteReporterAndCreator()
    {
        var site = db.AddSite();
        var guard = db.AddGuard();
        var user = db.AddUser("Георги", "Димитров");
        var incident = db.AddIncident(site, reportedBy: guard, createdById: user.Id);

        var details = await service.GetDetailsAsync(incident.Id);

        Assert.NotNull(details);
        Assert.Equal("Иван Петров", details!.ReportedBy);
        Assert.Equal("Георги Димитров", details.CreatedByName);
        Assert.Contains("Стара Загора", details.SiteAddress);
        Assert.Null(await service.GetDetailsAsync(999));
    }

    [Fact]
    public async Task ValidateAsync_UnknownSiteOrGuard_ReturnsErrors()
    {
        var site = db.AddSite();
        var valid = new IncidentFormViewModel { SiteId = site.Id };
        var invalid = new IncidentFormViewModel { SiteId = 999, ReportedByGuardId = 999 };

        Assert.Empty(await service.ValidateAsync(valid));
        Assert.Equal(2, (await service.ValidateAsync(invalid)).Count);
    }

    [Fact]
    public async Task CreateAsync_StoresCreatorAndTrimsText()
    {
        var site = db.AddSite();
        var admin = db.AddUser();
        var model = new IncidentFormViewModel
        {
            Title = "  Счупен прозорец  ",
            Description = " Задният вход. ",
            OccurredOn = TestDb.Now.AddHours(-1),
            Severity = IncidentSeverity.High,
            Status = IncidentStatus.Resolved,
            ResolutionNotes = " Подменен. ",
            SiteId = site.Id,
        };

        int id = await service.CreateAsync(model, admin.Id);
        var stored = await db.Context.Incidents.SingleAsync(i => i.Id == id);

        Assert.Equal("Счупен прозорец", stored.Title);
        Assert.Equal("Подменен.", stored.ResolutionNotes);
        Assert.Equal(admin.Id, stored.CreatedById);
        Assert.Equal(TestDb.Now, stored.CreatedOn);
    }

    [Fact]
    public async Task ReportAsync_CreatesOpenIncidentLinkedToUserAndGuard()
    {
        var site = db.AddSite();
        var guard = db.AddGuard();
        var user = db.AddUser(guard: guard);

        int id = await service.ReportAsync(new ReportIncidentViewModel
        {
            Title = "Непознат автомобил",
            Description = "На рампа 4 повече от час.",
            OccurredOn = TestDb.Now.AddMinutes(-30),
            Severity = IncidentSeverity.Medium,
            SiteId = site.Id,
        }, user.Id, guard.Id);

        var stored = await db.Context.Incidents.SingleAsync(i => i.Id == id);
        Assert.Equal(IncidentStatus.Open, stored.Status);
        Assert.Equal(guard.Id, stored.ReportedByGuardId);
        Assert.Equal(user.Id, stored.CreatedById);
    }

    [Fact]
    public async Task GetForUserAsync_ReturnsOnlyOwnIncidents()
    {
        var site = db.AddSite();
        var me = db.AddUser();
        var someoneElse = db.AddUser("Мария", "Колева");
        db.AddIncident(site, "Мой 1", createdById: me.Id);
        db.AddIncident(site, "Мой 2", createdById: me.Id, occurredOn: TestDb.Now.AddDays(-1));
        db.AddIncident(site, "Чужд", createdById: someoneElse.Id);

        var mine = await service.GetForUserAsync(me.Id, 1, 10);

        Assert.Equal(2, mine.Result.TotalCount);
        Assert.Equal("Мой 1", mine.Result.Items[0].Title);
        Assert.DoesNotContain(mine.Result.Items, i => i.Title == "Чужд");
    }

    [Fact]
    public async Task CanAccessAsync_AdminSeesAll_UserOnlyOwn()
    {
        var site = db.AddSite();
        var owner = db.AddUser();
        var other = db.AddUser("Мария", "Колева");
        var incident = db.AddIncident(site, createdById: owner.Id);

        Assert.True(await service.CanAccessAsync(incident.Id, owner.Id, isAdministrator: false));
        Assert.False(await service.CanAccessAsync(incident.Id, other.Id, isAdministrator: false));
        Assert.True(await service.CanAccessAsync(incident.Id, other.Id, isAdministrator: true));
        Assert.False(await service.CanAccessAsync(999, owner.Id, isAdministrator: true));
    }

    [Fact]
    public async Task GetForEditUpdateAndDelete_Work()
    {
        var site = db.AddSite();
        var incident = db.AddIncident(site);

        var form = await service.GetForEditAsync(incident.Id);
        form!.Status = IncidentStatus.InProgress;
        Assert.True(await service.UpdateAsync(form));
        Assert.Equal(IncidentStatus.InProgress, (await service.GetDetailsAsync(incident.Id))!.Status);

        Assert.True(await service.DeleteAsync(incident.Id));
        Assert.False(await service.DeleteAsync(incident.Id));
        Assert.False(await service.UpdateAsync(form));
        Assert.Null(await service.GetForEditAsync(incident.Id));
    }
}
