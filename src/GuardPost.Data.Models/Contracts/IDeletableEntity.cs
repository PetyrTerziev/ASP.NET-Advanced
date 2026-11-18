namespace GuardPost.Data.Models.Contracts;

/// <summary>Entities that are never physically removed – they are hidden by a global query filter instead.</summary>
public interface IDeletableEntity
{
    bool IsDeleted { get; set; }
}
