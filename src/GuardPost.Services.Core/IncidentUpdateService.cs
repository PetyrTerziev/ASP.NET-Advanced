using Microsoft.EntityFrameworkCore;

using GuardPost.Common;
using GuardPost.Common.Providers;
using GuardPost.Data;
using GuardPost.Data.Models;
using GuardPost.Services.Core.Contracts;
using GuardPost.Web.ViewModels.Incidents;

using static GuardPost.Common.EntityValidationConstants.IncidentUpdate;

namespace GuardPost.Services.Core;

public class IncidentUpdateService : IIncidentUpdateService
{
    private readonly GuardPostDbContext dbContext;
    private readonly IDateTimeProvider clock;

    public IncidentUpdateService(GuardPostDbContext dbContext, IDateTimeProvider clock)
    {
        this.dbContext = dbContext;
        this.clock = clock;
    }

    public async Task<IReadOnlyList<IncidentUpdateViewModel>> GetForIncidentAsync(int incidentId)
    {
        var updates = await dbContext.IncidentUpdates
            .AsNoTracking()
            .Where(u => u.IncidentId == incidentId)
            .OrderBy(u => u.CreatedOn)
            .Select(u => new
            {
                u.Id,
                u.Text,
                u.CreatedOn,
                AuthorName = u.Author.FirstName + " " + u.Author.LastName,
            })
            .ToListAsync();

        return updates
            .Select(u => ToViewModel(u.Id, u.Text, u.CreatedOn, u.AuthorName))
            .ToList();
    }

    public async Task<IncidentUpdateViewModel> AddAsync(int incidentId, string authorId, string text)
    {
        // Defensive checks – the API validates the input model, but the service must stay safe on its own.
        string trimmed = text?.Trim() ?? string.Empty;
        if (trimmed.Length < TextMinLength || trimmed.Length > TextMaxLength)
        {
            throw new ArgumentException(
                string.Format(ValidationMessages.StringLength, "Текст", TextMaxLength, TextMinLength),
                nameof(text));
        }

        var author = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == authorId)
            .Select(u => new { u.FirstName, u.LastName })
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException(ValidationMessages.UserNotFound);

        bool incidentExists = await dbContext.Incidents.AnyAsync(i => i.Id == incidentId);
        if (!incidentExists)
        {
            throw new KeyNotFoundException($"Incident {incidentId} was not found.");
        }

        var update = new IncidentUpdate
        {
            IncidentId = incidentId,
            AuthorId = authorId,
            Text = trimmed,
            CreatedOn = clock.Now,
        };

        await dbContext.IncidentUpdates.AddAsync(update);
        await dbContext.SaveChangesAsync();

        return ToViewModel(update.Id, update.Text, update.CreatedOn, $"{author.FirstName} {author.LastName}");
    }

    private static IncidentUpdateViewModel ToViewModel(int id, string text, DateTime createdOn, string authorName)
        => new()
        {
            Id = id,
            Text = text,
            CreatedOn = createdOn,
            CreatedOnText = createdOn.ToString(ApplicationConstants.DateTimeFormat),
            AuthorName = authorName,
        };
}
