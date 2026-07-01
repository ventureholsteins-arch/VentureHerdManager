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
        return _context.Animals.ToList();
    }

    public List<Animal> SearchAnimals(string searchText)
    {
        return _context.Animals
            .Where(a =>
                (a.BarnName != null && a.BarnName.Contains(searchText)) ||
                (a.RegisteredName != null && a.RegisteredName.Contains(searchText)) ||
                (a.RegistrationNumber != null && a.RegistrationNumber.Contains(searchText)))
            .ToList();
    }

    public Animal CreateAnimal(Animal animal)
    {
        _context.Animals.Add(animal);
        _context.SaveChanges();

        return animal;
    }
public Animal? UpdateAnimal(int animalId, Animal updatedAnimal)
{
    var existingAnimal = _context.Animals.FirstOrDefault(a => a.AnimalId == animalId);

    if (existingAnimal == null)
    {
        return null;
    }

    existingAnimal.BarnName = updatedAnimal.BarnName;
    existingAnimal.RegisteredName = updatedAnimal.RegisteredName;
    existingAnimal.RegistrationNumber = updatedAnimal.RegistrationNumber;
    existingAnimal.BirthDate = updatedAnimal.BirthDate;
    existingAnimal.Sex = updatedAnimal.Sex;
    existingAnimal.AnimalStage = updatedAnimal.AnimalStage;
    existingAnimal.AnimalStatus = updatedAnimal.AnimalStatus;
    existingAnimal.Breed = updatedAnimal.Breed;
    existingAnimal.SireId = updatedAnimal.SireId;
    existingAnimal.SireName = updatedAnimal.SireName;
    existingAnimal.DamId = updatedAnimal.DamId;
    existingAnimal.DamName = updatedAnimal.DamName;
    existingAnimal.Notes = updatedAnimal.Notes;

    _context.SaveChanges();

    return existingAnimal;
}

}