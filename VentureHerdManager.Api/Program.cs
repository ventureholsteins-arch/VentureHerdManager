using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;
using VentureHerdManager.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Database
var connectionString = builder.Configuration["ConnectionStrings__DefaultConnection"]
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["ConnectionStrings:DefaultConnection"];

if (string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = "Server=localhost;Database=VentureHerdManager;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Services
builder.Services.AddScoped<AnimalService>();
builder.Services.AddScoped<AnimalSnapshotService>();
builder.Services.AddScoped<HeatService>();
builder.Services.AddScoped<ClassificationService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<CalendarService>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
var allowedOrigins =
    builder.Configuration
        .GetSection("AllowedOrigins")
        .Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("Frontend");

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

await InitializeDatabaseAsync(app);

app.Run();

static async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();

    static async Task EnsureAnimalColumnAsync(
        ApplicationDbContext context,
        string columnName,
        string columnDefinition)
    {
        await context.Database.ExecuteSqlRawAsync(
            "IF OBJECT_ID(N'dbo.Animals', N'U') IS NOT NULL " +
            "AND COL_LENGTH(N'dbo.Animals', N'" + columnName + "') IS NULL " +
            "BEGIN " +
            "ALTER TABLE [Animals] ADD [" + columnName + "] " + columnDefinition + "; " +
            "END");
    }

    try
    {
        await context.Database.ExecuteSqlRawAsync(
            "IF OBJECT_ID(N'CalvingEvents', N'U') IS NOT NULL " +
            "AND EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.CalvingEvents') AND name = N'CK_CalvingEvents_NumberOfCalves') " +
            "BEGIN " +
            "ALTER TABLE [CalvingEvents] DROP CONSTRAINT [CK_CalvingEvents_NumberOfCalves]; " +
            "END");

        await context.Database.ExecuteSqlRawAsync(
            "IF OBJECT_ID(N'CalvingEvents', N'U') IS NOT NULL " +
            "BEGIN " +
            "IF COL_LENGTH(N'dbo.CalvingEvents', N'NumberOfCalves') IS NULL " +
            "BEGIN " +
            "ALTER TABLE [CalvingEvents] ADD [NumberOfCalves] INT NOT NULL CONSTRAINT [DF_CalvingEvents_NumberOfCalves] DEFAULT 1; " +
            "END " +
            "END");

        await context.Database.ExecuteSqlRawAsync(
            "IF OBJECT_ID(N'CalvingEvents', N'U') IS NOT NULL " +
            "AND COL_LENGTH(N'dbo.CalvingEvents', N'NumberOfCalves') IS NOT NULL " +
            "BEGIN " +
            "UPDATE [CalvingEvents] SET [NumberOfCalves] = 1 WHERE [NumberOfCalves] < 1; " +
            "END");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Calving data repair warning: {ex.Message}");
    }

    try
    {
        var animalColumns = new[]
        {
            ("CreatedAt", "DATETIME2 NOT NULL CONSTRAINT [DF_Animals_CreatedAt] DEFAULT SYSUTCDATETIME()"),
            ("CreatedBy", "NVARCHAR(200) NULL"),
            ("CurrentLactation", "INT NULL"),
            ("DeceasedDate", "DATETIME2 NULL"),
            ("DeceasedNotes", "NVARCHAR(2000) NULL"),
            ("IsFavorite", "BIT NOT NULL CONSTRAINT [DF_Animals_IsFavorite] DEFAULT 0"),
            ("ProfilePictureUrl", "NVARCHAR(1000) NULL"),
            ("SoldDate", "DATETIME2 NULL"),
            ("SoldNotes", "NVARCHAR(2000) NULL"),
            ("UpdatedAt", "DATETIME2 NOT NULL CONSTRAINT [DF_Animals_UpdatedAt] DEFAULT SYSUTCDATETIME()"),
            ("UpdatedBy", "NVARCHAR(200) NULL")
        };

        foreach (var (columnName, columnDefinition) in animalColumns)
        {
            try
            {
                await EnsureAnimalColumnAsync(context, columnName, columnDefinition);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Animal schema repair warning for {columnName}: {ex.Message}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Animal schema repair warning: {ex.Message}");
    }

    try
    {
        var heatEventColumns = new[]
        {
            ("HasEmbryoTransfer", "BIT NOT NULL CONSTRAINT [DF_HeatEvents_HasEmbryoTransfer] DEFAULT 0"),
            ("EmbryoImplantDate", "DATETIME2 NULL")
        };

        foreach (var (columnName, columnDefinition) in heatEventColumns)
        {
            try
            {
                await context.Database.ExecuteSqlRawAsync(
                    "IF OBJECT_ID(N'dbo.HeatEvents', N'U') IS NOT NULL " +
                    "AND COL_LENGTH(N'dbo.HeatEvents', N'" + columnName + "') IS NULL " +
                    "BEGIN " +
                    "ALTER TABLE [HeatEvents] ADD [" + columnName + "] " + columnDefinition + "; " +
                    "END");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HeatEvent schema repair warning for {columnName}: {ex.Message}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"HeatEvent schema repair warning: {ex.Message}");
    }

    try
    {
        await context.Database.MigrateAsync();
        Console.WriteLine("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database migration warning: {ex.Message}");
    }

    try
    {
        // Ensure LutalyseEvents table exists and has all required columns
        await context.Database.ExecuteSqlRawAsync(
            @"IF OBJECT_ID(N'dbo.LutalyseEvents', N'U') IS NOT NULL
              BEGIN
                IF COL_LENGTH(N'dbo.LutalyseEvents', N'UpdatedBy') IS NULL
                  ALTER TABLE [LutalyseEvents] ADD [UpdatedBy] NVARCHAR(200) NULL;
              END");

        Console.WriteLine("LutalyseEvents table columns ensured.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"LutalyseEvents table warning: {ex.Message}");
    }

    try
    {
        // Ensure LutalyseEvents table exists (create if not exists)
        await context.Database.ExecuteSqlRawAsync(
            @"IF OBJECT_ID(N'dbo.LutalyseEvents', N'U') IS NULL
              BEGIN
                CREATE TABLE [LutalyseEvents] (
                    [LutalyseEventId] INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
                    [AnimalId] INT NOT NULL,
                    [AdministrationDate] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    [ExpectedHeatWatchStart] DATETIME2 NOT NULL,
                    [ExpectedHeatWatchEnd] DATETIME2 NOT NULL,
                    [HeatObserved] BIT NOT NULL DEFAULT 0,
                    [HeatObservedDate] DATETIME2 NULL,
                    [ResultingHeatEventId] INT NULL,
                    [Notes] NVARCHAR(2000) NULL,
                    [CreatedBy] NVARCHAR(200) NULL,
                    [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    [UpdatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    [UpdatedBy] NVARCHAR(200) NULL,
                    CONSTRAINT [FK_LutalyseEvents_Animals] FOREIGN KEY ([AnimalId]) 
                        REFERENCES [Animals]([AnimalId]) ON DELETE CASCADE
                );
                
                CREATE INDEX [IX_LutalyseEvents_AnimalId] ON [LutalyseEvents]([AnimalId]);
                CREATE INDEX [IX_LutalyseEvents_AdministrationDate] ON [LutalyseEvents]([AdministrationDate]);
              END");

        Console.WriteLine("LutalyseEvents table created if needed.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"LutalyseEvents table creation warning: {ex.Message}");
    }

    try
    {
        // Ensure HeatEvents table has all required columns
        await context.Database.ExecuteSqlRawAsync(
            @"IF OBJECT_ID(N'dbo.HeatEvents', N'U') IS NOT NULL
              BEGIN
                IF COL_LENGTH(N'dbo.HeatEvents', N'HeatStrength') IS NULL
                  ALTER TABLE [HeatEvents] ADD [HeatStrength] INT NOT NULL CONSTRAINT [DF_HeatEvents_HeatStrength] DEFAULT 0;
                
                IF COL_LENGTH(N'dbo.HeatEvents', N'StandingHeat') IS NULL
                  ALTER TABLE [HeatEvents] ADD [StandingHeat] BIT NOT NULL CONSTRAINT [DF_HeatEvents_StandingHeat] DEFAULT 0;
                
                IF COL_LENGTH(N'dbo.HeatEvents', N'ExpectedNextHeatStart') IS NULL
                  ALTER TABLE [HeatEvents] ADD [ExpectedNextHeatStart] DATETIME2 NULL;
                
                IF COL_LENGTH(N'dbo.HeatEvents', N'ExpectedNextHeatEnd') IS NULL
                  ALTER TABLE [HeatEvents] ADD [ExpectedNextHeatEnd] DATETIME2 NULL;
                
                IF COL_LENGTH(N'dbo.HeatEvents', N'CreatedAt') IS NULL
                  ALTER TABLE [HeatEvents] ADD [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_HeatEvents_CreatedAt] DEFAULT SYSUTCDATETIME();
                
                IF COL_LENGTH(N'dbo.HeatEvents', N'UpdatedAt') IS NULL
                  ALTER TABLE [HeatEvents] ADD [UpdatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_HeatEvents_UpdatedAt] DEFAULT SYSUTCDATETIME();
                
                IF COL_LENGTH(N'dbo.HeatEvents', N'UpdatedBy') IS NULL
                  ALTER TABLE [HeatEvents] ADD [UpdatedBy] NVARCHAR(200) NULL;
              END");

        Console.WriteLine("HeatEvents table columns ensured.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"HeatEvents table warning: {ex.Message}");
    }

    try
    {
        // Ensure BreedingEvents table has all required columns
        await context.Database.ExecuteSqlRawAsync(
            @"IF OBJECT_ID(N'dbo.BreedingEvents', N'U') IS NOT NULL
              BEGIN
                IF COL_LENGTH(N'dbo.BreedingEvents', N'CreatedAt') IS NULL
                  ALTER TABLE [BreedingEvents] ADD [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_BreedingEvents_CreatedAt] DEFAULT SYSUTCDATETIME();
                
                IF COL_LENGTH(N'dbo.BreedingEvents', N'UpdatedAt') IS NULL
                  ALTER TABLE [BreedingEvents] ADD [UpdatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_BreedingEvents_UpdatedAt] DEFAULT SYSUTCDATETIME();
                
                IF COL_LENGTH(N'dbo.BreedingEvents', N'UpdatedBy') IS NULL
                  ALTER TABLE [BreedingEvents] ADD [UpdatedBy] NVARCHAR(200) NULL;
                
                IF COL_LENGTH(N'dbo.BreedingEvents', N'CloseUpDate') IS NULL
                  ALTER TABLE [BreedingEvents] ADD [CloseUpDate] DATETIME2 NULL;
                
                IF COL_LENGTH(N'dbo.BreedingEvents', N'RecommendedDryOffDate') IS NULL
                  ALTER TABLE [BreedingEvents] ADD [RecommendedDryOffDate] DATETIME2 NULL;
                
                IF COL_LENGTH(N'dbo.BreedingEvents', N'PregnancyCheckDate') IS NULL
                  ALTER TABLE [BreedingEvents] ADD [PregnancyCheckDate] DATETIME2 NULL;
                
                IF COL_LENGTH(N'dbo.BreedingEvents', N'Technician') IS NULL
                  ALTER TABLE [BreedingEvents] ADD [Technician] NVARCHAR(200) NULL;
              END");

        Console.WriteLine("BreedingEvents table columns ensured.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"BreedingEvents table warning: {ex.Message}");
    }

    try
    {
        // Ensure CalvingEvents table has all required columns
        await context.Database.ExecuteSqlRawAsync(
            @"IF OBJECT_ID(N'dbo.CalvingEvents', N'U') IS NOT NULL
              BEGIN
                IF COL_LENGTH(N'dbo.CalvingEvents', N'CreatedAt') IS NULL
                  ALTER TABLE [CalvingEvents] ADD [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_CalvingEvents_CreatedAt] DEFAULT SYSUTCDATETIME();
                
                IF COL_LENGTH(N'dbo.CalvingEvents', N'UpdatedAt') IS NULL
                  ALTER TABLE [CalvingEvents] ADD [UpdatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_CalvingEvents_UpdatedAt] DEFAULT SYSUTCDATETIME();
                
                IF COL_LENGTH(N'dbo.CalvingEvents', N'UpdatedBy') IS NULL
                  ALTER TABLE [CalvingEvents] ADD [UpdatedBy] NVARCHAR(200) NULL;
                
                IF COL_LENGTH(N'dbo.CalvingEvents', N'CalfRegistrationNumber') IS NULL
                  ALTER TABLE [CalvingEvents] ADD [CalfRegistrationNumber] NVARCHAR(100) NULL;
                
                IF COL_LENGTH(N'dbo.CalvingEvents', N'CalfAnimalId') IS NULL
                  ALTER TABLE [CalvingEvents] ADD [CalfAnimalId] INT NULL;
                
                IF COL_LENGTH(N'dbo.CalvingEvents', N'BirthWeight') IS NULL
                  ALTER TABLE [CalvingEvents] ADD [BirthWeight] DECIMAL(10, 2) NULL;
                
                IF COL_LENGTH(N'dbo.CalvingEvents', N'PictureUrl') IS NULL
                  ALTER TABLE [CalvingEvents] ADD [PictureUrl] NVARCHAR(1000) NULL;
              END");

        Console.WriteLine("CalvingEvents table columns ensured.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"CalvingEvents table warning: {ex.Message}");
    }

    try
    {
        // Ensure DryOffEvents table has all required columns
        await context.Database.ExecuteSqlRawAsync(
            @"IF OBJECT_ID(N'dbo.DryOffEvents', N'U') IS NOT NULL
              BEGIN
                IF COL_LENGTH(N'dbo.DryOffEvents', N'CreatedAt') IS NULL
                  ALTER TABLE [DryOffEvents] ADD [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_DryOffEvents_CreatedAt] DEFAULT SYSUTCDATETIME();
                
                IF COL_LENGTH(N'dbo.DryOffEvents', N'UpdatedAt') IS NULL
                  ALTER TABLE [DryOffEvents] ADD [UpdatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_DryOffEvents_UpdatedAt] DEFAULT SYSUTCDATETIME();
                
                IF COL_LENGTH(N'dbo.DryOffEvents', N'UpdatedBy') IS NULL
                  ALTER TABLE [DryOffEvents] ADD [UpdatedBy] NVARCHAR(200) NULL;
              END");

        Console.WriteLine("DryOffEvents table columns ensured.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"DryOffEvents table warning: {ex.Message}");
    }

    try
    {
        // Ensure ClassificationRecords table exists
        await context.Database.ExecuteSqlRawAsync(
            @"IF OBJECT_ID(N'dbo.ClassificationRecords', N'U') IS NULL
              BEGIN
                CREATE TABLE [ClassificationRecords] (
                    [ClassificationRecordId] INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
                    [AnimalId] INT NOT NULL,
                    [ClassificationDate] DATETIME2 NOT NULL,
                    [Score] DECIMAL(5, 2) NOT NULL,
                    [Baa] DECIMAL(7, 2) NULL,
                    [AgeInMonthsAtScoring] INT NOT NULL,
                    [ClassificationLabel] NVARCHAR(50) NULL,
                    [Notes] NVARCHAR(2000) NULL,
                    [CreatedBy] NVARCHAR(200) NULL,
                    [UpdatedBy] NVARCHAR(200) NULL,
                    [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    [UpdatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT [FK_ClassificationRecords_Animals] FOREIGN KEY ([AnimalId]) 
                        REFERENCES [Animals]([AnimalId]) ON DELETE CASCADE,
                    CONSTRAINT [CK_ClassificationRecords_AgeInMonths] CHECK ([AgeInMonthsAtScoring] >= 0),
                    CONSTRAINT [CK_ClassificationRecords_Score] CHECK ([Score] >= 0)
                );
                
                CREATE INDEX [IX_ClassificationRecords_AnimalId] ON [ClassificationRecords]([AnimalId]);
              END");

        Console.WriteLine("ClassificationRecords table ensured.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ClassificationRecords table warning: {ex.Message}");
    }

    try
    {
        // Ensure AnimalNotes table has all required columns
        await context.Database.ExecuteSqlRawAsync(
            @"IF OBJECT_ID(N'dbo.AnimalNotes', N'U') IS NOT NULL
              BEGIN
                IF COL_LENGTH(N'dbo.AnimalNotes', N'CreatedAt') IS NULL
                  ALTER TABLE [AnimalNotes] ADD [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_AnimalNotes_CreatedAt] DEFAULT SYSUTCDATETIME();
                
                IF COL_LENGTH(N'dbo.AnimalNotes', N'UpdatedAt') IS NULL
                  ALTER TABLE [AnimalNotes] ADD [UpdatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_AnimalNotes_UpdatedAt] DEFAULT SYSUTCDATETIME();
                
                IF COL_LENGTH(N'dbo.AnimalNotes', N'UpdatedBy') IS NULL
                  ALTER TABLE [AnimalNotes] ADD [UpdatedBy] NVARCHAR(200) NULL;
              END");

        Console.WriteLine("AnimalNotes table columns ensured.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"AnimalNotes table warning: {ex.Message}");
    }

    try
    {
        await context.Database.EnsureCreatedAsync();
        Console.WriteLine("Database schema ensured for local development.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database initialization warning: {ex.Message}");
    }

    try
    {
        if (!await context.Animals.AnyAsync())
        {
            var now = DateTime.UtcNow;

            var animals = new List<Animal>
            {
            new()
            {
                BarnName = "Mabel",
                RegisteredName = "Venture Mabel 01",
                RegistrationNumber = "VH-001",
                BirthDate = new DateOnly(2020, 3, 14),
                Sex = AnimalSex.Female,
                AnimalStage = AnimalStage.Milking,
                CurrentLactation = 3,
                AnimalStatus = AnimalStatus.Active,
                Breed = "Holstein",
                IsFavorite = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                BarnName = "Daisy",
                RegisteredName = "Venture Daisy 02",
                RegistrationNumber = "VH-002",
                BirthDate = new DateOnly(2021, 5, 8),
                Sex = AnimalSex.Female,
                AnimalStage = AnimalStage.Dry,
                CurrentLactation = 2,
                AnimalStatus = AnimalStatus.Active,
                Breed = "Holstein",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                BarnName = "Juniper",
                RegisteredName = "Venture Juniper 03",
                RegistrationNumber = "VH-003",
                BirthDate = new DateOnly(2023, 7, 1),
                Sex = AnimalSex.Female,
                AnimalStage = AnimalStage.Heifer,
                AnimalStatus = AnimalStatus.Active,
                Breed = "Jersey",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                BarnName = "Pip",
                RegisteredName = "Venture Pip 04",
                RegistrationNumber = "VH-004",
                BirthDate = new DateOnly(2024, 2, 10),
                Sex = AnimalSex.Male,
                AnimalStage = AnimalStage.Bull,
                AnimalStatus = AnimalStatus.Active,
                Breed = "Holstein",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                BarnName = "Rory",
                RegisteredName = "Venture Rory 05",
                RegistrationNumber = "VH-005",
                BirthDate = new DateOnly(2024, 8, 20),
                Sex = AnimalSex.Female,
                AnimalStage = AnimalStage.Calf,
                AnimalStatus = AnimalStatus.Active,
                Breed = "Holstein",
                CreatedAt = now,
                UpdatedAt = now
            }
        };

            context.Animals.AddRange(animals);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Animal initialization warning: {ex.Message}");
    }

    try
    {
        if (!await context.HeatEvents.AnyAsync())
        {
            var animals = await context.Animals.ToListAsync();

            if (animals.Count >= 2)
            {
                context.HeatEvents.AddRange(
                    new HeatEvent
                    {
                        AnimalId = animals[0].AnimalId,
                        HeatDateTime = DateTime.UtcNow.AddDays(-2),
                        Notes = "Observed standing heat this morning.",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new HeatEvent
                    {
                        AnimalId = animals[1].AnimalId,
                        HeatDateTime = DateTime.UtcNow.AddDays(-5),
                        Notes = "Heat detected after morning check.",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Heat event initialization warning: {ex.Message}");
    }

    try
    {
        if (!await context.BreedingEvents.AnyAsync())
        {
            var animals = await context.Animals.ToListAsync();

            if (animals.Count >= 2)
            {
                context.BreedingEvents.AddRange(
                    new BreedingEvent
                    {
                        AnimalId = animals[0].AnimalId,
                        BreedingDate = DateTime.UtcNow.AddDays(-20),
                        SireUsed = "Venture Bull 01",
                        BreedingType = BreedingType.Natural,
                        PregnancyStatus = PregnancyStatus.Unconfirmed,
                        PregnancyCheckDueDate = DateTime.UtcNow.AddDays(10),
                        ExpectedDueDate = DateTime.UtcNow.AddDays(280),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new BreedingEvent
                    {
                        AnimalId = animals[1].AnimalId,
                        BreedingDate = DateTime.UtcNow.AddDays(-45),
                        SireUsed = "Venture Bull 02",
                        BreedingType = BreedingType.AI,
                        PregnancyStatus = PregnancyStatus.Pregnant,
                        PregnancyCheckDueDate = DateTime.UtcNow.AddDays(-5),
                        ExpectedDueDate = DateTime.UtcNow.AddDays(20),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Breeding initialization warning: {ex.Message}");
    }

    try
    {
        await context.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database save warning: {ex.Message}");
    }
}