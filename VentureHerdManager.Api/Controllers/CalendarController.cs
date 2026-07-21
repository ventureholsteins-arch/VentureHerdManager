using Microsoft.AspNetCore.Mvc;
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
}