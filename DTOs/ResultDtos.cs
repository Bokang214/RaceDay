using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class CreateResultRequest
{
    [Required]
    public TimeOnly FinishTime { get; set; }

    [Range(1, int.MaxValue)]
    public int? Position { get; set; }
}

public class UpdateResultRequest
{
    public TimeOnly? FinishTime { get; set; }
    [Range(1, int.MaxValue)]
    public int? Position { get; set; }
}

public class ResultResponse
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public TimeOnly? FinishTime { get; set; }
    public int? Position { get; set; }
}

public class LeaderboardEntryResponse
{
    public string ParticipantName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public TimeOnly? FinishTime { get; set; }
    public int? Position { get; set; }

    // Filled in by GET /api/results/me so the web front end can show the full race history.
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly? EventDate { get; set; }
    /// <summary>How many finishers have a result in the same category (for "47th of 312 finishers").</summary>
    public int TotalFinishers { get; set; }
}
