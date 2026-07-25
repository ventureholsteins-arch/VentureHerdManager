using System.ComponentModel.DataAnnotations;

namespace VentureHerdManager.Api.Models;

public class EmbryoRecord
{
    public int EmbryoRecordId { get; set; }

    /// <summary>
    /// User-assigned code or label (e.g. ET-2026-001, tank 3 straw 5)
    /// </summary>
    [MaxLength(200)]
    public string? Code { get; set; }

    [MaxLength(200)]
    public string? Sire { get; set; }

    /// <summary>
    /// Donor cow name or ID
    /// </summary>
    [MaxLength(200)]
    public string? Donor { get; set; }

    /// <summary>
    /// Grade assigned at time of collection (Grade 1, Excellent, etc.)
    /// </summary>
    [MaxLength(100)]
    public string? Grade { get; set; }

    public EmbryoStatus Status { get; set; } = EmbryoStatus.InStorage;

    /// <summary>
    /// Animal this embryo is assigned or implanted into
    /// </summary>
    public int? RecipientAnimalId { get; set; }

    public Animal? RecipientAnimal { get; set; }

    /// <summary>
    /// Date the embryo was physically implanted
    /// </summary>
    public DateOnly? ImplantDate { get; set; }

    /// <summary>
    /// Free-text link to a breeding event note or date for traceability
    /// </summary>
    [MaxLength(500)]
    public string? LinkedBreedingNote { get; set; }

    /// <summary>
    /// Recorded when status is Failed — reason, vet notes, recheck details
    /// </summary>
    [MaxLength(2000)]
    public string? FailureNotes { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [MaxLength(200)]
    public string? CollectionLocation { get; set; }

    [MaxLength(200)]
    public string? StorageLocation { get; set; }

    [MaxLength(200)]
    public string? CreatedBy { get; set; }

    [MaxLength(200)]
    public string? UpdatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum EmbryoStatus
{
    InStorage = 0,
    Assigned = 1,
    Implanted = 2,
    Failed = 3,
    Successful = 4
}
