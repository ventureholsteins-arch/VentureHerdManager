using System.ComponentModel.DataAnnotations;

namespace VentureHerdManager.Api.DTOs;

public sealed class PcdartImportRequest
{
    [Required]
    public string RawText { get; set; } = string.Empty;

    public string? ReportLabel { get; set; }

    public bool ApplySuggestedChanges { get; set; }
}

public sealed class PcdartImportResult
{
    public bool Applied { get; set; }

    public string ReportLabel { get; set; } = string.Empty;

    public int RowsRead { get; set; }

    public int AnimalsMatched { get; set; }

    public int AnimalsCreated { get; set; }

    public int NotesCreated { get; set; }

    public int DuplicateNotesSkipped { get; set; }

    public int SuggestedChangesApplied { get; set; }

    public List<string> MissingAnimals { get; } = [];

    public List<string> Conflicts { get; } = [];

    public List<PcdartAuditAlert> Alerts { get; } = [];

    public List<PcdartSuggestedChange> SuggestedChanges { get; } = [];
}

public sealed class PcdartAuditAlert
{
    public string Severity { get; set; } = "info";

    public string Code { get; set; } = string.Empty;

    public int? AnimalId { get; set; }

    public string AnimalLabel { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}

public sealed class PcdartSuggestedChange
{
    public string Code { get; set; } = string.Empty;

    public int? AnimalId { get; set; }

    public string AnimalLabel { get; set; } = string.Empty;

    public string ProposedAction { get; set; } = string.Empty;

    public bool CanAutoApply { get; set; }
}