using System.ComponentModel.DataAnnotations;
using RaceDay.Mvc.Models.Api;

namespace RaceDay.Mvc.Models.ViewModels;

public static class EventOptions
{
    public static readonly string[] Types = { "Run", "Walk", "Cycle" };
}

// ---------------------------------------------------------------- Account

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "That does not look like an email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please choose how you will use RaceDay.")]
    [RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Please choose Participant or Organiser.")]
    public string Role { get; set; } = "Participant";

    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(100, ErrorMessage = "Your name can be at most 100 characters.")]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "That does not look like an email address.")]
    [StringLength(150, ErrorMessage = "Your email can be at most 150 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a password.")]
    [StringLength(255, MinimumLength = 8, ErrorMessage = "Your password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please repeat your password.")]
    [Compare(nameof(Password), ErrorMessage = "The two passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateOnly? DateOfBirth { get; set; }
}

// ---------------------------------------------------------------- Events (public / participant)

public class EventFilterViewModel
{
    public string? Search { get; set; }
    public string? Type { get; set; }
    public string? Location { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }

    /// <summary>When false (the default) only events from today onwards are listed.</summary>
    public bool IncludePast { get; set; }

    public bool HasAny => !string.IsNullOrWhiteSpace(this.Search) || !string.IsNullOrWhiteSpace(this.Type)
        || !string.IsNullOrWhiteSpace(this.Location) || this.From is not null || this.To is not null || this.IncludePast;
}

public class HomeIndexViewModel
{
    public List<EventListItemResponse> Events { get; set; } = new();
    public EventFilterViewModel Filter { get; set; } = new();
    public HashSet<int> EnrolledEventIds { get; set; } = new();
}

public class EventDetailsViewModel
{
    public EventDetailResponse Event { get; set; } = new();
    public bool IsUpcoming { get; set; }
    public bool IsOwner { get; set; }

    /// <summary>The Participant's current enrolments in this event (category ids), so we can show "Enrolled".</summary>
    public List<EnrolmentDetailResponse> MyEnrolments { get; set; } = new();
}

// ---------------------------------------------------------------- Organiser

public class EventFormViewModel
{
    public int? EventId { get; set; }

    [Required(ErrorMessage = "Please enter the event name.")]
    [StringLength(150, ErrorMessage = "The name can be at most 150 characters.")]
    [Display(Name = "Event name")]
    public string EventName { get; set; } = string.Empty;

    [StringLength(4000, ErrorMessage = "The description is too long.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose the event date.")]
    [DataType(DataType.Date)]
    [Display(Name = "Date")]
    public DateOnly? EventDate { get; set; }

    [Required(ErrorMessage = "Please enter the location.")]
    [StringLength(200, ErrorMessage = "The location can be at most 200 characters.")]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the distance in kilometres.")]
    [Range(0.01, 99999.99, ErrorMessage = "The distance must be more than 0 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? Distance { get; set; }

    [Required(ErrorMessage = "Please choose the event type.")]
    [RegularExpression("^(?i:run|walk|cycle)$", ErrorMessage = "The type must be Run, Walk or Cycle.")]
    [Display(Name = "Type")]
    public string EventType { get; set; } = "Run";

    /// <summary>The banner already saved for this event (edit page).</summary>
    public string? BannerImageUrl { get; set; }

    [Display(Name = "Banner image")]
    public IFormFile? Banner { get; set; }
}

public class CategoriesViewModel
{
    public EventDetailResponse Event { get; set; } = new();
    public List<CategoryResponse> Categories { get; set; } = new();

    [Required(ErrorMessage = "Please enter a category name.")]
    [StringLength(100, ErrorMessage = "The name can be at most 100 characters.")]
    [Display(Name = "Category name")]
    public string NewCategoryName { get; set; } = string.Empty;
}

public class EventEnrolmentsViewModel
{
    public EventDetailResponse Event { get; set; } = new();
    public List<EnrolmentDetailResponse> Enrolments { get; set; } = new();
    public string? CategoryFilter { get; set; }
}

public class ResultRowViewModel
{
    public int EnrolmentId { get; set; }
    public int? ResultId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public TimeOnly? FinishTime { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "The position must be 1 or more.")]
    public int? Position { get; set; }
}

public class ResultsEntryViewModel
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public bool EventHasHappened { get; set; }
    public List<ResultRowViewModel> Rows { get; set; } = new();
}

// ---------------------------------------------------------------- Participant

public class MyResultViewModel
{
    public LeaderboardEntryResponse Entry { get; set; } = new();
}

public class ProfileViewModel
{
    public string Role { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(100, ErrorMessage = "Your name can be at most 100 characters.")]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateOnly? DateOfBirth { get; set; }

    public string? ProfilePictureUrl { get; set; }

    [Display(Name = "Profile picture")]
    public IFormFile? Picture { get; set; }

    public bool IsParticipant => this.Role == "Participant";
}

public class ErrorViewModel
{
    public int Code { get; set; } = 500;
    public string Title { get; set; } = "Something went wrong";
    public string Message { get; set; } = "An unexpected error occurred. Please try again.";
}
