namespace RaceDay.Api.DTOs;

/// <summary>Multipart form with a single image file (field name "file").</summary>
public class ImageUploadRequest
{
    /// <summary>The image: JPG, PNG, GIF or WebP, up to 5 MB.</summary>
    public IFormFile? File { get; set; }
}

public class ImageUploadResponse
{
    /// <summary>Public Azure Blob Storage URL of the stored image (also saved in the database).</summary>
    public string ImageUrl { get; set; } = string.Empty;
}

public class ProfilePictureResponse
{
    public string? ProfilePictureUrl { get; set; }
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
