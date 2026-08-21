using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.Services;

public sealed class DemoSessionMaintenanceService
{
    // A visitor keeps one private showcase herd for a full day. LastSeenAt is
    // still useful operationally, but it must not keep abandoned demo data
    // alive forever when a browser is left open at a fair or kiosk.
    public static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(24);
    private static readonly TimeSpan TouchInterval = TimeSpan.FromMinutes(5);

    private readonly ApplicationDbContext _context;
    private readonly DemoSessionContext _sessionContext;

    public DemoSessionMaintenanceService(
        ApplicationDbContext context,
        DemoSessionContext sessionContext)
    {
        _context = context;
        _sessionContext = sessionContext;
    }

    public async Task TouchAsync(CancellationToken cancellationToken)
    {
        var sessionId = _sessionContext.SessionId;
        if (!_sessionContext.IsDemoMode || sessionId == null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var session = await _context.DemoSessions
            .FindAsync([sessionId], cancellationToken);

        if (session == null)
        {
            _context.DemoSessions.Add(new DemoSession
            {
                DemoSessionId = sessionId,
                CreatedAt = now,
                LastSeenAt = now
            });

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                _context.ChangeTracker.Clear();
            }

            await CleanupExpiredAsync(now, cancellationToken);
            return;
        }

        if (now - session.LastSeenAt >= TouchInterval)
        {
            session.LastSeenAt = now;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task CleanupExpiredAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        var cutoff = now - SessionLifetime;
        var expiredIds = await _context.DemoSessions
            .Where(session => session.CreatedAt < cutoff)
            .Select(session => session.DemoSessionId)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (expiredIds.Count == 0)
        {
            return;
        }

        await DeleteScopedRowsAsync<AnimalPhoto>(expiredIds, cancellationToken);
        await DeleteScopedRowsAsync<AnimalNote>(expiredIds, cancellationToken);
        await DeleteScopedRowsAsync<ClassificationRecord>(expiredIds, cancellationToken);
        await DeleteScopedRowsAsync<ShowAchievement>(expiredIds, cancellationToken);
        await DeleteScopedRowsAsync<EmbryoRecord>(expiredIds, cancellationToken);
        await DeleteScopedRowsAsync<HeatEvent>(expiredIds, cancellationToken);
        await DeleteScopedRowsAsync<BreedingEvent>(expiredIds, cancellationToken);
        await DeleteScopedRowsAsync<DryOffEvent>(expiredIds, cancellationToken);
        await DeleteScopedRowsAsync<LutalyseEvent>(expiredIds, cancellationToken);
        await DeleteScopedRowsAsync<CalvingEvent>(expiredIds, cancellationToken);

        await _context.Animals
            .IgnoreQueryFilters()
            .Where(entity => expiredIds.Contains(
                EF.Property<string>(entity, "DemoSessionId")))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(animal => animal.DamId, (int?)null)
                    .SetProperty(animal => animal.SireId, (int?)null),
                cancellationToken);

        await DeleteScopedRowsAsync<Animal>(expiredIds, cancellationToken);
        await DeleteScopedRowsAsync<AppearanceSetting>(expiredIds, cancellationToken);

        await _context.DemoSessions
            .Where(session => expiredIds.Contains(session.DemoSessionId))
            .ExecuteDeleteAsync(cancellationToken);
    }

    private Task<int> DeleteScopedRowsAsync<TEntity>(
        IReadOnlyCollection<string> sessionIds,
        CancellationToken cancellationToken)
        where TEntity : class =>
        _context.Set<TEntity>()
            .IgnoreQueryFilters()
            .Where(entity => sessionIds.Contains(
                EF.Property<string>(entity, "DemoSessionId")))
            .ExecuteDeleteAsync(cancellationToken);
}
