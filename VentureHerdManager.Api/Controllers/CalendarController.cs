using Microsoft.AspNetCore.Mvc;
using System.Text;
using VentureHerdManager.Api.Models;
using VentureHerdManager.Api.Services;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CalendarController : ControllerBase
{
    private readonly CalendarService _calendarService;

    public CalendarController(CalendarService calendarService)
    {
        _calendarService = calendarService;
    }

    [HttpGet]
    public IActionResult GetCalendarEvents(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var resolvedStartDate =
            startDate?.Date
            ?? new DateTime(
                DateTime.Today.Year,
                DateTime.Today.Month,
                1);

        var resolvedEndDate =
            endDate?.Date
            ?? resolvedStartDate.AddMonths(1);

        if (resolvedEndDate <= resolvedStartDate)
        {
            return BadRequest(
                "The end date must be after the start date.");
        }

        var events = _calendarService.GetCalendarEvents(
            resolvedStartDate,
            resolvedEndDate);

        return Ok(events);
    }

    [HttpGet("export.ics")]
    public IActionResult ExportCalendarEvents(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var resolvedStartDate =
            startDate?.Date
            ?? new DateTime(
                DateTime.Today.Year,
                DateTime.Today.Month,
                1);

        var resolvedEndDate =
            endDate?.Date
            ?? resolvedStartDate.AddMonths(1);

        if (resolvedEndDate <= resolvedStartDate)
        {
            return BadRequest(
                "The end date must be after the start date.");
        }

        var events = _calendarService.GetCalendarEvents(
            resolvedStartDate,
            resolvedEndDate);

        var ics = BuildIcs(
            events,
            resolvedStartDate,
            resolvedEndDate);

        var fileName =
            $"venture-herd-calendar-" +
            $"{resolvedStartDate:yyyyMMdd}-" +
            $"{resolvedEndDate:yyyyMMdd}.ics";

        return File(
            Encoding.UTF8.GetBytes(ics),
            "text/calendar; charset=utf-8",
            fileName);
    }

    private static string BuildIcs(
        IEnumerable<CalendarEventDto> events,
        DateTime startDate,
        DateTime endDate)
    {
        static string EscapeIcs(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value
                .Replace("\\", "\\\\")
                .Replace(";", "\\;")
                .Replace(",", "\\,")
                .Replace("\r\n", "\\n")
                .Replace("\n", "\\n");
        }

        static string FormatUtc(DateTime value)
        {
            return value
                .ToUniversalTime()
                .ToString("yyyyMMdd'T'HHmmss'Z'");
        }

        var generatedAt = DateTime.UtcNow;
        var lines = new List<string>
        {
            "BEGIN:VCALENDAR",
            "VERSION:2.0",
            "PRODID:-//Venture Herd Manager//Calendar Export//EN",
            "CALSCALE:GREGORIAN",
            "METHOD:PUBLISH",
            "X-WR-CALNAME:Venture Herd Calendar",
            "X-WR-TIMEZONE:UTC",
            $"X-WR-CALDESC:Herd events from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}"
        };

        foreach (var calendarEvent in events)
        {
            var uid = EscapeIcs(
                $"{calendarEvent.EventId}@ventureherdmanager.app");

            var summary = EscapeIcs(
                $"{calendarEvent.AnimalName}: {calendarEvent.Title}");

            var description = EscapeIcs(
                string.IsNullOrWhiteSpace(
                    calendarEvent.Description)
                    ? calendarEvent.EventType
                    : calendarEvent.Description);

            var startUtc = calendarEvent.EventDate.ToUniversalTime();
            var endUtc = startUtc.AddHours(1);

            lines.Add("BEGIN:VEVENT");
            lines.Add($"UID:{uid}");
            lines.Add($"DTSTAMP:{FormatUtc(generatedAt)}");
            lines.Add($"DTSTART:{FormatUtc(startUtc)}");
            lines.Add($"DTEND:{FormatUtc(endUtc)}");
            lines.Add($"SUMMARY:{summary}");
            lines.Add($"DESCRIPTION:{description}");
            lines.Add("END:VEVENT");
        }

        lines.Add("END:VCALENDAR");
        return string.Join("\r\n", lines) + "\r\n";
    }
}