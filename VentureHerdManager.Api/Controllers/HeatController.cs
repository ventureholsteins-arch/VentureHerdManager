using Microsoft.AspNetCore.Mvc;
using VentureHerdManager.Api.Models;
using VentureHerdManager.Api.Services;
using VentureHerdManager.Api.DTOs;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/animals/{animalId}/heats")]
public class HeatController : ControllerBase
{
    private readonly HeatService _heatService;

    public HeatController(HeatService heatService)
    {
        _heatService = heatService;
    }

    [HttpPost]
    public IActionResult RecordHeat(int animalId, RecordHeatRequest request)
{
    var createdHeat = _heatService.RecordHeat(animalId, request);

    return Ok(createdHeat);
}

    [HttpGet]
    public IActionResult GetHeatsForAnimal(int animalId)
    {
        return Ok(_heatService.GetHeatsForAnimal(animalId));
    }
}