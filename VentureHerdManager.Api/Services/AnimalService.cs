using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.DTOs;
using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.Services;

public class AnimalService
{
    private readonly ApplicationDbContext _context;

    public AnimalService(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Animal> GetAllAnimals()
    {
        return GetActiveAnimals();
    }

    public List<Animal> GetActiveAnimals()
    {
        var animals = _context.Animals
            .AsNoTracking()
            .Where(animal =>
                animal.AnimalStatus == AnimalStatus.Active)
            .OrderByDescending(animal => animal.IsFavorite)
            .ThenBy(animal => animal.BarnName)
            .ThenBy(animal => animal.RegisteredName)
            .ToList();

        return animals;
    }

    public List<Animal> GetArchivedAnimals()
    {
        return _context.Animals
            .AsNoTracking()
            .Where(animal =>
                animal.AnimalStatus == AnimalStatus.Sold ||
                animal.AnimalStatus == AnimalStatus.Deceased)
            .OrderBy(animal => animal.AnimalStatus)
            .ThenByDescending(animal =>
                animal.SoldDate ?? animal.DeceasedDate)
            .ThenBy(animal => animal.BarnName)
            .ThenBy(animal => animal.RegisteredName)
            .ToList();
    }

    public List<Animal> GetSoldAnimals()
    {
        return _context.Animals
            .AsNoTracking()
            .Where(animal =>
                animal.AnimalStatus == AnimalStatus.Sold)
            .OrderByDescending(animal => animal.SoldDate)
            .ThenBy(animal => animal.BarnName)
            .ThenBy(animal => animal.RegisteredName)
            .ToList();
    }

    public List<Animal> GetDeceasedAnimals()
    {
        return _context.Animals
            .AsNoTracking()
            .Where(animal =>
                animal.AnimalStatus == AnimalStatus.Deceased)
            .OrderByDescending(animal => animal.DeceasedDate)
            .ThenBy(animal => animal.BarnName)
            .ThenBy(animal => animal.RegisteredName)
            .ToList();
    }

    public Animal? GetAnimalById(int animalId)
    {
        return _context.Animals
            .AsNoTracking()
            .Include(animal => animal.Dam)
            .Include(animal => animal.Sire)
            .Include(animal => animal.HeatEvents)
            .Include(animal => animal.BreedingEvents)
            .Include(animal => animal.CalvingEvents)
            .Include(animal => animal.DryOffEvents)
            .Include(animal => animal.AnimalNotes)
            .Include(animal => animal.ClassificationRecords)
            .Include(animal => animal.LutalyseEvents)
            .Include(animal => animal.Photos)
            .AsSplitQuery()
            .FirstOrDefault(animal =>
                animal.AnimalId == animalId);
    }

    public List<Animal> SearchAnimals(string? searchText)
    {
        return SearchAnimals(
            searchText,
            includeArchived: false);
    }

    public List<Animal> SearchAnimals(
        string? searchText,
        bool includeArchived)
    {
        IQueryable<Animal> query = _context.Animals
            .AsNoTracking();

        if (!includeArchived)
        {
            query = query.Where(animal =>
                animal.AnimalStatus == AnimalStatus.Active);
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var normalizedSearch = searchText.Trim();

            query = query.Where(animal =>
                (animal.BarnName != null &&
                 animal.BarnName.Contains(normalizedSearch)) ||
                (animal.RegisteredName != null &&
                 animal.RegisteredName.Contains(normalizedSearch)) ||
                (animal.RegistrationNumber != null &&
                 animal.RegistrationNumber.Contains(normalizedSearch)) ||
                (animal.SireName != null &&
                 animal.SireName.Contains(normalizedSearch)) ||
                (animal.DamName != null &&
                 animal.DamName.Contains(normalizedSearch)) ||
                (animal.Breed != null &&
                 animal.Breed.Contains(normalizedSearch)));
        }

        return query
            .OrderByDescending(animal => animal.IsFavorite)
            .ThenBy(animal => animal.BarnName)
            .ThenBy(animal => animal.RegisteredName)
            .ToList();
    }

    public Animal CreateAnimal(Animal animal)
    {
        ValidateAnimalRelationships(
            animal.AnimalId,
            animal.SireId,
            animal.DamId);

        animal.AnimalId = 0;
        animal.BarnName =
            CleanOptionalText(animal.BarnName);
        animal.RegisteredName =
            CleanOptionalText(animal.RegisteredName);
        animal.RegistrationNumber =
            CleanOptionalText(animal.RegistrationNumber);
        animal.Breed =
            CleanOptionalText(animal.Breed);
        animal.SireName =
            CleanOptionalText(animal.SireName);
        animal.DamName =
            CleanOptionalText(animal.DamName);
        animal.Notes =
            CleanOptionalText(animal.Notes);
        animal.ProfilePictureUrl =
            CleanOptionalText(animal.ProfilePictureUrl);
        animal.SoldNotes =
            CleanOptionalText(animal.SoldNotes);
        animal.DeceasedNotes =
            CleanOptionalText(animal.DeceasedNotes);
        animal.CreatedBy =
            CleanOptionalText(animal.CreatedBy);
        animal.UpdatedBy =
            CleanOptionalText(animal.UpdatedBy);

        animal.CreatedAt = DateTime.UtcNow;
        animal.UpdatedAt = DateTime.UtcNow;

        ApplyArchiveState(animal);

        _context.Animals.Add(animal);
        _context.SaveChanges();

        return animal;
    }

    public Animal? UpdateAnimal(
        int animalId,
        Animal updatedAnimal)
    {
        var existingAnimal = _context.Animals
            .FirstOrDefault(animal =>
                animal.AnimalId == animalId);

        if (existingAnimal == null)
        {
            return null;
        }

        ValidateAnimalRelationships(
            animalId,
            updatedAnimal.SireId,
            updatedAnimal.DamId);

        existingAnimal.BarnName =
            CleanOptionalText(updatedAnimal.BarnName);

        existingAnimal.RegisteredName =
            CleanOptionalText(updatedAnimal.RegisteredName);

        existingAnimal.RegistrationNumber =
            CleanOptionalText(updatedAnimal.RegistrationNumber);

        existingAnimal.BirthDate =
            updatedAnimal.BirthDate;

        existingAnimal.Sex =
            updatedAnimal.Sex;

        existingAnimal.AnimalStage =
            updatedAnimal.AnimalStage;

        existingAnimal.CurrentLactation =
            updatedAnimal.CurrentLactation;

        existingAnimal.AnimalStatus =
            updatedAnimal.AnimalStatus;

        existingAnimal.Breed =
            CleanOptionalText(updatedAnimal.Breed);

        existingAnimal.SireId =
            updatedAnimal.SireId;

        existingAnimal.SireName =
            CleanOptionalText(updatedAnimal.SireName);

        existingAnimal.DamId =
            updatedAnimal.DamId;

        existingAnimal.DamName =
            CleanOptionalText(updatedAnimal.DamName);

        existingAnimal.Notes =
            CleanOptionalText(updatedAnimal.Notes);

        existingAnimal.ProfilePictureUrl =
            CleanOptionalText(updatedAnimal.ProfilePictureUrl);

        existingAnimal.IsFavorite =
            updatedAnimal.IsFavorite;

        existingAnimal.SoldDate =
            updatedAnimal.SoldDate;


        existingAnimal.SoldNotes =
            CleanOptionalText(updatedAnimal.SoldNotes);

        existingAnimal.DeceasedDate =
            updatedAnimal.DeceasedDate;

        existingAnimal.DeceasedNotes =
            CleanOptionalText(updatedAnimal.DeceasedNotes);

        existingAnimal.UpdatedBy =
            CleanOptionalText(updatedAnimal.UpdatedBy);

        existingAnimal.UpdatedAt =
            DateTime.UtcNow;

        ApplyArchiveState(existingAnimal);

        _context.SaveChanges();

        return existingAnimal;
    }

    public Animal? SetFavorite(
        int animalId,
        bool isFavorite)
    {
        var animal = _context.Animals
            .FirstOrDefault(item =>
                item.AnimalId == animalId);

        if (animal == null)
        {
            return null;
        }

        animal.IsFavorite = isFavorite;
        animal.UpdatedAt = DateTime.UtcNow;

        _context.SaveChanges();

        return animal;
    }

    public Animal? UpdateAnimal(
        int animalId,
        UpdateAnimalRequest request)
    {
        var animal = _context.Animals
            .FirstOrDefault(item =>
                item.AnimalId == animalId);

        if (animal == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(request.BarnName))
        {
            animal.BarnName = request.BarnName;
        }

        if (!string.IsNullOrEmpty(request.RegisteredName))
        {
            animal.RegisteredName = request.RegisteredName;
        }

        if (!string.IsNullOrEmpty(request.Breed))
        {
            animal.Breed = request.Breed;
        }

        if (!string.IsNullOrEmpty(request.SireName))
        {
            animal.SireName = request.SireName;
        }

        if (request.CurrentLactation.HasValue)
        {
            animal.CurrentLactation = request.CurrentLactation.Value;
        }

        if (!string.IsNullOrEmpty(request.Notes))
        {
            animal.Notes = request.Notes;
        }

        if (request.IsFavorite.HasValue)
        {
            animal.IsFavorite = request.IsFavorite.Value;
        }

        animal.UpdatedAt = DateTime.UtcNow;

        _context.SaveChanges();

        return animal;
    }

    public Animal? ArchiveAsSold(
        int animalId,
        DateTime soldDate,
        string? soldNotes,
        string? updatedBy)
    {
        var animal = _context.Animals
            .FirstOrDefault(item =>
                item.AnimalId == animalId);

        if (animal == null)
        {
            return null;
        }

        animal.AnimalStatus = AnimalStatus.Sold;
        animal.SoldDate = soldDate;
        animal.SoldNotes = CleanOptionalText(soldNotes);

        animal.DeceasedDate = null;
        animal.DeceasedNotes = null;

        animal.IsFavorite = false;
        animal.UpdatedBy = CleanOptionalText(updatedBy);
        animal.UpdatedAt = DateTime.UtcNow;

        _context.SaveChanges();

        return animal;
    }

    public Animal? ArchiveAsDeceased(
        int animalId,
        DateTime deceasedDate,
        string? deceasedNotes,
        string? updatedBy)
    {
        var animal = _context.Animals
            .FirstOrDefault(item =>
                item.AnimalId == animalId);

        if (animal == null)
        {
            return null;
        }

        animal.AnimalStatus = AnimalStatus.Deceased;
        animal.DeceasedDate = deceasedDate;
        animal.DeceasedNotes =
            CleanOptionalText(deceasedNotes);

        animal.SoldDate = null;
        animal.SoldNotes = null;

        animal.IsFavorite = false;
        animal.UpdatedBy = CleanOptionalText(updatedBy);
        animal.UpdatedAt = DateTime.UtcNow;

        _context.SaveChanges();

        return animal;
    }

    public Animal? RestoreAnimal(
        int animalId,
        string? updatedBy)
    {
        var animal = _context.Animals
            .FirstOrDefault(item =>
                item.AnimalId == animalId);

        if (animal == null)
        {
            return null;
        }

        animal.AnimalStatus = AnimalStatus.Active;

        animal.SoldDate = null;
        animal.SoldNotes = null;

        animal.DeceasedDate = null;
        animal.DeceasedNotes = null;

        animal.UpdatedBy = CleanOptionalText(updatedBy);
        animal.UpdatedAt = DateTime.UtcNow;

        _context.SaveChanges();

        return animal;
    }

    public bool DeleteAnimal(int animalId)
    {
        var animal = _context.Animals
            .FirstOrDefault(item =>
                item.AnimalId == animalId);

        if (animal == null)
        {
            return false;
        }

        using var transaction =
            _context.Database.BeginTransaction();

        try
        {
            var offspringAsDam = _context.Animals
                .Where(item =>
                    item.DamId == animalId)
                .ToList();

            foreach (var offspring in offspringAsDam)
            {
                offspring.DamId = null;

                if (string.IsNullOrWhiteSpace(
                        offspring.DamName))
                {
                    offspring.DamName =
                        animal.RegisteredName ??
                        animal.BarnName;
                }

                offspring.UpdatedAt = DateTime.UtcNow;
            }

            var offspringAsSire = _context.Animals
                .Where(item =>
                    item.SireId == animalId)
                .ToList();

            foreach (var offspring in offspringAsSire)
            {
                offspring.SireId = null;

                if (string.IsNullOrWhiteSpace(
                        offspring.SireName))
                {
                    offspring.SireName =
                        animal.RegisteredName ??
                        animal.BarnName;
                }

                offspring.UpdatedAt = DateTime.UtcNow;
            }

            var calfReferences = _context.CalvingEvents
                .Where(item =>
                    item.CalfAnimalId == animalId)
                .ToList();

            foreach (var calvingEvent in calfReferences)
            {
                calvingEvent.CalfAnimalId = null;
                calvingEvent.UpdatedAt = DateTime.UtcNow;
            }

            var heatEvents = _context.HeatEvents
                .Where(item =>
                    item.AnimalId == animalId);

            var breedingEvents = _context.BreedingEvents
                .Where(item =>
                    item.AnimalId == animalId);

            var calvingEvents = _context.CalvingEvents
                .Where(item =>
                    item.AnimalId == animalId);

            var dryOffEvents = _context.DryOffEvents
                .Where(item =>
                    item.AnimalId == animalId);

            var animalNotes = _context.AnimalNotes
                .Where(item =>
                    item.AnimalId == animalId);

            var classificationRecords =
                _context.ClassificationRecords
                    .Where(item =>
                        item.AnimalId == animalId);

            var lutalyseEvents =
                _context.LutalyseEvents
                    .Where(item =>
                        item.AnimalId == animalId);

            var animalPhotos =
                _context.AnimalPhotos
                    .Where(item =>
                        item.AnimalId == animalId);

            _context.HeatEvents
                .RemoveRange(heatEvents);

            _context.BreedingEvents
                .RemoveRange(breedingEvents);

            _context.CalvingEvents
                .RemoveRange(calvingEvents);

            _context.DryOffEvents
                .RemoveRange(dryOffEvents);

            _context.AnimalNotes
                .RemoveRange(animalNotes);

            _context.ClassificationRecords
                .RemoveRange(classificationRecords);

            _context.LutalyseEvents
                .RemoveRange(lutalyseEvents);

            _context.AnimalPhotos
                .RemoveRange(animalPhotos);

            _context.Animals.Remove(animal);

            _context.SaveChanges();
            transaction.Commit();

            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private void ValidateAnimalRelationships(
        int animalId,
        int? sireId,
        int? damId)
    {
        if (sireId.HasValue &&
            sireId.Value == animalId &&
            animalId != 0)
        {
            throw new InvalidOperationException(
                "An animal cannot be its own sire.");
        }

        if (damId.HasValue &&
            damId.Value == animalId &&
            animalId != 0)
        {
            throw new InvalidOperationException(
                "An animal cannot be its own dam.");
        }

        if (sireId.HasValue &&
            damId.HasValue &&
            sireId.Value == damId.Value)
        {
            throw new InvalidOperationException(
                "The sire and dam cannot be the same animal.");
        }

        if (sireId.HasValue)
        {
            var sireExists = _context.Animals
                .Any(animal =>
                    animal.AnimalId == sireId.Value);

            if (!sireExists)
            {
                throw new InvalidOperationException(
                    "The selected sire does not exist.");
            }
        }

        if (damId.HasValue)
        {
            var damExists = _context.Animals
                .Any(animal =>
                    animal.AnimalId == damId.Value);

            if (!damExists)
            {
                throw new InvalidOperationException(
                    "The selected dam does not exist.");
            }
        }
    }

    private static void ApplyArchiveState(
        Animal animal)
    {
        switch (animal.AnimalStatus)
        {
            case AnimalStatus.Active:
                animal.SoldDate = null;
                animal.SoldNotes = null;
                animal.DeceasedDate = null;
                animal.DeceasedNotes = null;
                break;

            case AnimalStatus.Sold:
                animal.SoldDate ??= DateTime.UtcNow;
                animal.DeceasedDate = null;
                animal.DeceasedNotes = null;
                animal.IsFavorite = false;
                break;

            case AnimalStatus.Deceased:
                animal.DeceasedDate ??= DateTime.UtcNow;
                animal.SoldDate = null;
                animal.SoldNotes = null;
                animal.IsFavorite = false;
                break;
        }
    }

    private static string? CleanOptionalText(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}