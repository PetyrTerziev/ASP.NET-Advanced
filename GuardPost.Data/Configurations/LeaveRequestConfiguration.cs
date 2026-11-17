using GuardPost.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using static GuardPost.Common.EntityValidationConstants.LeaveRequest;

namespace GuardPost.Data.Configurations;

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.StartDate)
            .HasColumnType("date");

        builder.Property(l => l.EndDate)
            .HasColumnType("date");

        builder.Property(l => l.Reason)
            .HasMaxLength(ReasonMaxLength);

        builder.Property(l => l.ReviewNote)
            .HasMaxLength(ReviewNoteMaxLength);

        builder.HasOne(l => l.Guard)
            .WithMany(g => g.LeaveRequests)
            .HasForeignKey(l => l.GuardId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.RequestedBy)
            .WithMany()
            .HasForeignKey(l => l.RequestedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.ReviewedBy)
            .WithMany()
            .HasForeignKey(l => l.ReviewedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => new { l.GuardId, l.StartDate });

        builder.HasQueryFilter(l => !l.Guard.IsDeleted);
    }
}
