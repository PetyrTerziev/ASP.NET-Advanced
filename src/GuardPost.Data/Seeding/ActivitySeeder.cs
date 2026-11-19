using GuardPost.Data.Models;
using GuardPost.Data.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace GuardPost.Data.Seeding;

/// <summary>
/// Demo leave requests and incident timeline entries. Runs only when both tables are empty
/// and the demo accounts exist (see <see cref="IdentitySeeder"/>).
/// </summary>
public static class ActivitySeeder
{
    public static async Task SeedAsync(GuardPostDbContext context, DateTime today)
    {
        if (await context.LeaveRequests.IgnoreQueryFilters().AnyAsync()
            || await context.IncidentUpdates.IgnoreQueryFilters().AnyAsync())
        {
            return;
        }

        var admin = await context.Users.FirstOrDefaultAsync(u => u.Email == IdentitySeeder.AdminEmail);
        var guardUsers = await context.Users
            .Where(u => u.GuardId != null)
            .OrderBy(u => u.Email)
            .ToListAsync();

        if (admin is null || guardUsers.Count == 0)
        {
            return;
        }

        var ivan = guardUsers.FirstOrDefault(u => u.Email == IdentitySeeder.GuardAccounts[0].Email) ?? guardUsers[0];
        var maria = guardUsers.FirstOrDefault(u => u.Email == IdentitySeeder.GuardAccounts[1].Email) ?? guardUsers[0];
        var georgi = guardUsers.FirstOrDefault(u => u.Email == IdentitySeeder.GuardAccounts[2].Email) ?? guardUsers[0];

        // The generated schedule ends 14 days from today, so leaves after that do not clash with shifts.
        await context.LeaveRequests.AddRangeAsync(
            new LeaveRequest
            {
                GuardId = georgi.GuardId!.Value,
                StartDate = today.AddDays(20),
                EndDate = today.AddDays(26),
                Type = LeaveType.Paid,
                Reason = "Семейна почивка.",
                Status = LeaveStatus.Approved,
                CreatedOn = today.AddDays(-5),
                RequestedById = georgi.Id,
                ReviewedById = admin.Id,
                ReviewedOn = today.AddDays(-4),
                ReviewNote = "Одобрено – графикът ще бъде съобразен.",
            },
            new LeaveRequest
            {
                GuardId = maria.GuardId!.Value,
                StartDate = today.AddDays(30),
                EndDate = today.AddDays(34),
                Type = LeaveType.Paid,
                Reason = "Пътуване извън страната.",
                Status = LeaveStatus.Pending,
                CreatedOn = today.AddDays(-1),
                RequestedById = maria.Id,
            },
            new LeaveRequest
            {
                GuardId = ivan.GuardId!.Value,
                StartDate = today.AddDays(-30),
                EndDate = today.AddDays(-28),
                Type = LeaveType.Unpaid,
                Reason = "Лични ангажименти.",
                Status = LeaveStatus.Rejected,
                CreatedOn = today.AddDays(-40),
                RequestedById = ivan.Id,
                ReviewedById = admin.Id,
                ReviewedOn = today.AddDays(-38),
                ReviewNote = "Няма кой да поеме смените в този период.",
            });

        var incidents = await context.Incidents
            .OrderBy(i => i.OccurredOn)
            .ToListAsync();

        foreach (var incident in incidents)
        {
            // Incidents reported by a guard that has an account are attributed to that account.
            var reporter = guardUsers.FirstOrDefault(u => u.GuardId == incident.ReportedByGuardId);
            if (reporter != null && incident.CreatedById == null)
            {
                incident.CreatedById = reporter.Id;
            }

            await context.IncidentUpdates.AddAsync(new IncidentUpdate
            {
                IncidentId = incident.Id,
                AuthorId = reporter?.Id ?? admin.Id,
                CreatedOn = incident.OccurredOn.AddMinutes(20),
                Text = "Сигналът е регистриран. Уведомен е дежурният началник смяна.",
            });

            if (incident.Status != IncidentStatus.Open)
            {
                await context.IncidentUpdates.AddAsync(new IncidentUpdate
                {
                    IncidentId = incident.Id,
                    AuthorId = admin.Id,
                    CreatedOn = incident.OccurredOn.AddHours(2),
                    Text = incident.Status == IncidentStatus.Resolved
                        ? "Проверено на място – случаят е приключен."
                        : "Възложителят е уведомен, очакваме решение.",
                });
            }
        }

        await context.SaveChangesAsync();
    }
}
