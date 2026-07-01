using VentureHerdManager.Api.Data;

namespace VentureHerdManager.Api.Services;

public class DashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public object GetDashboard()
    {
        var today = DateTime.Today;

        var pregChecksDue = _context.BreedingEvents
            .Where(b => b.PregnancyStatus == Models.PregnancyStatus.Unconfirmed &&
                        b.PregnancyCheckDueDate.Date <= today)
            .OrderBy(b => b.PregnancyCheckDueDate)
            .ToList();

        var recentHeats = _context.HeatEvents
            .OrderByDescending(h => h.HeatDateTime)
            .Take(10)
            .ToList();

        var recentBreedings = _context.BreedingEvents
            .OrderByDescending(b => b.BreedingDate)
            .Take(10)
            .ToList();

        return new
        {
            PregChecksDue = pregChecksDue,
            RecentHeats = recentHeats,
            RecentBreedings = recentBreedings
        };
    }
}