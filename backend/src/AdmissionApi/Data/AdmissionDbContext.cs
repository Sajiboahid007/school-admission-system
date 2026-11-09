using AdmissionApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AdmissionApi.Data;

public class AdmissionDbContext : DbContext
{
    public AdmissionDbContext(DbContextOptions<AdmissionDbContext> options) : base(options) { }

    public DbSet<SchoolClass> Classes => Set<SchoolClass>();
    public DbSet<Hold> Holds => Set<Hold>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SchoolClass>(e =>
        {
            e.ToTable("classes");
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Hold>(e =>
        {
            e.ToTable("holds");
            e.Property(x => x.StudentId).HasMaxLength(17).IsRequired();
            e.HasOne(x => x.Class)
             .WithMany(c => c.Holds)
             .HasForeignKey(x => x.ClassId)
             .OnDelete(DeleteBehavior.Cascade);

            // Stored as "HELD", "CONFIRMED", ... so rows are readable in psql
            // and match the values the API returns.
            e.Property(x => x.Status)
             .HasConversion(
                 v => v.ToString().ToUpperInvariant(),
                 v => Enum.Parse<HoldStatus>(v, true))
             .HasMaxLength(20);

            // R3 safety net: only one HELD or CONFIRMED row per student per class.
            // EXPIRED / CANCELLED rows are not included, so the student can apply again later.
            e.HasIndex(x => new { x.ClassId, x.StudentId })
             .IsUnique()
             .HasFilter("status IN ('HELD', 'CONFIRMED')");
        });
    }
}
