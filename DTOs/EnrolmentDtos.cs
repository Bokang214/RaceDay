namespace RaceDay.Api.DTOs;

public class EnrolmentResponse
{
    public int EnrolmentId { get; set; }
    public int UserId { get; set; }
    public int EventId { get; set; }
    public int CategoryId { get; set; }
    public DateTime EnrolmentDate { get; set; }
}

public class EnrolmentDetailResponse : EnrolmentResponse
{
    public string ParticipantName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;

    public DateOnly EventDate { get; set; }
    public string EventLocation { get; set; } = string.Empty;

    /// <summary>Confirmed (event still to come), Pending (event over, result not captured yet) or Completed (result captured).</summary>
    public string Status { get; set; } = EnrolmentStatuses.Confirmed;

    // Filled in once the Organiser has captured a result.
    public int? ResultId { get; set; }
    public TimeOnly? FinishTime { get; set; }
    public int? Position { get; set; }
}

/// <summary>
/// Enrolment status shown in the web front end. It is worked out from the data (no extra column):
/// Completed = a result exists, Pending = the event date has passed but no result yet, Confirmed = otherwise.
/// </summary>
public static class EnrolmentStatuses
{
    public const string Confirmed = "Confirmed";
    public const string Pending = "Pending";
    public const string Completed = "Completed";
}
