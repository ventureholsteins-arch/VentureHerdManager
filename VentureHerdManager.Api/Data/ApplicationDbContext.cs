using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Animal> Animals { get; set; }

    public DbSet<HeatEvent> HeatEvents { get; set; }

    public DbSet<BreedingEvent> BreedingEvents { get; set; }

    public DbSet<CalvingEvent> CalvingEvents { get; set; }

    public DbSet<DryOffEvent> DryOffEvents { get; set; }

    public DbSet<AnimalNote> AnimalNotes { get; set; }
}