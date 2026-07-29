using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.Services;

public static class ReproductiveEventRules
{
    public const int StandardGestationDays = 280;
    public const int EmbryoTransferGestationDays = 273;
    public const int PregnancyCheckAfterBreedingDays = 30;
    public const int PregnancyCheckAfterTransferDays = 28;
    public const int DryPeriodDays = 60;
    public const int CloseUpDays = 21;

    public static void ApplyPregnancyStatus(
        BreedingEvent breeding,
        PregnancyStatus status,
        bool isEmbryoTransfer,
        DateTime? checkedAt)
    {
        breeding.PregnancyStatus = status;
        if (checkedAt.HasValue)
        {
            breeding.PregnancyCheckDate = checkedAt.Value;
        }

        if (status is not (
                PregnancyStatus.Open or PregnancyStatus.Aborted))
        {
            var dueDate = breeding.BreedingDate.AddDays(
                isEmbryoTransfer
                    ? EmbryoTransferGestationDays
                    : StandardGestationDays);
            breeding.ExpectedDueDate = dueDate;
            breeding.RecommendedDryOffDate =
                dueDate.AddDays(-DryPeriodDays);
            breeding.CloseUpDate = dueDate.AddDays(-CloseUpDays);
        }
        else
        {
            breeding.ExpectedDueDate = null;
            breeding.RecommendedDryOffDate = null;
            breeding.CloseUpDate = null;
        }

        breeding.UpdatedAt = DateTime.UtcNow;
    }

    public static void SynchronizeEmbryoOutcome(
        EmbryoRecord embryo,
        PregnancyStatus status,
        string? failureNotes = null)
    {
        embryo.Status = status switch
        {
            PregnancyStatus.Pregnant => EmbryoStatus.Successful,
            PregnancyStatus.Open or PregnancyStatus.Aborted =>
                EmbryoStatus.Failed,
            _ => EmbryoStatus.Implanted
        };

        if (status is (
                PregnancyStatus.Pregnant
                or PregnancyStatus.Unconfirmed
                or PregnancyStatus.Recheck))
        {
            embryo.FailureNotes = null;
        }
        else if (status is PregnancyStatus.Open or PregnancyStatus.Aborted)
        {
            embryo.FailureNotes = string.IsNullOrWhiteSpace(failureNotes)
                ? status == PregnancyStatus.Aborted
                    ? $"Pregnancy loss recorded on {DateTime.UtcNow:d}."
                    : $"Pregnancy check on {DateTime.UtcNow:d}: embryo did not establish a pregnancy."
                : failureNotes.Trim();
        }

        embryo.UpdatedAt = DateTime.UtcNow;
    }

    public static void CompleteByCalving(
        BreedingEvent breeding,
        DateTime calvingDate)
    {
        breeding.PregnancyStatus = PregnancyStatus.Pregnant;
        breeding.ExpectedDueDate = null;
        breeding.PregnancyCheckDueDate = null;
        breeding.RecommendedDryOffDate = null;
        breeding.CloseUpDate = null;
        breeding.Notes = AppendNote(
            breeding.Notes,
            $"Pregnancy completed by calving on {calvingDate:d}.");
        breeding.UpdatedBy = "Calving workflow";
        breeding.UpdatedAt = DateTime.UtcNow;
    }

    public static string AppendNote(string? notes, string note)
    {
        if (notes?.Contains(note, StringComparison.OrdinalIgnoreCase) == true)
        {
            return notes;
        }

        return string.IsNullOrWhiteSpace(notes)
            ? note
            : $"{notes.Trim()} {note}";
    }
}
