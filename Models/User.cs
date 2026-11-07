using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

// Matches Users table in database_schema.sql.
// NOTE (documented deviation from the Part 1 script): the Part 1 API Endpoint
// Plan requires an optional DateOfBirth for Participant registration/profile
// ("dateOfBirth" in /api/auth/register and /api/users/me), but the Users
// table in database_schema.sql does not define that column. A nullable
// DateOfBirth column has been added here (and in the EF Core migration) so
// the functional requirement in Part 2, section 3, can be met. No other
// column, type, key or constraint from the original script was changed.
public class User
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Participant";
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateOnly? DateOfBirth { get; set; }

    // Part 3: public URL of the profile picture stored in Azure Blob Storage (uploaded through the API).
    [MaxLength(500)]
    public string? ProfilePictureUrl { get; set; }

    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
