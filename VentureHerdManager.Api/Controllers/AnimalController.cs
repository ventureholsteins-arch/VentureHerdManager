using Microsoft.AspNetCore.Mvc;
using VentureHerdManager.Api.Models;
using VentureHerdManager.Api.Services;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnimalsController : ControllerBase
{
    private readonly AnimalService _animalService;

    public AnimalsController(AnimalService animalService)
    {
        _animalService = animalService;
    }

    [HttpGet]
    public IActionResult GetAllAnimals()
    {
        return Ok(_animalService.GetAllAnimals());
    }
[HttpGet("{animalId}")]
public IActionResult GetAnimalById(int animalId)
{
    var animal = _animalService.GetAnimalById(animalId);

    if (animal == null)
    {
        return NotFound("Animal not found.");
    }

    return Ok(animal);
}


[HttpGet("search")]
public IActionResult SearchAnimals(string searchText)
{
    var animals = _animalService.SearchAnimals(searchText);

    if (!animals.Any())
    {
        return NotFound("No animals found.");
    }

    return Ok(animals);
}

    [HttpPost]
    public IActionResult CreateAnimal(Animal animal)
    {
        var createdAnimal = _animalService.CreateAnimal(animal);

        return CreatedAtAction(nameof(GetAllAnimals), new { id = createdAnimal.AnimalId }, createdAnimal);
    }

    [HttpPut("{animalId}")]
public IActionResult UpdateAnimal(int animalId, Animal updatedAnimal)
{
    var animal = _animalService.UpdateAnimal(animalId, updatedAnimal);

    if (animal == null)
    {
        return NotFound("Animal not found.");
    }

    return Ok(animal);
}
}