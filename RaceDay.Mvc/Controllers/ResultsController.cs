using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models.Api;
using RaceDay.Mvc.Models.ViewModels;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public class ResultsController : RaceDayController
{
    private readonly IApiClient api;

    public ResultsController(IApiClient api)
    {
        this.api = api;
    }

    /// <summary>Participant: my published results.</summary>
    [HttpGet]
    [RoleRequired(Roles.Participant)]
    public async Task<IActionResult> Mine()
    {
        var result = await this.api.GetMyResultsAsync();
        if (!result.Success)
        {
            if (result.IsUnauthorized || result.IsForbidden)
            {
                return this.Fail(result, "Mine");
            }

            this.ViewData["LoadError"] = result.Message;
            return this.View(new List<LeaderboardEntryResponse>());
        }

        return this.View(result.Data!);
    }

    /// <summary>Organiser: enter finish times and positions for an event's participants.</summary>
    [HttpGet]
    [RoleRequired(Roles.Organiser)]
    public async Task<IActionResult> Entry(int eventId)
    {
        var model = await this.BuildEntryAsync(eventId);
        return model.Failure is not null ? model.Failure : this.View(model.View!);
    }

    [HttpPost]
    [RoleRequired(Roles.Organiser)]
    public async Task<IActionResult> Save(int eventId, List<ResultRowViewModel> rows)
    {
        var current = await this.BuildEntryAsync(eventId);
        if (current.Failure is not null)
        {
            return current.Failure;
        }

        if (!current.View!.EventHasHappened)
        {
            this.FlashError("Results can only be published after the event has taken place.");
            return this.RedirectToAction(nameof(this.Entry), new { eventId });
        }

        // Only rows that really belong to this event may be saved (ids come from the server's list, not the form).
        var allowed = current.View.Rows.ToDictionary(r => r.EnrolmentId);
        var saved = 0;
        var problems = new List<string>();

        foreach (var row in rows ?? new List<ResultRowViewModel>())
        {
            if (!allowed.TryGetValue(row.EnrolmentId, out var known) || row.FinishTime is null)
            {
                continue;
            }

            if (row.Position is < 1)
            {
                problems.Add($"{known.ParticipantName}: the position must be 1 or more.");
                continue;
            }

            var request = new ResultRequest { FinishTime = row.FinishTime, Position = row.Position };
            var outcome = known.ResultId is int existingId
                ? await this.api.UpdateResultAsync(existingId, request)
                : await this.api.CreateResultAsync(row.EnrolmentId, request);

            if (outcome.Success)
            {
                saved++;
            }
            else if (outcome.IsUnauthorized || outcome.IsForbidden)
            {
                return this.Fail(outcome, nameof(this.Entry), null, new { eventId });
            }
            else
            {
                problems.Add($"{known.ParticipantName}: {outcome.Message}");
            }
        }

        if (saved > 0)
        {
            this.FlashSuccess(saved == 1 ? "1 result saved." : $"{saved} results saved.");
        }

        if (problems.Count > 0)
        {
            this.FlashError("Some results were not saved. " + string.Join(" ", problems));
        }
        else if (saved == 0)
        {
            this.FlashWarning("Nothing to save. Enter at least one finish time.");
        }

        return this.RedirectToAction(nameof(this.Entry), new { eventId });
    }

    private async Task<(ResultsEntryViewModel? View, IActionResult? Failure)> BuildEntryAsync(int eventId)
    {
        var ev = await this.api.GetEventAsync(eventId);
        if (!ev.Success)
        {
            return (null, this.Fail(ev, "Events", "Organiser"));
        }

        if (ev.Data!.OrganiserId != this.CurrentUser!.UserId)
        {
            return (null, this.RedirectToAction("AccessDenied", "Account"));
        }

        var enrolments = await this.api.GetEventEnrolmentsAsync(eventId);
        if (!enrolments.Success)
        {
            return (null, this.Fail(enrolments, "Events", "Organiser"));
        }

        var view = new ResultsEntryViewModel
        {
            EventId = eventId,
            EventName = ev.Data.EventName,
            EventDate = ev.Data.EventDate,
            EventHasHappened = ev.Data.EventDate < Today,
            Rows = enrolments.Data!
                .OrderBy(e => e.CategoryName)
                .ThenBy(e => e.Position ?? int.MaxValue)
                .ThenBy(e => e.ParticipantName)
                .Select(e => new ResultRowViewModel
                {
                    EnrolmentId = e.EnrolmentId,
                    ResultId = e.ResultId,
                    ParticipantName = e.ParticipantName,
                    CategoryName = e.CategoryName,
                    FinishTime = e.FinishTime,
                    Position = e.Position
                })
                .ToList()
        };

        return (view, null);
    }
}
