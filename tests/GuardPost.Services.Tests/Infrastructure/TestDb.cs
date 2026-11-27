using Microsoft.EntityFrameworkCore;

using GuardPost.Data;
using GuardPost.Data.Models;
using GuardPost.Data.Models.Enums;

namespace GuardPost.Services.Tests.Infrastructure;

/// <summary>
/// A fresh EF Core InMemory database per test plus small builders for test data.
/// "Now" is Wednesday, 14 October 2026, 10:00.
/// </summary>
public sealed class TestDb : IDisposable
{
    public static readonly DateTime Now = new(2026, 10, 14, 10, 0, 0);

    private int licenseCounter = 100000;

    public TestDb()
    {
        var options = new DbContextOptionsBuilder<GuardPostDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        Context = new GuardPostDbContext(options);
        Clock = new FakeDateTimeProvider(Now);
    }

    public GuardPostDbContext Context { get; }

    public FakeDateTimeProvider Clock { get; }

    public DateTime Today => Clock.Today;

    public Site AddSite(string name = "Склад Б", string city = "Стара Загора", bool active = true, int required = 1, string client = "Тракия ООД")
    {
        var site = new Site
        {
            Name = name,
            Address = "ул. „Индустриална“ 12",
            City = city,
            ClientName = client,
            ContactPhone = "+359 42 600 111",
            RequiredGuardsPerShift = required,
            IsActive = active,
            CreatedOn = Now.AddMonths(-1),
        };

        Context.Sites.Add(site);
        Context.SaveChanges();
        return site;
    }

    public Guard AddGuard(string firstName = "Иван", string lastName = "Петров", bool active = true, GuardRank rank = GuardRank.Guard, string? license = null)
    {
        var guard = new Guard
        {
            FirstName = firstName,
            LastName = lastName,
            LicenseNumber = license ?? $"ОХ-{licenseCounter++}",
            PhoneNumber = "+359 888 000 000",
            HireDate = new DateTime(2022, 1, 10),
            Rank = rank,
            IsActive = active,
        };

        Context.Guards.Add(guard);
        Context.SaveChanges();
        return guard;
    }

    public Shift AddShift(Site site, Guard guard, DateTime start, double hours = 12, string? notes = null)
    {
        var shift = new Shift
        {
            SiteId = site.Id,
            GuardId = guard.Id,
            StartTime = start,
            EndTime = start.AddHours(hours),
            Notes = notes,
        };

        Context.Shifts.Add(shift);
        Context.SaveChanges();
        return shift;
    }

    public Incident AddIncident(
        Site site,
        string title = "Повредена ограда",
        IncidentSeverity severity = IncidentSeverity.Medium,
        IncidentStatus status = IncidentStatus.Open,
        DateTime? occurredOn = null,
        Guard? reportedBy = null,
        string? createdById = null)
    {
        var incident = new Incident
        {
            Title = title,
            Description = "Описание на инцидента.",
            Severity = severity,
            Status = status,
            OccurredOn = occurredOn ?? Now.AddHours(-2),
            CreatedOn = Now.AddHours(-1),
            SiteId = site.Id,
            ReportedByGuardId = reportedBy?.Id,
            CreatedById = createdById,
        };

        Context.Incidents.Add(incident);
        Context.SaveChanges();
        return incident;
    }

    public ApplicationUser AddUser(string firstName = "Петя", string lastName = "Иванова", Guard? guard = null, string? email = null)
    {
        email ??= $"{Guid.NewGuid():N}@example.com";
        var user = new ApplicationUser
        {
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            FirstName = firstName,
            LastName = lastName,
            GuardId = guard?.Id,
            CreatedOn = Now.AddDays(-10),
            SecurityStamp = Guid.NewGuid().ToString(),
        };

        Context.Users.Add(user);
        Context.SaveChanges();
        return user;
    }

    public LeaveRequest AddLeave(Guard guard, DateTime start, DateTime end, LeaveStatus status = LeaveStatus.Pending, string? requestedById = null)
    {
        var leave = new LeaveRequest
        {
            GuardId = guard.Id,
            StartDate = start.Date,
            EndDate = end.Date,
            Type = LeaveType.Paid,
            Status = status,
            CreatedOn = Now.AddDays(-1),
            RequestedById = requestedById,
        };

        Context.LeaveRequests.Add(leave);
        Context.SaveChanges();
        return leave;
    }

    public void Dispose() => Context.Dispose();
}
