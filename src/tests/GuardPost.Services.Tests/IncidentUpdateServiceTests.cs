using GuardPost.Services.Core;
using GuardPost.Services.Tests.Infrastructure;

namespace GuardPost.Services.Tests;

public class IncidentUpdateServiceTests : IDisposable
{
    private readonly TestDb db = new();
    private readonly IncidentUpdateService service;

    public IncidentUpdateServiceTests()
    {
        service = new IncidentUpdateService(db.Context, db.Clock);
    }

    public void Dispose() => db.Dispose();

    [Fact]
    public async Task AddAsync_StoresTrimmedTextAndReturnsAuthorName()
    {
        var incident = db.AddIncident(db.AddSite());
        var user = db.AddUser("Петя", "Иванова");

        var result = await service.AddAsync(incident.Id, user.Id, "  Уведомен е възложителят.  ");

        Assert.Equal("Уведомен е възложителят.", result.Text);
        Assert.Equal("Петя Иванова", result.AuthorName);
        Assert.Equal("14.10.2026 10:00", result.CreatedOnText);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task GetForIncidentAsync_ReturnsEntriesInChronologicalOrder()
    {
        var incident = db.AddIncident(db.AddSite());
        var otherIncident = db.AddIncident(db.AddSite("Офис"));
        var user = db.AddUser();

        await service.AddAsync(incident.Id, user.Id, "Първо действие");
        db.Clock.Now = TestDb.Now.AddHours(1);
        await service.AddAsync(incident.Id, user.Id, "Второ действие");
        await service.AddAsync(otherIncident.Id, user.Id, "Друг инцидент");

        var updates = await service.GetForIncidentAsync(incident.Id);

        Assert.Equal(2, updates.Count);
        Assert.Equal("Първо действие", updates[0].Text);
        Assert.Equal("Второ действие", updates[1].Text);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" x ")]
    public async Task AddAsync_TooShortText_Throws(string text)
    {
        var incident = db.AddIncident(db.AddSite());
        var user = db.AddUser();

        await Assert.ThrowsAsync<ArgumentException>(() => service.AddAsync(incident.Id, user.Id, text));
    }

    [Fact]
    public async Task AddAsync_TooLongText_Throws()
    {
        var incident = db.AddIncident(db.AddSite());
        var user = db.AddUser();

        await Assert.ThrowsAsync<ArgumentException>(() => service.AddAsync(incident.Id, user.Id, new string('а', 1001)));
    }

    [Fact]
    public async Task AddAsync_UnknownUserOrIncident_Throws()
    {
        var incident = db.AddIncident(db.AddSite());
        var user = db.AddUser();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddAsync(incident.Id, "missing", "Текст"));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.AddAsync(999, user.Id, "Текст"));
    }
}
