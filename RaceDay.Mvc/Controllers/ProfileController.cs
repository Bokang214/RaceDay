using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models.Api;
using RaceDay.Mvc.Models.ViewModels;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

/// <summary>View and update my profile (any logged-in user); Participants can also upload a profile picture.</summary>
[RoleRequired]
public class ProfileController : RaceDayController
{
    private readonly IApiClient api;

    public ProfileController(IApiClient api)
    {
        this.api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = await this.LoadAsync();
        return model.Failure is not null ? model.Failure : this.View(model.View!);
    }

    [HttpPost]
    public async Task<IActionResult> Update(ProfileViewModel model)
    {
        // Email and role are display-only; reload them so the page renders correctly if validation fails.
        var existing = await this.LoadAsync();
        if (existing.Failure is not null)
        {
            return existing.Failure;
        }

        model.Email = existing.View!.Email;
        model.Role = existing.View.Role;
        model.ProfilePictureUrl = existing.View.ProfilePictureUrl;

        if (!this.ModelState.IsValid)
        {
            return this.View("Index", model);
        }

        var result = await this.api.UpdateProfileAsync(new UpdateProfileRequest
        {
            FullName = model.FullName.Trim(),
            DateOfBirth = model.IsParticipant ? model.DateOfBirth : null
        });

        if (!result.Success)
        {
            if (result.IsUnauthorized || result.IsForbidden)
            {
                return this.Fail(result, nameof(this.Index));
            }

            this.AddApiErrors(result);
            return this.View("Index", model);
        }

        this.HttpContext.Session.SetDisplayName(result.Data!.FullName);
        this.FlashSuccess("Your profile has been updated.");
        return this.RedirectToAction(nameof(this.Index));
    }

    [HttpPost]
    [RoleRequired(Roles.Participant)]
    public async Task<IActionResult> Picture(IFormFile? picture)
    {
        if (picture is null || picture.Length == 0)
        {
            this.FlashError("Please choose an image first.");
            return this.RedirectToAction(nameof(this.Index));
        }

        if (picture.Length > 5 * 1024 * 1024)
        {
            this.FlashError("The picture must be smaller than 5 MB.");
            return this.RedirectToAction(nameof(this.Index));
        }

        var extension = Path.GetExtension(picture.FileName).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".gif" or ".webp"))
        {
            this.FlashError("Please choose a JPG, PNG, GIF or WebP image.");
            return this.RedirectToAction(nameof(this.Index));
        }

        var result = await this.api.UploadProfilePictureAsync(picture);
        if (!result.Success)
        {
            return this.Fail(result, nameof(this.Index));
        }

        this.HttpContext.Session.SetProfilePicture(result.Data!.ImageUrl);
        this.FlashSuccess("Profile picture updated.");
        return this.RedirectToAction(nameof(this.Index));
    }

    private async Task<(ProfileViewModel? View, IActionResult? Failure)> LoadAsync()
    {
        var profile = await this.api.GetProfileAsync();
        if (!profile.Success)
        {
            return (null, this.Fail(profile, "Index", "Home"));
        }

        var view = new ProfileViewModel
        {
            Role = profile.Data!.Role,
            Email = profile.Data.Email,
            FullName = profile.Data.FullName,
            DateOfBirth = profile.Data.DateOfBirth
        };

        if (view.IsParticipant)
        {
            var picture = await this.api.GetProfilePictureAsync();
            if (picture.Success)
            {
                view.ProfilePictureUrl = picture.Data!.ProfilePictureUrl;
                this.HttpContext.Session.SetProfilePicture(view.ProfilePictureUrl);
            }
        }

        return (view, null);
    }
}
