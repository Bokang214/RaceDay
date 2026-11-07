using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Filters;

/// <summary>
/// Protects a controller or action. Users who are not logged in are redirected to the login page; users
/// who are logged in with the wrong role are sent to the "access denied" page.
/// <c>[RoleRequired]</c> = any logged-in user, <c>[RoleRequired("Organiser")]</c> = Organisers only.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RoleRequiredAttribute : Attribute, IAuthorizationFilter
{
    private readonly string? role;

    public RoleRequiredAttribute(string? role = null)
    {
        this.role = role;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.Session.GetSessionUser();

        if (user is null)
        {
            var returnUrl = HttpMethods.IsGet(context.HttpContext.Request.Method)
                ? context.HttpContext.Request.Path + context.HttpContext.Request.QueryString
                : null;

            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (this.role is not null && !string.Equals(user.Role, this.role, StringComparison.Ordinal))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}
