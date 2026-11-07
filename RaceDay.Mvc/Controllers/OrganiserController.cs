using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models.Api;
using RaceDay.Mvc.Models.ViewModels;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

/// <summary>Organiser-only pages: dashboard and event management.</summary>
[RoleRequired(Roles.Organiser)]
public class OrganiserController : RaceDayController
{
    private readonly IApiClient api;

    public OrganiserController(IApiClient api)
    {
        this.api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var result = await this.api.GetDashboardAsync();
        if (!result.Success)
        {
            if (result.IsUnauthorized || result.IsForbidden)
            {
                return this.Fail(result, "Dashboard");
            }

            // Show the page with a friendly message instead of an error screen (also avoids a redirect loop).
            this.ViewData["LoadError"] = result.Message;
            return this.View(new OrganiserDashboardResponse());
        }

        return this.View(result.Data!);
    }

    /// <summary>The Organiser's own events, with create / edit / delete.</summary>
    [HttpGet]
    public async Task<IActionResult> Events()
    {
        var result = await this.api.GetDashboardAsync();
        if (!result.Success)
        {
            if (result.IsUnauthorized || result.IsForbidden)
            {
                return this.Fail(result, "Dashboard");
            }

            this.ViewData["LoadError"] = result.Message;
            return this.View(new OrganiserDashboardResponse());
        }

        return this.View(result.Data!);
    }

    [HttpGet]
    public IActionResult Create() =>
        this.View("EventForm", new EventFormViewModel { EventDate = Today.AddDays(30) });

    [HttpPost]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        this.ValidateBanner(model);
        if (!this.ModelState.IsValid)
        {
            return this.View("EventForm", model);
        }

        var created = await this.api.CreateEventAsync(ToRequest(model));
        if (!created.Success)
        {
            if (created.IsUnauthorized || created.IsForbidden)
            {
                return this.Fail(created, "Dashboard");
            }

            this.AddApiErrors(created);
            return this.View("EventForm", model);
        }

        var eventId = created.Data!.EventId;
        if (model.Banner is { Length: > 0 })
        {
            var upload = await this.api.UploadBannerAsync(eventId, model.Banner);
            if (!upload.Success)
            {
                this.FlashWarning($"The event was created, but the banner could not be saved: {upload.Message}");
                return this.RedirectToAction(nameof(this.Edit), new { id = eventId });
            }
        }

        this.FlashSuccess("Event created. Now add the categories people can enrol in.");
        return this.RedirectToAction("Index", "Categories", new { eventId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await this.api.GetEventAsync(id);
        if (!result.Success)
        {
            return this.Fail(result, "Events");
        }

        if (result.Data!.OrganiserId != this.CurrentUser!.UserId)
        {
            return this.RedirectToAction("AccessDenied", "Account");
        }

        return this.View("EventForm", new EventFormViewModel
        {
            EventId = result.Data.EventId,
            EventName = result.Data.EventName,
            Description = result.Data.Description,
            EventDate = result.Data.EventDate,
            Location = result.Data.Location,
            Distance = result.Data.Distance,
            EventType = result.Data.EventType,
            BannerImageUrl = result.Data.BannerImageUrl
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model)
    {
        model.EventId = id;
        this.ValidateBanner(model);
        if (!this.ModelState.IsValid)
        {
            return this.View("EventForm", model);
        }

        var updated = await this.api.UpdateEventAsync(id, ToRequest(model));
        if (!updated.Success)
        {
            if (updated.IsUnauthorized || updated.IsForbidden || updated.IsNotFound)
            {
                return this.Fail(updated, "Events");
            }

            this.AddApiErrors(updated);
            model.BannerImageUrl ??= (await this.api.GetEventAsync(id)).Data?.BannerImageUrl;
            return this.View("EventForm", model);
        }

        if (model.Banner is { Length: > 0 })
        {
            var upload = await this.api.UploadBannerAsync(id, model.Banner);
            if (!upload.Success)
            {
                this.FlashWarning($"The event was saved, but the new banner could not be uploaded: {upload.Message}");
                return this.RedirectToAction(nameof(this.Edit), new { id });
            }
        }

        this.FlashSuccess("Event updated.");
        return this.RedirectToAction(nameof(this.Events));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await this.api.DeleteEventAsync(id);
        if (!result.Success)
        {
            return this.Fail(result, "Events");
        }

        this.FlashSuccess("Event deleted.");
        return this.RedirectToAction(nameof(this.Events));
    }

    private void ValidateBanner(EventFormViewModel model)
    {
        if (model.Banner is null || model.Banner.Length == 0)
        {
            return;
        }

        // Friendly pre-check; the API repeats these checks and has the final say.
        if (model.Banner.Length > 5 * 1024 * 1024)
        {
            this.ModelState.AddModelError(nameof(EventFormViewModel.Banner), "The banner must be smaller than 5 MB.");
        }

        var extension = Path.GetExtension(model.Banner.FileName).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".gif" or ".webp"))
        {
            this.ModelState.AddModelError(nameof(EventFormViewModel.Banner), "Please choose a JPG, PNG, GIF or WebP image.");
        }
    }

    private static EventRequest ToRequest(EventFormViewModel model) => new()
    {
        EventName = model.EventName.Trim(),
        Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
        EventDate = model.EventDate!.Value,
        Location = model.Location.Trim(),
        Distance = model.Distance!.Value,
        EventType = model.EventType
    };
}
