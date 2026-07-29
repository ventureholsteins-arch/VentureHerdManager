using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.DTOs;
using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.Services;

public class HeatService
{
    private readonly ApplicationDbContext _context;

    public HeatService(ApplicationDbContext context)
    {
        _context = context;
    }

    public HeatEvent RecordHeat(int animalId, RecordHeatRequest request)
    {
        var heatEvent = new HeatEvent
        {
            AnimalId = animalId,
            HeatDateTime = request.HeatDateTime,
            Notes = request.HeatNotes,
            PictureUrl = request.PictureUrl,
            CreatedBy = request.CreatedBy
        };

        _context.HeatEvents.Add(heatEvent);

        if (request.WasBred)
        {
            var breedingDate = request.HeatDateTime;
            var breedingType = request.BreedingType ?? BreedingType.AI;

            var breedingEvent = new BreedingEvent
            {
                AnimalId = animalId,
                BreedingDate = breedingDate,
                SireUsed = request.SireUsed ?? "",
                BreedingType = breedingType,
                PregnancyCheckDueDate = breedingDate.AddDays(30),
                ExpectedDueDate = breedingType == BreedingType.EmbryoTransfer
                    ? breedingDate.AddDays(287)
                    : breedingDate.AddDays(280),
                PregnancyStatus = PregnancyStatus.Unconfirmed,
                Notes = request.BreedingNotes,
                CreatedBy = request.CreatedBy
            };

            _context.BreedingEvents.Add(breedingEvent);
        }

        _context.SaveChanges();

        return heatEvent;
    }

    public List<HeatEvent> GetHeatsForAnimal(int animalId)
    {
        return _context.HeatEvents
            .Where(h => h.AnimalId == animalId)
            .OrderByDescending(h => h.HeatDateTime)
            .ToList();
    }
}