using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models.Api;
using RaceDay.Mvc.Models.ViewModels;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public class AccountController : RaceDayController
{
    private readonly IApiClient api;

    public AccountController(IApiClient api)
    {
        this.api = api;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (this.CurrentUser is not null)
        {
            return this.RedirectToHome(this.CurrentUser);
        }

        return this.View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!this.ModelState.IsValid)
        {
            return this.View(model);
        }

        var result = await this.api.LoginAsync(new LoginRequest { Email = model.Email.Trim(), Password = model.Password });
        if (!result.Success)
        {
            // Never reveal whether the email or the password was wrong.
            this.ModelState.AddModelError(string.Empty, result.StatusCode == 401
                ? "The email or password is not correct."
                : result.Message);
            model.Password = string.Empty;
            return this.View(model);
        }

        this.FlashSuccess($"Welcome back, {result.Data!.FullName}!");

        // Only follow local return URLs (prevents open redirects).
        if (!string.IsNullOrEmpty(model.ReturnUrl) && this.Url.IsLocalUrl(model.ReturnUrl))
        {
            return this.LocalRedirect(model.ReturnUrl);
        }

        return this.RedirectToHome(this.CurrentUser!);
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (this.CurrentUser is not null)
        {
            return this.RedirectToHome(this.CurrentUser);
        }

        return this.View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!this.ModelState.IsValid)
        {
            return this.View(model);
        }

        var request = new RegisterRequest
        {
            Role = model.Role,
            FullName = model.FullName.Trim(),
            Email = model.Email.Trim(),
            Password = model.Password,
            DateOfBirth = model.Role == Roles.Participant ? model.DateOfBirth : null
        };

        var result = await this.api.RegisterAsync(request);
        if (!result.Success)
        {
            if (result.IsConflict)
            {
                this.ModelState.AddModelError(nameof(RegisterViewModel.Email), "That email address is already registered. Try logging in instead.");
            }
            else
            {
                this.AddApiErrors(result);
            }

            model.Password = string.Empty;
            model.ConfirmPassword = string.Empty;
            return this.View(model);
        }

        // Log the new user straight in so they land on their own home page.
        var login = await this.api.LoginAsync(new LoginRequest { Email = request.Email, Password = request.Password });
        if (!login.Success)
        {
            this.FlashSuccess("Your account was created. Please log in.");
            return this.RedirectToAction(nameof(this.Login));
        }

        this.FlashSuccess($"Welcome to RaceDay, {login.Data!.FullName}!");
        return this.RedirectToHome(this.CurrentUser!);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await this.api.LogoutAsync();
        this.FlashSuccess("You have been logged out.");
        return this.RedirectToAction(nameof(this.Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => this.View();

    private IActionResult RedirectToHome(SessionUser user) =>
        user.IsOrganiser
            ? this.RedirectToAction("Dashboard", "Organiser")
            : this.RedirectToAction("Index", "Home");
}
