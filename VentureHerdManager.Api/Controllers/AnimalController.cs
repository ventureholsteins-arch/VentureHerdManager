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

    [HttpGet("{animalId:int}")]
    public IActionResult GetAnimalById(int animalId)
    {
        var animal =
            _animalService.GetAnimalById(animalId);

        if (animal == null)
        {
            return NotFound("Animal not found.");
        }

        return Ok(animal);
    }

    [HttpGet("search")]
    public IActionResult SearchAnimals(
        [FromQuery] string? searchText)
    {
        return Ok(
            _animalService.SearchAnimals(searchText));
    }

    [HttpPost]
    public IActionResult CreateAnimal(Animal animal)
    {
        var createdAnimal =
            _animalService.CreateAnimal(animal);

        return CreatedAtAction(
            nameof(GetAnimalById),
            new
            {
                animalId = createdAnimal.AnimalId
            },
            createdAnimal);
    }

    [HttpPut("{animalId:int}")]
    public IActionResult UpdateAnimal(
        int animalId,
        Animal updatedAnimal)
    {
        var animal =
            _animalService.UpdateAnimal(
                animalId,
                updatedAnimal);

        if (animal == null)
        {
            return NotFound("Animal not found.");
        }

        return Ok(animal);
    }

    [HttpDelete("{animalId:int}")]
    public IActionResult DeleteAnimal(int animalId)
    {
        var deleted =
            _animalService.DeleteAnimal(animalId);

        if (!deleted)
        {
            return NotFound("Animal not found.");
        }

        return NoContent();
    }
}