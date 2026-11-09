using AdmissionApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AdmissionApi.Data;

public class AdmissionDbContext : DbContext
{
    public AdmissionDbContext(DbContextOptions<AdmissionDbContext> options) : base(options) { }

    public DbSet<SchoolClass> Classes => Set<SchoolClass>();
    public DbSet<Hold> Holds => Set<Hold>();
}
