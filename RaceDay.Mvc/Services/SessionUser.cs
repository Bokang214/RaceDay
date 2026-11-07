using RaceDay.Mvc.Models.Api;

namespace RaceDay.Mvc.Services;

public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

/// <summary>The logged-in user, as remembered in the MVC session.</summary>
public sealed record SessionUser(int UserId, string Role, string FullName, string Email, string? ProfilePictureUrl)
{
    public bool IsOrganiser => this.Role == Roles.Organiser;
    public bool IsParticipant => this.Role == Roles.Participant;
}

/// <summary>Reads and writes the logged-in user in the server-side session.</summary>
public static class RaceDaySessionExtensions
{
    private const string UserIdKey = "UserId";
    private const string RoleKey = "Role";
    private const string NameKey = "FullName";
    private const string EmailKey = "Email";
    private const string PictureKey = "ProfilePictureUrl";
    private const string ApiCookieKey = "ApiSessionCookie";

    /// <summary>Stores the user id, role and the API's session cookie after a successful login.</summary>
    public static void SignIn(this ISession session, LoginResponse login, string apiSessionCookie)
    {
        session.Clear();
        session.SetInt32(UserIdKey, login.UserId);
        session.SetString(RoleKey, login.Role);
        session.SetString(NameKey, login.FullName);
        session.SetString(EmailKey, login.Email);
        session.SetString(ApiCookieKey, apiSessionCookie);
    }

    public static SessionUser? GetSessionUser(this ISession session)
    {
        var id = session.GetInt32(UserIdKey);
        var role = session.GetString(RoleKey);
        if (id is null || string.IsNullOrEmpty(role))
        {
            return null;
        }

        return new SessionUser(
            id.Value,
            role,
            session.GetString(NameKey) ?? string.Empty,
            session.GetString(EmailKey) ?? string.Empty,
            session.GetString(PictureKey));
    }

    public static string? GetApiCookie(this ISession session) => session.GetString(ApiCookieKey);

    public static void SetProfilePicture(this ISession session, string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            session.Remove(PictureKey);
        }
        else
        {
            session.SetString(PictureKey, url);
        }
    }

    public static void SetDisplayName(this ISession session, string fullName) => session.SetString(NameKey, fullName);
}
