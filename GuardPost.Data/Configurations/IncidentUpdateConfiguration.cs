using GuardPost.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using static GuardPost.Common.EntityValidationConstants.IncidentUpdate;

namespace GuardPost.Data.Configurations;

public class IncidentUpdateConfiguration : IEntityTypeConfiguration<IncidentUpdate>
{
    public void Configure(EntityTypeBuilder<IncidentUpdate> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Text)
            .IsRequired()
            .HasMaxLength(TextMaxLength);

        builder.HasOne(u => u.Incident)
            .WithMany(i => i.Updates)
            .HasForeignKey(u => u.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.Author)
            .WithMany()
            .HasForeignKey(u => u.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(u => !u.Incident.Site.IsDeleted);
    }
}
