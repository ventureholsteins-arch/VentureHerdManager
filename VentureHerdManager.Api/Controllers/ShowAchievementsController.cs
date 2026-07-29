using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShowAchievementsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ShowAchievementsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<ShowAchievement>>> GetAll()
    {
        return await _context.ShowAchievements
            .AsNoTracking()
            .OrderByDescending(a => a.ShowDate)
            .ThenByDescending(a => a.ShowAchievementId)
            .ToListAsync();
    }

    [HttpGet("animal/{animalId}")]
    public async Task<ActionResult<List<ShowAchievement>>> GetByAnimal(int animalId)
    {
        return await _context.ShowAchievements
            .Where(a => a.AnimalId == animalId)
            .OrderByDescending(a => a.ShowDate)
            .ThenByDescending(a => a.ShowAchievementId)
            .ToListAsync();
    }

    /// <summary>
    /// Returns the most recent achievement per animal — used for dashboard card badges.
    /// </summary>
    [HttpGet("latest-per-animal")]
    public async Task<ActionResult<List<object>>> GetLatestPerAnimal()
    {
        var latest = await _context.ShowAchievements
            .AsNoTracking()
            .GroupBy(a => a.AnimalId)
            .Select(g => g
                .OrderByDescending(a => a.ShowDate)
                .ThenByDescending(a => a.ShowAchievementId)
                .Select(a => new
                {
                    a.ShowAchievementId,
                    a.AnimalId,
                    a.ShowName,
                    a.ShowDate,
                    a.Placed,
                    a.Bagged
                })
                .First())
            .ToListAsync();

        return Ok(latest);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ShowAchievement>> GetById(int id)
    {
        var achievement = await _context.ShowAchievements.FindAsync(id);

        if (achievement == null)
        {
            return NotFound();
        }

        return achievement;
    }

    [HttpPost]
    public async Task<ActionResult<ShowAchievement>> Create(ShowAchievement achievement)
    {
        var animal = await _context.Animals
            .FirstOrDefaultAsync(a => a.AnimalId == achievement.AnimalId);

        if (animal == null)
        {
            return NotFound($"Animal {achievement.AnimalId} was not found.");
        }

        _context.ShowAchievements.Add(achievement);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetByAnimal),
            new { animalId = achievement.AnimalId },
            achievement);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, ShowAchievement achievement)
    {
        if (id != achievement.ShowAchievementId)
        {
            return BadRequest("ID mismatch.");
        }

        var existing = await _context.ShowAchievements.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.ShowName = achievement.ShowName;
        existing.ShowDate = achievement.ShowDate;
        existing.Placed = achievement.Placed;
        existing.Bagged = achievement.Bagged;
        existing.Notes = achievement.Notes;
        existing.UpdatedBy = achievement.UpdatedBy;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var achievement = await _context.ShowAchievements.FindAsync(id);
        if (achievement == null)
        {
            return NotFound();
        }

        _context.ShowAchievements.Remove(achievement);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
