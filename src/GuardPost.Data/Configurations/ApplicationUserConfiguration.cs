using GuardPost.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using static GuardPost.Common.EntityValidationConstants.User;

namespace GuardPost.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(FirstNameMaxLength);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(LastNameMaxLength);

        // One account per guard (a guard may also have no account).
        builder.HasOne(u => u.Guard)
            .WithOne()
            .HasForeignKey<ApplicationUser>(u => u.GuardId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
