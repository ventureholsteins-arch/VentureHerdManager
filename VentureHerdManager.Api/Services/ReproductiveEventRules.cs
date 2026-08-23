using Microsoft.EntityFrameworkCore;
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

    public static void CompleteEmbryoByCalving(
        EmbryoRecord embryo,
        DateTime calvingDate)
    {
        embryo.Status = EmbryoStatus.Completed;
        embryo.FailureNotes = null;
        embryo.LinkedBreedingNote = AppendNote(
            embryo.LinkedBreedingNote,
            $"Successful implant completed by calving on {calvingDate:d}.");
        embryo.UpdatedBy = "Calving workflow";
        embryo.UpdatedAt = DateTime.UtcNow;
    }

    public static async Task ClosePriorServiceAsync(
        Data.ApplicationDbContext context,
        int animalId,
        DateTime newEventDate,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var prior = await context.BreedingEvents
            .Where(breeding =>
                breeding.AnimalId == animalId
                && breeding.BreedingDate < newEventDate
                && breeding.PregnancyStatus != PregnancyStatus.Open
                && breeding.PregnancyStatus != PregnancyStatus.Aborted)
            .OrderByDescending(breeding => breeding.BreedingDate)
            .ThenByDescending(breeding => breeding.BreedingEventId)
            .FirstOrDefaultAsync(cancellationToken);
        if (prior == null)
        {
            return;
        }

        ApplyPregnancyStatus(
            prior,
            PregnancyStatus.Open,
            prior.BreedingType == BreedingType.EmbryoTransfer,
            newEventDate);
        prior.PregnancyCheckDueDate = null;
        prior.Notes = AppendNote(
            prior.Notes,
            $"Closed as open when {reason} was recorded on {newEventDate:d}.");
        prior.UpdatedBy = "Reproductive workflow";

        var linkedEmbryo = await context.EmbryoRecords
            .FirstOrDefaultAsync(
                embryo => embryo.BreedingEventId == prior.BreedingEventId,
                cancellationToken);
        if (linkedEmbryo != null)
        {
            SynchronizeEmbryoOutcome(
                linkedEmbryo,
                PregnancyStatus.Open,
                $"Did not establish a pregnancy; a later {reason} was recorded on {newEventDate:d}.");
        }
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
