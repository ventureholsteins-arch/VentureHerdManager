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
}

public sealed class SaveBaggingScheduleRequest
{
    public int? SharedBaggingScheduleId { get; set; }
    public string ShowName { get; set; } = "Show Bagging";
    public DateOnly ShowDate { get; set; }
    public string ScheduleJson { get; set; } = "[]";
}
