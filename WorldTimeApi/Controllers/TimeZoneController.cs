using Microsoft.AspNetCore.Mvc;

namespace WorldTimeApi.Controllers;
public record TimeZoneResponse(DateTime currentTime, bool isDst, DateTime? dstStart, DateTime? dstEnd);
[ApiController]
[Route("api")]
public class TimeZoneController : ControllerBase
{

    private readonly ILogger<TimeZoneController> _logger;

    public TimeZoneController(ILogger<TimeZoneController> logger)
    {
        _logger = logger;
    }

    [HttpGet("time-zone")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<TimeZoneResponse> GetTimeZone(string timeZone)
    {
        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        if (timeZoneInfo == null) return BadRequest("Time Zone not found");

        var result = GetTimeZoneResponse(timeZone);

        return Ok(result);
    }

    [HttpGet("time-zones")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<TimeZoneResponse> Get(string[] timeZones)
    {
        var result = timeZones.Select(tz => GetTimeZoneResponse(tz));
        if(result == null)
        {
            return NotFound();
        }
        if(result.Where(tzr => tzr == null).Any())
        {
            return BadRequest("Could not find timezone");
        }

        return Ok(result);
    }

    private TimeZoneResponse? GetTimeZoneResponse(string timeZone)
    {
        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        if (timeZoneInfo == null) {
            return null;
        }
        var dstInfo = FindDstTransitions(2025, timeZone);

        var currentTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, timeZoneInfo);
        var isDst = timeZoneInfo.IsDaylightSavingTime(currentTime);

        return new TimeZoneResponse(currentTime, isDst, dstInfo?.dstStart, dstInfo?.dstEnd);
    }

    public record DstResult(DateTime dstStart, DateTime dstEnd);

    public static DstResult? FindDstTransitions(int year, string timeZoneId)
    {
        // Get the specified time zone
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

        // Get all DST transitions for the specified year
        DateTime startOfYear = new DateTime(year, 1, 1);
        DateTime endOfYear = new DateTime(year, 12, 31, 23, 59, 59);

        var adjustmentRules = timeZone.GetAdjustmentRules();

        foreach (var rule in adjustmentRules)
        {
            if (rule.DateStart <= endOfYear && rule.DateEnd >= startOfYear)
            {
                // Calculate DST start and end for the specified year
                DateTime dstStart = GetTransitionDate(year, rule.DaylightTransitionStart);
                DateTime dstEnd = GetTransitionDate(year, rule.DaylightTransitionEnd);

                return new DstResult(dstStart, dstEnd);
                // Convert to local time for display
            } 
        }
        return null;
    }

    private static DateTime GetTransitionDate(int year, TimeZoneInfo.TransitionTime transition)
    {
        DateTime date;

        if (transition.IsFixedDateRule)
        {
            // Fixed date rule (e.g., March 15)
            date = new DateTime(year, transition.Month, transition.Day);
        }
        else
        {
            // Floating date rule (e.g., second Sunday in March)
            date = GetDayOfWeekInMonth(year, transition.Month, transition.DayOfWeek, transition.Week);
        }

        // Add the transition time
        return date.Add(transition.TimeOfDay.TimeOfDay);
    }

    private static DateTime GetDayOfWeekInMonth(int year, int month, DayOfWeek dayOfWeek, int occurrence)
    {
        // Get the first day of the month
        DateTime firstDay = new DateTime(year, month, 1);

        // Find the first occurrence of the specified day of week
        int daysToAdd = ((int)dayOfWeek - (int)firstDay.DayOfWeek + 7) % 7;
        DateTime firstOccurrence = firstDay.AddDays(daysToAdd);

        // Find the nth occurrence (occurrence is 1-based, so we subtract 1)
        // If occurrence is 5 (last), we need to find the last occurrence in the month
        if (occurrence == 5)
        {
            DateTime lastOccurrence = firstOccurrence;
            DateTime nextOccurrence = lastOccurrence.AddDays(7);

            while (nextOccurrence.Month == month)
            {
                lastOccurrence = nextOccurrence;
                nextOccurrence = nextOccurrence.AddDays(7);
            }

            return lastOccurrence;
        }
        else
        {
            return firstOccurrence.AddDays(7 * (occurrence - 1));
        }
    }
}
