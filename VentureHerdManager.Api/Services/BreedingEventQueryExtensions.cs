using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.Services;

public static class BreedingEventQueryExtensions
{
    /// <summary>
    /// Returns the latest breeding for each animal, provided it has not
    /// already been completed by a later calving. Historical breedings stay
    /// in the database, but must not create current pregnancy alerts.
    /// </summary>
    public static IQueryable<BreedingEvent> CurrentReproductiveEvents(
        this IQueryable<BreedingEvent> query,
        ApplicationDbContext context)
    {
        return query.Where(breeding =>
            !context.BreedingEvents.Any(candidate =>
                candidate.AnimalId == breeding.AnimalId
                && (
                    candidate.BreedingDate > breeding.BreedingDate
                    || (
                        candidate.BreedingDate == breeding.BreedingDate
                        && candidate.BreedingEventId
                            > breeding.BreedingEventId
                    )
                ))
            && !context.CalvingEvents.Any(calving =>
                calving.AnimalId == breeding.AnimalId
                && calving.CalvingDate >= breeding.BreedingDate));
    }
}
