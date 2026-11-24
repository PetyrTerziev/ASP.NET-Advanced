namespace GuardPost.Web.ViewModels.Incidents;

/// <summary>One timeline entry, returned as JSON by the incident updates API.</summary>
public class IncidentUpdateViewModel
{
    public int Id { get; set; }

    public string Text { get; set; } = null!;

    public DateTime CreatedOn { get; set; }

    public string CreatedOnText { get; set; } = null!;

    public string AuthorName { get; set; } = null!;
}
