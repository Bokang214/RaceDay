using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/organiser/dashboard")]
[Authorize(Roles = "Organiser")]
public class OrganiserDashboardController : ApiControllerBase
{
    private readonly RaceDayContext db;

    public OrganiserDashboardController(RaceDayContext db)
    {
        this.db = db;
    }

    /// <summary>Dashboard summary for the logged-in Organiser.</summary>
    /// <remarks>Totals plus every event the Organiser created, with its enrolment count, ordered by date.</remarks>
    /// <response code="200">The dashboard summary.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant.</response>
    [HttpGet]
    [ProducesResponseType(typeof(OrganiserDashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Get()
    {
        try
        {
            var organiserId = User.GetUserId();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var events = await this.db.Events
                .Where(e => e.OrganiserId == organiserId)
                .OrderBy(e => e.EventDate)
                .Select(e => new DashboardEventResponse
                {
                    EventId = e.EventId,
                    EventName = e.EventName,
                    EventDate = e.EventDate,
                    Location = e.Location,
                    Distance = e.Distance,
                    EventType = e.EventType,
                    BannerImageUrl = e.BannerImageUrl,
                    EnrolmentCount = e.Enrolments.Count(),
                    CategoryCount = e.Categories.Count(),
                    IsUpcoming = e.EventDate >= today
                })
                .ToListAsync();

            return Ok(new OrganiserDashboardResponse
            {
                TotalEvents = events.Count,
                TotalEnrolments = events.Sum(e => e.EnrolmentCount),
                UpcomingEvents = events.Count(e => e.IsUpcoming),
                Events = events
            });
        }
        catch (Exception ex)
        {
            return ServerError(ex);
        }
    }
}
