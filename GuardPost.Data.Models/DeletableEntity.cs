using GuardPost.Data.Models.Contracts;

namespace GuardPost.Data.Models;

/// <summary>
/// Base class for soft-deletable entities. It is not mapped to its own table –
/// EF Core maps the inherited property into the table of every derived entity.
/// </summary>
public abstract class DeletableEntity : IDeletableEntity
{
    public bool IsDeleted { get; set; }
}
