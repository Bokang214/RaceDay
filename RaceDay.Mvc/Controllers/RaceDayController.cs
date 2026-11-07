using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

/// <summary>Shared helpers for all MVC controllers: current user, flash messages and API failure handling.</summary>
public abstract class RaceDayController : Controller
{
    protected SessionUser? CurrentUser => this.HttpContext.Session.GetSessionUser();

    protected static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    protected void FlashSuccess(string message) => this.TempData["Success"] = message;

    protected void FlashError(string message) => this.TempData["Error"] = message;

    protected void FlashWarning(string message) => this.TempData["Warning"] = message;

    /// <summary>
    /// Turns a failed API call into the right response: expired session -> login, wrong role -> access denied,
    /// missing item -> friendly 404 page, anything else -> a flash message and a redirect to <paramref name="action"/>.
    /// </summary>
    protected IActionResult Fail<T>(ApiResult<T> result, string action, string? controller = null, object? routeValues = null)
    {
        if (result.IsUnauthorized)
        {
            this.HttpContext.Session.Clear();
            this.FlashError("Your session has ended. Please log in again.");
            return this.RedirectToAction("Login", "Account");
        }

        if (result.IsForbidden)
        {
            return this.RedirectToAction("AccessDenied", "Account");
        }

        if (result.IsNotFound)
        {
            return this.NotFound();
        }

        this.FlashError(result.Message);
        return this.RedirectToAction(action, controller, routeValues);
    }

    /// <summary>Copies the API's validation messages into ModelState so they show next to the form fields.</summary>
    protected void AddApiErrors<T>(ApiResult<T> result)
    {
        if (result.Errors.Count == 0)
        {
            this.ModelState.AddModelError(string.Empty, result.Message);
            return;
        }

        foreach (var (key, messages) in result.Errors)
        {
            var field = key.StartsWith("$.", StringComparison.Ordinal) ? key[2..] : key;
            foreach (var message in messages)
            {
                this.ModelState.AddModelError(field, message);
            }
        }
    }
}
