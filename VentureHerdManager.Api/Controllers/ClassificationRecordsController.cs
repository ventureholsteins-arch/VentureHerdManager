using Microsoft.AspNetCore.Mvc;
using VentureHerdManager.Api.DTOs;
using VentureHerdManager.Api.Models;
using VentureHerdManager.Api.Services;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClassificationRecordsController : ControllerBase
{
    private readonly ClassificationService _classificationService;

    public ClassificationRecordsController(ClassificationService classificationService)
    {
        _classificationService = classificationService;
    }

    /// <summary>
    /// Get the latest classification for an animal
    /// </summary>
    [HttpGet("latest/{animalId:int}")]
    public ActionResult<CurrentClassificationDto> GetLatestClassification(int animalId)
    {
        var classification = _classificationService.GetCurrentClassificationDto(animalId);
        if (classification == null)
            return NotFound();

        return Ok(classification);
    }

    /// <summary>
    /// Get classification history for an animal
    /// </summary>
    [HttpGet("history/{animalId:int}")]
    public ActionResult<List<ClassificationRecord>> GetClassificationHistory(int animalId)
    {
        var history = _classificationService.GetClassificationHistory(animalId);
        return Ok(history);
    }

    /// <summary>
    /// Add a new classification record
    /// </summary>
    [HttpPost]
    public ActionResult<ClassificationRecord> AddClassification([FromBody] ClassificationRecord record)
    {
        if (record.AnimalId <= 0)
            return BadRequest("AnimalId must be greater than 0");

        if (record.Score < 0)
            return BadRequest("Score must be non-negative");

        var created = _classificationService.AddClassification(record);
        return CreatedAtAction(nameof(GetLatestClassification), new { animalId = created.AnimalId }, created);
    }

    /// <summary>
    /// Update an existing classification record
    /// </summary>
    [HttpPut("{recordId:int}")]
    public ActionResult<ClassificationRecord> UpdateClassification(int recordId, [FromBody] ClassificationRecord updates)
    {
        try
        {
            var updated = _classificationService.UpdateClassification(recordId, updates);
            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Get the latest classifications for multiple animals
    /// </summary>
    [HttpPost("latest-for-animals")]
    public ActionResult<List<CurrentClassificationDto>> GetLatestForAnimals([FromBody] List<int> animalIds)
    {
        if (!animalIds.Any())
            return BadRequest("At least one animal ID required");

        var classifications = _classificationService.GetLatestClassificationsForAnimals(animalIds);
        return Ok(classifications);
    }
}
