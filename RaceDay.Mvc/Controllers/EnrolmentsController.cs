using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models.ViewModels;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public class EnrolmentsController : RaceDayController
{
    private readonly IApiClient api;

    public EnrolmentsController(IApiClient api)
    {
        this.api = api;
    }

    /// <summary>Participant: enrol in one category of an event.</summary>
    [HttpPost]
    [RoleRequired(Roles.Participant)]
    public async Task<IActionResult> Enrol(int eventId, int? categoryId)
    {
        if (categoryId is null)
        {
            this.FlashError("Please choose a category before enrolling.");
            return this.RedirectToAction("Details", "Events", new { id = eventId });
        }

        var result = await this.api.EnrolAsync(categoryId.Value);
        if (!result.Success)
        {
            if (result.IsConflict)
            {
                this.FlashWarning("You are already enrolled in that category.");
                return this.RedirectToAction("Details", "Events", new { id = eventId });
            }

            return this.Fail(result, "Details", "Events", new { id = eventId });
        }

        this.FlashSuccess("You are in! Your enrolment is confirmed.");
        return this.RedirectToAction(nameof(this.Mine));
    }

    /// <summary>Participant: the events I am enrolled in, with colour-coded status.</summary>
    [HttpGet]
    [RoleRequired(Roles.Participant)]
    public async Task<IActionResult> Mine()
    {
        var result = await this.api.GetMyEnrolmentsAsync();
        if (!result.Success)
        {
            if (result.IsUnauthorized || result.IsForbidden)
            {
                return this.Fail(result, "Mine");
            }

            this.ViewData["LoadError"] = result.Message;
            return this.View(new List<RaceDay.Mvc.Models.Api.EnrolmentDetailResponse>());
        }

        return this.View(result.Data!);
    }

    [HttpPost]
    [RoleRequired(Roles.Participant)]
    public async Task<IActionResult> Withdraw(int id)
    {
        var result = await this.api.WithdrawAsync(id);
        if (!result.Success)
        {
            return this.Fail(result, nameof(this.Mine));
        }

        this.FlashSuccess("You have withdrawn from the event.");
        return this.RedirectToAction(nameof(this.Mine));
    }

    /// <summary>Organiser: everyone enrolled in one of my events.</summary>
    [HttpGet]
    [RoleRequired(Roles.Organiser)]
    public async Task<IActionResult> ForEvent(int eventId, string? category = null)
    {
        var ev = await this.api.GetEventAsync(eventId);
        if (!ev.Success)
        {
            return this.Fail(ev, "Events", "Organiser");
        }

        if (ev.Data!.OrganiserId != this.CurrentUser!.UserId)
        {
            return this.RedirectToAction("AccessDenied", "Account");
        }

        var list = await this.api.GetEventEnrolmentsAsync(eventId);
        if (!list.Success)
        {
            return this.Fail(list, "Events", "Organiser");
        }

        var rows = list.Data!;
        if (!string.IsNullOrWhiteSpace(category))
        {
            rows = rows.Where(r => string.Equals(r.CategoryName, category, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return this.View(new EventEnrolmentsViewModel
        {
            Event = ev.Data,
            Enrolments = rows.OrderBy(r => r.CategoryName).ThenBy(r => r.ParticipantName).ToList(),
            CategoryFilter = category
        });
    }
}
