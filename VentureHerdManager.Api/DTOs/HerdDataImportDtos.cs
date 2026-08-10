using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.DTOs;

public sealed class HerdDataImportRequest
{
    public HerdDataSource Source { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string CsvText { get; set; } = string.Empty;
    public DateOnly ReportDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public Dictionary<string, int> AnimalMappings { get; set; } = [];
    public bool ConfirmDuplicateReplace { get; set; }
}

public sealed class HerdDataPreview
{
    public HerdDataSource Source { get; set; }
    public int RowsRead { get; set; }
    public bool DuplicateImport { get; set; }
    public bool ExactDuplicateFile { get; set; }
    public string? ExistingFileName { get; set; }
    public int? ExistingRows { get; set; }
    public DateTime? ExistingImportedAt { get; set; }
    public List<HerdDataPreviewRow> Rows { get; set; } = [];
}

public sealed class HerdDataPreviewRow
{
    public string SourceKey { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public string? OfficialId { get; set; }
    public int? AnimalId { get; set; }
    public string? AnimalName { get; set; }
    public bool NeedsConfirmation { get; set; }
    public List<HerdDataCandidate> Candidates { get; set; } = [];
}

public sealed class HerdDataCandidate
{
    public int AnimalId { get; set; }
    public string AnimalName { get; set; } = string.Empty;
    public string? RegistrationNumber { get; set; }
}
