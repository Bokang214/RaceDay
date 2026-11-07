namespace RaceDay.Mvc.Services;

/// <summary>Small presentation helpers shared by the views (icons, colours, date and time formats).</summary>
public static class UiHelpers
{
    public static string TypeIcon(string? type) => type?.Trim().ToLowerInvariant() switch
    {
        "run" => "bi-lightning-charge-fill",
        "walk" => "bi-person-walking",
        "cycle" => "bi-bicycle",
        _ => "bi-flag-fill"
    };

    public static string TypeClass(string? type) => "type-" + (type?.Trim().ToLowerInvariant() ?? "other");

    public static string StatusClass(string? status) => status switch
    {
        "Confirmed" => "status-confirmed",
        "Pending" => "status-pending",
        "Completed" => "status-completed",
        _ => "status-unknown"
    };

    public static string StatusIcon(string? status) => status switch
    {
        "Confirmed" => "bi-check-circle-fill",
        "Pending" => "bi-hourglass-split",
        "Completed" => "bi-trophy-fill",
        _ => "bi-question-circle-fill"
    };

    public static string StatusHint(string? status) => status switch
    {
        "Confirmed" => "Your entry is confirmed. The event is still to come.",
        "Pending" => "The event is over. Waiting for the Organiser to publish results.",
        "Completed" => "Results have been published.",
        _ => string.Empty
    };

    /// <summary>1 -> 1st, 2 -> 2nd, 3 -> 3rd, 11 -> 11th, 47 -> 47th.</summary>
    public static string Ordinal(int number)
    {
        var abs = Math.Abs(number);
        var lastTwo = abs % 100;
        if (lastTwo is >= 11 and <= 13)
        {
            return $"{number}th";
        }

        return (abs % 10) switch
        {
            1 => $"{number}st",
            2 => $"{number}nd",
            3 => $"{number}rd",
            _ => $"{number}th"
        };
    }

    public static string Time(TimeOnly? time) => time is null ? "-" : time.Value.ToString("HH:mm:ss");

    public static string Date(DateOnly date) => date.ToString("ddd, d MMM yyyy");

    public static string ShortDate(DateOnly date) => date.ToString("d MMM yyyy");

    public static string Initials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "?";
        }

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 1
            ? parts[0][..1].ToUpperInvariant()
            : (parts[0][..1] + parts[^1][..1]).ToUpperInvariant();
    }
}
