using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class BaggingSchedulesController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet("latest")]
    public async Task<IActionResult> Latest(CancellationToken ct)
    {
        var schedule = await context.SharedBaggingSchedules.AsNoTracking()
            .Where(value => value.IsActive)
            .OrderByDescending(value => value.UpdatedAt)
            .FirstOrDefaultAsync(ct);
        return schedule == null ? NoContent() : Ok(schedule);
    }

    [HttpPost]
    public async Task<IActionResult> Save(SaveBaggingScheduleRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ScheduleJson)) return BadRequest("Add at least one cow before saving the show plan.");
        var schedule = request.SharedBaggingScheduleId.HasValue
            ? await context.SharedBaggingSchedules.FindAsync([request.SharedBaggingScheduleId.Value], ct)
            : null;
        if (schedule == null)
        {
            schedule = new SharedBaggingSchedule();
            context.SharedBaggingSchedules.Add(schedule);
        }
        schedule.ShowName = string.IsNullOrWhiteSpace(request.ShowName) ? "Show Bagging" : request.ShowName.Trim();
        schedule.ShowDate = request.ShowDate;
        schedule.ScheduleJson = request.ScheduleJson;
        schedule.IsActive = true;
        schedule.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
        return Ok(schedule);
    }

    [HttpPost("show-string")]
    public async Task<IActionResult> SaveShowString(SaveShowStringRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ShowStringJson))
            return BadRequest("Add at least one animal before sharing the show string.");
        if (request.ShowStringJson.Length > 2_000_000)
            return BadRequest("The show string is too large to share.");

        var schedule = new SharedBaggingSchedule
        {
            ShowName = "__SHOW_STRING__",
            ShowDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ScheduleJson = request.ShowStringJson,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.SharedBaggingSchedules.Add(schedule);
        await context.SaveChangesAsync(ct);
        return Ok(new { token = schedule.PublicToken });
    }

    [HttpGet("show-string/{token}")]
    public async Task<IActionResult> GetShowString(string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 64) return NotFound();
        var schedule = await context.SharedBaggingSchedules.AsNoTracking()
            .Where(value => value.PublicToken == token && value.IsActive && value.ShowName == "__SHOW_STRING__")
            .Select(value => new { showStringJson = value.ScheduleJson, updatedAt = value.UpdatedAt })
            .FirstOrDefaultAsync(ct);
        return schedule == null ? NotFound() : Ok(schedule);
    }
}

public sealed class SaveBaggingScheduleRequest
{
    public int? SharedBaggingScheduleId { get; set; }
    public string ShowName { get; set; } = "Show Bagging";
    public DateOnly ShowDate { get; set; }
    public string ScheduleJson { get; set; } = "[]";
}

public sealed class SaveShowStringRequest
{
    public string ShowStringJson { get; set; } = string.Empty;
}
