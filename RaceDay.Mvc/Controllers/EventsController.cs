using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models.ViewModels;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public class EventsController : RaceDayController
{
    private readonly IApiClient api;

    public EventsController(IApiClient api)
    {
        this.api = api;
    }

    /// <summary>Event detail page: banner, details and categories. Anyone can read it; Participants can enrol.</summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var result = await this.api.GetEventAsync(id);
        if (!result.Success)
        {
            return result.IsNotFound ? this.NotFound() : this.Fail(result, "Index", "Home");
        }

        var user = this.CurrentUser;
        var model = new EventDetailsViewModel
        {
            Event = result.Data!,
            IsUpcoming = result.Data!.EventDate >= Today,
            IsOwner = user is not null && user.IsOrganiser && user.UserId == result.Data.OrganiserId
        };

        if (user is not null && user.IsParticipant)
        {
            var mine = await this.api.GetMyEnrolmentsAsync();
            if (mine.Success)
            {
                model.MyEnrolments = mine.Data!.Where(e => e.EventId == id).ToList();
            }
        }

        return this.View(model);
    }
}
