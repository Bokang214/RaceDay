namespace RaceDay.Mvc.Models.Api;

// Plain data shapes that mirror the JSON returned by the RaceDay API. They contain no logic.

public class RegisterRequest
{
    public string Role { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
}

public class RegisterResponse
{
    public int Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public int UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class ProfileResponse
{
    public int Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
}

public class UpdateProfileRequest
{
    public string? FullName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
}

public class ProfilePictureResponse
{
    public string? ProfilePictureUrl { get; set; }
}

public class ImageUploadResponse
{
    public string ImageUrl { get; set; } = string.Empty;
}

public class CategoryResponse
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}

public class CategoryRequest
{
    public string CategoryName { get; set; } = string.Empty;
}

public class EventListItemResponse
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? BannerImageUrl { get; set; }
}

public class EventDetailResponse : EventListItemResponse
{
    public string? Description { get; set; }
    public int OrganiserId { get; set; }
    public List<CategoryResponse> Categories { get; set; } = new();
}

public class EventRequest
{
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public string EventType { get; set; } = string.Empty;
}

public class EventFilter
{
    public string? Search { get; set; }
    public string? Location { get; set; }
    public string? Type { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
}

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
    public string Status { get; set; } = string.Empty;
    public int? ResultId { get; set; }
    public TimeOnly? FinishTime { get; set; }
    public int? Position { get; set; }
}

public class ResultRequest
{
    public TimeOnly? FinishTime { get; set; }
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
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly? EventDate { get; set; }
    public int TotalFinishers { get; set; }
}

public class DashboardEventResponse
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? BannerImageUrl { get; set; }
    public int EnrolmentCount { get; set; }
    public int CategoryCount { get; set; }
    public bool IsUpcoming { get; set; }
}

public class OrganiserDashboardResponse
{
    public int TotalEvents { get; set; }
    public int TotalEnrolments { get; set; }
    public int UpcomingEvents { get; set; }
    public List<DashboardEventResponse> Events { get; set; } = new();
}
