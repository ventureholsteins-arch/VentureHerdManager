using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.DTOs;
using VentureHerdManager.Api.Models;
using VentureHerdManager.Api.Utilities;

namespace VentureHerdManager.Api.Services;

public class ClassificationService
{
    private readonly ApplicationDbContext _context;

    public ClassificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public ClassificationRecord? GetLatestClassification(int animalId)
    {
        return _context.ClassificationRecords
            .AsNoTracking()
            .Where(cr => cr.AnimalId == animalId && cr.ClassificationDate.HasValue)
            .OrderByDescending(cr => cr.ClassificationDate)
            .ThenByDescending(cr => cr.ClassificationRecordId)
            .FirstOrDefault();
    }

    public List<ClassificationRecord> GetClassificationHistory(int animalId)
    {
        return _context.ClassificationRecords
            .AsNoTracking()
            .Where(cr => cr.AnimalId == animalId && cr.ClassificationDate.HasValue)
            .OrderByDescending(cr => cr.ClassificationDate)
            .ThenByDescending(cr => cr.ClassificationRecordId)
            .ToList();
    }

    public CurrentClassificationDto? GetCurrentClassificationDto(int animalId)
    {
        var latest = GetLatestClassification(animalId);
        if (latest == null)
            return null;

        return new CurrentClassificationDto
        {
            AnimalId = animalId,
            Score = latest.Score,
            Baa = latest.Baa,
            Label = ScoreClassification.GetGradeLabel(latest.Score),
            ClassificationDate = latest.ClassificationDate
        };
    }

    public ClassificationRecord AddClassification(ClassificationRecord record)
    {
        record.CreatedAt = DateTime.UtcNow;
        _context.ClassificationRecords.Add(record);
        _context.SaveChanges();
        return record;
    }

    public ClassificationRecord UpdateClassification(int recordId, ClassificationRecord updates)
    {
        var existing = _context.ClassificationRecords.Find(recordId);
        if (existing == null)
            throw new KeyNotFoundException($"Classification record {recordId} not found");

        existing.Score = updates.Score;
        existing.Baa = updates.Baa;
        existing.ClassificationDate = updates.ClassificationDate;
        existing.Notes = updates.Notes;
        existing.AgeInMonthsAtScoring = updates.AgeInMonthsAtScoring;
        existing.ClassificationLabel = updates.ClassificationLabel;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = updates.UpdatedBy;

        _context.SaveChanges();
        return existing;
    }

    /// <summary>
    /// Get latest classifications for multiple milking cows
    /// </summary>
    public List<CurrentClassificationDto> GetLatestClassificationsForAnimals(List<int> animalIds)
    {
        // Materialize to client first, then group - LINQ-to-SQL can't handle complex grouping
        var records = _context.ClassificationRecords
            .AsNoTracking()
            .Where(cr => animalIds.Contains(cr.AnimalId))
            .ToList();

        var latest = records
            .GroupBy(cr => cr.AnimalId)
            .Select(g => g.OrderByDescending(cr => cr.ClassificationDate ?? DateTime.MinValue)
                .ThenByDescending(cr => cr.ClassificationRecordId)
                .First())
            .ToList();

        return latest.Select(cr => new CurrentClassificationDto
        {
            AnimalId = cr.AnimalId,
            Score = cr.Score,
            Baa = cr.Baa,
            Label = ScoreClassification.GetGradeLabel(cr.Score),
            ClassificationDate = cr.ClassificationDate
        }).ToList();
    }
}
