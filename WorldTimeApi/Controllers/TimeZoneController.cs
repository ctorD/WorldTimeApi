using Microsoft.AspNetCore.Mvc;

namespace WorldTimeApi.Controllers;
public record TimeZoneResponse(String currentTime, bool isDst);
[ApiController]
[Route("time-zone")]
public class TimeZoneController : ControllerBase
{

    private readonly ILogger<TimeZoneController> _logger;

    public TimeZoneController(ILogger<TimeZoneController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<TimeZoneResponse> Get(string timeZone)
    {
        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        if (timeZoneInfo == null) return BadRequest("Time Zone not found");

        var currentTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, timeZoneInfo);
        var isDst = timeZoneInfo.IsDaylightSavingTime(currentTime);

        return Ok(new TimeZoneResponse(currentTime.ToString(@"hh\:mm"), isDst));
    }
}
