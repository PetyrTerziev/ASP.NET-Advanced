using GuardPost.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GuardPost.Data;

/// <summary>
/// Application database. Inherits the ASP.NET Identity tables (users, roles, claims, ...)
/// and adds the domain tables of GuardPost.
/// </summary>
public class GuardPostDbContext : IdentityDbContext<ApplicationUser>
{
    public GuardPostDbContext(DbContextOptions<GuardPostDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Site> Sites { get; set; } = null!;

    public virtual DbSet<Guard> Guards { get; set; } = null!;

    public virtual DbSet<Shift> Shifts { get; set; } = null!;

    public virtual DbSet<Incident> Incidents { get; set; } = null!;

    public virtual DbSet<IncidentUpdate> IncidentUpdates { get; set; } = null!;

    public virtual DbSet<LeaveRequest> LeaveRequests { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configures the Identity tables – must run first.
        base.OnModelCreating(modelBuilder);

        // Picks up every IEntityTypeConfiguration<T> in this assembly (see /Configurations).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GuardPostDbContext).Assembly);
    }
}
