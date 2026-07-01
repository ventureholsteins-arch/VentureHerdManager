using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.Services;

public class AnimalService
{
    private readonly List<Animal> _animals = new();

    public List<Animal> GetAllAnimals()
    {
        return _animals;
    }

public List<Animal> SearchAnimals(string searchText)
{
    return _animals.Where(a =>
        (a.BarnName != null &&
         a.BarnName.Contains(searchText, StringComparison.OrdinalIgnoreCase))

        ||

        (a.RegisteredName != null &&
         a.RegisteredName.Contains(searchText, StringComparison.OrdinalIgnoreCase))

        ||

        (a.RegistrationNumber != null &&
         a.RegistrationNumber.Contains(searchText, StringComparison.OrdinalIgnoreCase))
    ).ToList();
}

    public Animal CreateAnimal(Animal animal)
    {
        animal.AnimalId = _animals.Count + 1;

        _animals.Add(animal);

        return animal;
    }
}