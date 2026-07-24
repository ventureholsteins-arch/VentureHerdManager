using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Animal> Animals => Set<Animal>();

    public DbSet<HeatEvent> HeatEvents => Set<HeatEvent>();

    public DbSet<BreedingEvent> BreedingEvents => Set<BreedingEvent>();

    public DbSet<CalvingEvent> CalvingEvents => Set<CalvingEvent>();

    public DbSet<DryOffEvent> DryOffEvents => Set<DryOffEvent>();

    public DbSet<AnimalNote> AnimalNotes => Set<AnimalNote>();

    public DbSet<ClassificationRecord> ClassificationRecords =>
        Set<ClassificationRecord>();

    public DbSet<LutalyseEvent> LutalyseEvents => Set<LutalyseEvent>();

    public DbSet<AnimalPhoto> AnimalPhotos => Set<AnimalPhoto>();

    public DbSet<AppearanceSetting> AppearanceSettings =>
        Set<AppearanceSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureAnimal(modelBuilder);
        ConfigureHeatEvent(modelBuilder);
        ConfigureBreedingEvent(modelBuilder);
        ConfigureCalvingEvent(modelBuilder);
        ConfigureDryOffEvent(modelBuilder);
        ConfigureAnimalNote(modelBuilder);
        ConfigureClassificationRecord(modelBuilder);
        ConfigureLutalyseEvent(modelBuilder);
        ConfigureAnimalPhoto(modelBuilder);
        ConfigureAppearanceSetting(modelBuilder);
    }

    private static void ConfigureAnimal(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Animal>();

        entity.HasKey(animal => animal.AnimalId);

        entity.HasOne(animal => animal.Dam)
            .WithMany(dam => dam.OffspringAsDam)
            .HasForeignKey(animal => animal.DamId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(animal => animal.Sire)
            .WithMany(sire => sire.OffspringAsSire)
            .HasForeignKey(animal => animal.SireId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(animal => animal.BarnName);

        entity.HasIndex(animal => animal.RegisteredName);

        entity.HasIndex(animal => animal.AnimalStatus);

        entity.HasIndex(animal => animal.AnimalStage);

        entity.HasIndex(animal => animal.IsFavorite);

        entity.HasIndex(animal => animal.DamId);

        entity.HasIndex(animal => animal.SireId);

        entity.HasIndex(animal => animal.RegistrationNumber)
            .IsUnique()
            .HasFilter("[RegistrationNumber] IS NOT NULL");

        entity.Property(animal => animal.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        entity.Property(animal => animal.UpdatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");
    }

    private static void ConfigureHeatEvent(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<HeatEvent>();

        entity.HasKey(heat => heat.HeatEventId);

        entity.HasOne(heat => heat.Animal)
            .WithMany(animal => animal.HeatEvents)
            .HasForeignKey(heat => heat.AnimalId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(heat => heat.AnimalId);

        entity.HasIndex(heat => heat.HeatDateTime);

        entity.HasIndex(heat => new
        {
            heat.AnimalId,
            heat.HeatDateTime
        });

        entity.HasIndex(heat => heat.ExpectedNextHeatStart);

        entity.HasIndex(heat => heat.ExpectedNextHeatEnd);

        entity.HasIndex(heat => heat.HasEmbryoTransfer);

        entity.HasIndex(heat => heat.EmbryoImplantDate);

        entity.Property(heat => heat.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        entity.Property(heat => heat.UpdatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");
    }

    private static void ConfigureBreedingEvent(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<BreedingEvent>();

        entity.HasKey(breeding => breeding.BreedingEventId);

        entity.HasOne(breeding => breeding.Animal)
            .WithMany(animal => animal.BreedingEvents)
            .HasForeignKey(breeding => breeding.AnimalId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(breeding => breeding.AnimalId);

        entity.HasIndex(breeding => breeding.BreedingDate);

        entity.HasIndex(breeding => new
        {
            breeding.AnimalId,
            breeding.BreedingDate
        });

        entity.HasIndex(breeding => breeding.ExpectedDueDate);

        entity.HasIndex(breeding => breeding.PregnancyCheckDueDate);

        entity.HasIndex(breeding => breeding.RecommendedDryOffDate);

        entity.HasIndex(breeding => breeding.CloseUpDate);

        entity.HasIndex(breeding => breeding.PregnancyStatus);

        entity.Property(breeding => breeding.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        entity.Property(breeding => breeding.UpdatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");
    }

    private static void ConfigureCalvingEvent(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<CalvingEvent>();

        entity.HasKey(calving => calving.CalvingEventId);

        entity.HasOne(calving => calving.Animal)
            .WithMany(animal => animal.CalvingEvents)
            .HasForeignKey(calving => calving.AnimalId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(calving => calving.CalfAnimal)
            .WithMany()
            .HasForeignKey(calving => calving.CalfAnimalId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(calving => calving.AnimalId);

        entity.HasIndex(calving => calving.CalfAnimalId);

        entity.HasIndex(calving => calving.CalvingDate);

        entity.HasIndex(calving => new
        {
            calving.AnimalId,
            calving.CalvingDate
        });

        entity.Property(calving => calving.BirthWeight)
            .HasPrecision(6, 2);

        entity.Property(calving => calving.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        entity.Property(calving => calving.UpdatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");
    }

    private static void ConfigureDryOffEvent(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<DryOffEvent>();

        entity.HasKey(dryOff => dryOff.DryOffEventId);

        entity.HasOne(dryOff => dryOff.Animal)
            .WithMany(animal => animal.DryOffEvents)
            .HasForeignKey(dryOff => dryOff.AnimalId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(dryOff => dryOff.AnimalId);

        entity.HasIndex(dryOff => dryOff.DryOffDate);

        entity.HasIndex(dryOff => new
        {
            dryOff.AnimalId,
            dryOff.DryOffDate
        });

        entity.Property(dryOff => dryOff.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        entity.Property(dryOff => dryOff.UpdatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");
    }

    private static void ConfigureAnimalNote(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AnimalNote>();

        entity.HasKey(note => note.AnimalNoteId);

        entity.HasOne(note => note.Animal)
            .WithMany(animal => animal.AnimalNotes)
            .HasForeignKey(note => note.AnimalId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(note => note.AnimalId);

        entity.HasIndex(note => note.NoteDate);

        entity.HasIndex(note => new
        {
            note.AnimalId,
            note.NoteDate
        });

        entity.Property(note => note.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        entity.Property(note => note.UpdatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");
    }

    private static void ConfigureClassificationRecord(
        ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ClassificationRecord>();

        entity.HasKey(record => record.ClassificationRecordId);

        entity.HasOne(record => record.Animal)
            .WithMany(animal => animal.ClassificationRecords)
            .HasForeignKey(record => record.AnimalId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(record => record.AnimalId);

        entity.HasIndex(record => record.ClassificationDate);

        entity.HasIndex(record => new
        {
            record.AnimalId,
            record.ClassificationDate
        });

        entity.HasIndex(record => record.Baa);

        entity.Property(record => record.Score)
            .HasPrecision(5, 2);

        entity.Property(record => record.Baa)
            .HasPrecision(7, 2);

        entity.Property(record => record.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        entity.Property(record => record.UpdatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");
    }

    private static void ConfigureLutalyseEvent(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<LutalyseEvent>();

        entity.HasKey(lutalyse => lutalyse.LutalyseEventId);

        entity.HasOne(lutalyse => lutalyse.Animal)
            .WithMany(animal => animal.LutalyseEvents)
            .HasForeignKey(lutalyse => lutalyse.AnimalId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(lutalyse => lutalyse.AnimalId);

        entity.HasIndex(lutalyse => lutalyse.AdministrationDate);

        entity.HasIndex(lutalyse => lutalyse.ExpectedHeatWatchStart);

        entity.HasIndex(lutalyse => lutalyse.ExpectedHeatWatchEnd);

        entity.HasIndex(lutalyse => new
        {
            lutalyse.AnimalId,
            lutalyse.AdministrationDate
        });

        entity.Property(lutalyse => lutalyse.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        entity.Property(lutalyse => lutalyse.UpdatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");
    }

    private static void ConfigureAnimalPhoto(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AnimalPhoto>();

        entity.HasKey(photo => photo.AnimalPhotoId);

        entity.HasOne(photo => photo.Animal)
            .WithMany(animal => animal.Photos)
            .HasForeignKey(photo => photo.AnimalId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(photo => photo.AnimalId);

        entity.HasIndex(photo => photo.PhotoDate);

        entity.HasIndex(photo => photo.PhotoType);

        entity.HasIndex(photo => new
        {
            photo.AnimalId,
            photo.PhotoDate
        });

        entity.Property(photo => photo.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        entity.Property(photo => photo.UpdatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");
    }

    private static void ConfigureAppearanceSetting(
        ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AppearanceSetting>();

        entity.HasKey(setting => setting.AppearanceSettingId);

        entity.Property(setting => setting.BackgroundOpacity)
            .HasPrecision(4, 2);

        entity.Property(setting => setting.OverlayOpacity)
            .HasPrecision(4, 2);

        entity.Property(setting => setting.UpdatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");
    }
}