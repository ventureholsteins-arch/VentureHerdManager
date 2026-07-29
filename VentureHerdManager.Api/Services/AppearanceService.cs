using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.Services;

public class AppearanceService
{
    private readonly ApplicationDbContext _context;

    public AppearanceService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AppearanceSetting> GetAppearanceSettingsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var setting = await _context.AppearanceSettings
                .AsNoTracking()
                .OrderBy(setting => setting.AppearanceSettingId)
                .FirstOrDefaultAsync(cancellationToken);

            if (setting != null)
            {
                return setting;
            }

            return await CreateDefaultAppearanceSettingsAsync(cancellationToken);
        }
        catch (Exception ex) when (IsMissingTableException(ex))
        {
            await _context.Database.EnsureCreatedAsync(cancellationToken);
            return await CreateDefaultAppearanceSettingsAsync(cancellationToken);
        }
    }

    public async Task<AppearanceSetting> UpdateAppearanceSettingsAsync(
        AppearanceSetting updated,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var current = await _context.AppearanceSettings
                .FirstOrDefaultAsync(cancellationToken);

            if (current == null)
            {
                current = new AppearanceSetting();
                _context.AppearanceSettings.Add(current);
            }

            current.FarmName = string.IsNullOrWhiteSpace(updated.FarmName)
                ? "Venture Holsteins"
                : updated.FarmName.Trim();

            current.LogoUrl = string.IsNullOrWhiteSpace(updated.LogoUrl)
                ? null
                : updated.LogoUrl.Trim();

            current.BackgroundImageUrl = string.IsNullOrWhiteSpace(updated.BackgroundImageUrl)
                ? null
                : updated.BackgroundImageUrl.Trim();

            current.BackgroundOpacity = Math.Clamp(updated.BackgroundOpacity, 0m, 1m);
            current.OverlayOpacity = Math.Clamp(updated.OverlayOpacity, 0m, 1m);
            current.Theme = string.IsNullOrWhiteSpace(updated.Theme)
                ? "light"
                : updated.Theme.Trim();

            current.AccentColor = string.IsNullOrWhiteSpace(updated.AccentColor)
                ? "green"
                : updated.AccentColor.Trim();

            current.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return current;
        }
        catch (Exception ex) when (IsMissingTableException(ex))
        {
            await _context.Database.EnsureCreatedAsync(cancellationToken);
            return await UpdateAppearanceSettingsAsync(updated, cancellationToken);
        }
    }

    private async Task<AppearanceSetting> CreateDefaultAppearanceSettingsAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _context.AppearanceSettings
                .AsNoTracking()
                .OrderBy(setting => setting.AppearanceSettingId)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing != null)
            {
                return existing;
            }

            var created = new AppearanceSetting
            {
                FarmName = "Venture Holsteins",
                LogoUrl = "/farm logo.png",
                BackgroundImageUrl = "/2F1D8FDE-C401-4E8F-AA57-0BAC674AD74D.jpg",
                BackgroundOpacity = 0.15m,
                OverlayOpacity = 0.85m,
                Theme = "light",
                AccentColor = "green"
            };

            _context.AppearanceSettings.Add(created);
            await _context.SaveChangesAsync(cancellationToken);

            return created;
        }
        catch (Exception ex) when (IsMissingTableException(ex))
        {
            await _context.Database.EnsureCreatedAsync(cancellationToken);
            return await CreateDefaultAppearanceSettingsAsync(cancellationToken);
        }
    }

    private static bool IsMissingTableException(Exception ex)
    {
        var message = ex.Message ?? string.Empty;
        return message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase)
            || message.Contains("There is no object named", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Could not find object", StringComparison.OrdinalIgnoreCase)
            || message.Contains("does not exist", StringComparison.OrdinalIgnoreCase);
    }
}
