using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models.Api;
using RaceDay.Mvc.Models.ViewModels;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

/// <summary>Organiser-only: manage the age or distance categories of an event.</summary>
[RoleRequired(Roles.Organiser)]
public class CategoriesController : RaceDayController
{
    private readonly IApiClient api;

    public CategoriesController(IApiClient api)
    {
        this.api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int eventId)
    {
        var model = await this.LoadAsync(eventId);
        return model is null ? this.NotFound() : this.View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Add(int eventId, string newCategoryName)
    {
        newCategoryName = (newCategoryName ?? string.Empty).Trim();
        if (newCategoryName.Length == 0 || newCategoryName.Length > 100)
        {
            this.FlashError("Please enter a category name of up to 100 characters.");
            return this.RedirectToAction(nameof(this.Index), new { eventId });
        }

        var result = await this.api.CreateCategoryAsync(eventId, new CategoryRequest { CategoryName = newCategoryName });
        if (!result.Success)
        {
            return this.Fail(result, nameof(this.Index), null, new { eventId });
        }

        this.FlashSuccess($"Category \"{newCategoryName}\" added.");
        return this.RedirectToAction(nameof(this.Index), new { eventId });
    }

    [HttpPost]
    public async Task<IActionResult> Rename(int eventId, int categoryId, string categoryName)
    {
        categoryName = (categoryName ?? string.Empty).Trim();
        if (categoryName.Length == 0 || categoryName.Length > 100)
        {
            this.FlashError("Please enter a category name of up to 100 characters.");
            return this.RedirectToAction(nameof(this.Index), new { eventId });
        }

        var result = await this.api.UpdateCategoryAsync(categoryId, new CategoryRequest { CategoryName = categoryName });
        if (!result.Success)
        {
            return this.Fail(result, nameof(this.Index), null, new { eventId });
        }

        this.FlashSuccess("Category renamed.");
        return this.RedirectToAction(nameof(this.Index), new { eventId });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int eventId, int categoryId)
    {
        var result = await this.api.DeleteCategoryAsync(categoryId);
        if (!result.Success)
        {
            if (result.IsConflict)
            {
                this.FlashError("That category already has enrolments, so it cannot be deleted.");
                return this.RedirectToAction(nameof(this.Index), new { eventId });
            }

            return this.Fail(result, nameof(this.Index), null, new { eventId });
        }

        this.FlashSuccess("Category deleted.");
        return this.RedirectToAction(nameof(this.Index), new { eventId });
    }

    private async Task<CategoriesViewModel?> LoadAsync(int eventId)
    {
        var ev = await this.api.GetEventAsync(eventId);
        if (!ev.Success || ev.Data!.OrganiserId != this.CurrentUser!.UserId)
        {
            return null;
        }

        return new CategoriesViewModel { Event = ev.Data, Categories = ev.Data.Categories };
    }
}
