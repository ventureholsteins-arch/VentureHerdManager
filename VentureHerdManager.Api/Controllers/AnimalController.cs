using Microsoft.AspNetCore.Mvc;
using VentureHerdManager.Api.DTOs;
using VentureHerdManager.Api.Models;
using VentureHerdManager.Api.Services;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnimalsController : ControllerBase
{
    private readonly AnimalService _animalService;
    private readonly AnimalSnapshotService _animalSnapshotService;

    public AnimalsController(
        AnimalService animalService,
        AnimalSnapshotService animalSnapshotService)
    {
        _animalService = animalService;
        _animalSnapshotService = animalSnapshotService;
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

    [HttpGet("archived")]
    public IActionResult GetArchivedAnimals()
    {
        return Ok(_animalService.GetArchivedAnimals());
    }

    [HttpGet("{animalId:int}/snapshot")]
    public async Task<IActionResult> GetAnimalSnapshot(
        int animalId,
        CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await _animalSnapshotService
                .GetSnapshotAsync(animalId, cancellationToken);

            return Ok(snapshot);
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Animal not found.");
        }
    }

    [HttpPut("{animalId:int}/favorite")]
    public IActionResult SetFavorite(
        int animalId,
        [FromQuery] bool isFavorite)
    {
        var animal = _animalService.SetFavorite(
            animalId,
            isFavorite);

        if (animal == null)
        {
            return NotFound("Animal not found.");
        }

        return Ok(animal);
    }

    [HttpPut("{animalId:int}")]
    public IActionResult UpdateAnimal(
        int animalId,
        [FromBody] UpdateAnimalRequest request)
    {
        var animal = _animalService.UpdateAnimal(
            animalId,
            request);

        if (animal == null)
        {
            return NotFound("Animal not found.");
        }

        return Ok(animal);
    }

    [HttpPut("{animalId:int}/archive/sold")]
    public IActionResult ArchiveAsSold(
        int animalId,
        [FromBody] ArchiveAnimalRequest request)
    {
        var animal = _animalService.ArchiveAsSold(
            animalId,
            request.SoldDate,
            request.SoldNotes,
            request.UpdatedBy);

        if (animal == null)
        {
            return NotFound("Animal not found.");
        }

        return Ok(animal);
    }

    [HttpPut("{animalId:int}/archive/deceased")]
    public IActionResult ArchiveAsDeceased(
        int animalId,
        [FromBody] ArchiveAnimalRequest request)
    {
        var animal = _animalService.ArchiveAsDeceased(
            animalId,
            request.SoldDate,
            request.SoldNotes,
            request.UpdatedBy);

        if (animal == null)
        {
            return NotFound("Animal not found.");
        }

        return Ok(animal);
    }

    [HttpPut("{animalId:int}/restore")]
    public IActionResult RestoreAnimal(
        int animalId,
        [FromBody] RestoreAnimalRequest request)
    {
        var animal = _animalService.RestoreAnimal(
            animalId,
            request.UpdatedBy);

        if (animal == null)
        {
            return NotFound("Animal not found.");
        }

        return Ok(animal);
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