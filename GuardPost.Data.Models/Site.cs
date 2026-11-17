namespace GuardPost.Data.Models;

/// <summary>
/// A guarded location (object under protection) – office building, warehouse, shop, etc.
/// </summary>
public class Site : DeletableEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string City { get; set; } = null!;

    /// <summary>The client (company) that ordered the protection of the site.</summary>
    public string ClientName { get; set; } = null!;

    public string ContactPhone { get; set; } = null!;

    /// <summary>How many guards must be on duty at the same time.</summary>
    public int RequiredGuardsPerShift { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedOn { get; set; }

    public virtual ICollection<Shift> Shifts { get; set; } = new HashSet<Shift>();

    public virtual ICollection<Incident> Incidents { get; set; } = new HashSet<Incident>();
}
