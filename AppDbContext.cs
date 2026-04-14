using Microsoft.EntityFrameworkCore;
using PolicePortal.Models;

namespace PolicePortal.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Incident>       Incidents       => Set<Incident>();
    public DbSet<Officer>        Officers        => Set<Officer>();
    public DbSet<Location>       Locations       => Set<Location>();
    public DbSet<IncidentStatus> IncidentStatuses => Set<IncidentStatus>();
    public DbSet<IncidentType>   IncidentTypes   => Set<IncidentType>();
 
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── Incident ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Incident>(e =>
        {
            e.HasIndex(i => i.CaseNumber).IsUnique();
            e.Property(i => i.CaseNumber).HasMaxLength(20);
            e.Property(i => i.Description).HasMaxLength(2000);
            e.Property(i => i.IsDeleted).HasDefaultValue(false);
            e.ToTable(t => t.HasTrigger("trg_Incidents_UpdatedAt"));

            e.HasOne(i => i.IncidentType)
             .WithMany()
             .HasForeignKey(i => i.IncidentTypeId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(i => i.IncidentStatus)
             .WithMany()
             .HasForeignKey(i => i.IncidentStatusId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(i => i.Location)
             .WithMany(l => l.Incidents)
             .HasForeignKey(i => i.LocationId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(i => i.Officer)
             .WithMany(o => o.Incidents)
             .HasForeignKey(i => i.OfficerId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Officer ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Officer>(e =>
        {
            e.HasIndex(o => o.BadgeNumber).IsUnique();
            e.Property(o => o.BadgeNumber).HasMaxLength(20);
            e.Property(o => o.Precinct).HasMaxLength(50);
        });

        // ── Location ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Location>(e =>
        {
            e.Property(l => l.Precinct).HasMaxLength(50);
        });

        // ── Seed lookup data ──────────────────────────────────────────────────
        modelBuilder.Entity<IncidentStatus>().HasData(
            new IncidentStatus { Id = 1, Name = "Open",                 ColorHex = "#EF4444" },
            new IncidentStatus { Id = 2, Name = "Under Investigation",  ColorHex = "#F59E0B" },
            new IncidentStatus { Id = 3, Name = "Closed",              ColorHex = "#10B981" },
            new IncidentStatus { Id = 4, Name = "Pending Review",      ColorHex = "#6366F1" }
        );

        modelBuilder.Entity<IncidentType>().HasData(
            new IncidentType { Id = 1, Name = "Theft" },
            new IncidentType { Id = 2, Name = "Assault" },
            new IncidentType { Id = 3, Name = "Vandalism" },
            new IncidentType { Id = 4, Name = "Burglary" },
            new IncidentType { Id = 5, Name = "Traffic Incident" },
            new IncidentType { Id = 6, Name = "Disturbance" },
            new IncidentType { Id = 7, Name = "Fraud" }
        );
    }
}
