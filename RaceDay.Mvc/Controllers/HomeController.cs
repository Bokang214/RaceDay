using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Models.Api;
using RaceDay.Mvc.Models.ViewModels;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public class HomeController : RaceDayController
{
    private readonly IApiClient api;

    public HomeController(IApiClient api)
    {
        this.api = api;
    }

    /// <summary>The home page: upcoming events with filters.</summary>
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] EventFilterViewModel filter)
    {
        var user = this.CurrentUser;
        if (user is not null && user.IsOrganiser)
        {
            return this.RedirectToAction("Dashboard", "Organiser");
        }

        // By default only events that have not happened yet are shown.
        var from = filter.From ?? (filter.IncludePast ? null : Today);
        var result = await this.api.GetEventsAsync(new EventFilter
        {
            Search = filter.Search,
            Location = filter.Location,
            Type = filter.Type,
            From = from,
            To = filter.To
        });

        var model = new HomeIndexViewModel { Filter = filter };
        if (result.Success)
        {
            model.Events = result.Data!;
        }
        else
        {
            this.ViewData["LoadError"] = result.Message;
        }

        if (user is not null && user.IsParticipant)
        {
            var mine = await this.api.GetMyEnrolmentsAsync();
            if (mine.Success)
            {
                model.EnrolledEventIds = mine.Data!.Select(e => e.EventId).ToHashSet();
            }
        }

        return this.View(model);
    }

    [HttpGet]
    public IActionResult Error() => this.View("Error", new ErrorViewModel());

    [HttpGet]
    public IActionResult HttpStatus(int code)
    {
        var model = code switch
        {
            404 => new ErrorViewModel { Code = 404, Title = "Page not found", Message = "We could not find that page. It may have been moved or deleted." },
            403 => new ErrorViewModel { Code = 403, Title = "Not allowed", Message = "You do not have access to that page." },
            _ => new ErrorViewModel { Code = code, Title = "Something went wrong", Message = "An unexpected error occurred. Please try again." }
        };

        this.Response.StatusCode = code;
        return this.View("Error", model);
    }
}
