using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
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
        return _context.Animals
            .AsNoTracking()
            .OrderBy(animal => animal.BarnName)
            .ThenBy(animal => animal.RegisteredName)
            .ToList();
    }

    public Animal? GetAnimalById(int animalId)
    {
        return _context.Animals
            .AsNoTracking()
            .FirstOrDefault(animal =>
                animal.AnimalId == animalId);
    }

    public List<Animal> SearchAnimals(string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return GetAllAnimals();
        }

        var normalizedSearch = searchText.Trim();

        return _context.Animals
            .AsNoTracking()
            .Where(animal =>
                (
                    animal.BarnName != null &&
                    animal.BarnName.Contains(normalizedSearch)
                )
                ||
                (
                    animal.RegisteredName != null &&
                    animal.RegisteredName.Contains(normalizedSearch)
                )
                ||
                (
                    animal.RegistrationNumber != null &&
                    animal.RegistrationNumber.Contains(
                        normalizedSearch)
                ))
            .OrderBy(animal => animal.BarnName)
            .ThenBy(animal => animal.RegisteredName)
            .ToList();
    }

    public Animal CreateAnimal(Animal animal)
    {
        animal.AnimalId = 0;

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

        _context.SaveChanges();

        return existingAnimal;
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

            _context.HeatEvents.RemoveRange(heatEvents);
            _context.BreedingEvents.RemoveRange(breedingEvents);
            _context.CalvingEvents.RemoveRange(calvingEvents);
            _context.DryOffEvents.RemoveRange(dryOffEvents);
            _context.AnimalNotes.RemoveRange(animalNotes);

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

    private static string? CleanOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}