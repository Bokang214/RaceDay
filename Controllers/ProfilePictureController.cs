using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/users/me/picture")]
[Authorize(Roles = "Participant")]
public class ProfilePictureController : ApiControllerBase
{
    private readonly RaceDayContext db;
    private readonly IBlobStorageService storage;

    public ProfilePictureController(RaceDayContext db, IBlobStorageService storage)
    {
        this.db = db;
        this.storage = storage;
    }

    /// <summary>Get the URL of my profile picture (Participant only).</summary>
    /// <response code="200">The picture URL, or null when none has been uploaded.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ProfilePictureResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Get()
    {
        try
        {
            var user = await this.db.Users.FindAsync(User.GetUserId());
            if (user is null) return Unauthorized();

            return Ok(new ProfilePictureResponse { ProfilePictureUrl = user.ProfilePictureUrl });
        }
        catch (Exception ex)
        {
            return ServerError(ex);
        }
    }

    /// <summary>Upload or replace my profile picture (Participant only).</summary>
    /// <remarks>
    /// Send a multipart form with one file field called <c>file</c> (JPG, PNG, GIF or WebP, up to 5 MB).
    /// The API stores the image in Azure Blob Storage and saves its URL on the user.
    /// </remarks>
    /// <param name="request">The image file.</param>
    /// <response code="200">The URL of the stored image.</response>
    /// <response code="400">No file, wrong file type or file too large.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    /// <response code="503">Azure Blob Storage is not configured.</response>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImageUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Upload([FromForm] ImageUploadRequest request)
    {
        try
        {
            var user = await this.db.Users.FindAsync(User.GetUserId());
            if (user is null) return Unauthorized();

            if (!this.storage.IsConfigured)
            {
                return Problem(
                    title: "Image storage is not available.",
                    detail: "Azure Blob Storage has not been configured for this API.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            var check = await ImageFileValidator.ValidateAsync(request.File);
            if (!check.IsValid)
            {
                ModelState.AddModelError("file", check.Error!);
                return ValidationProblem(ModelState);
            }

            await using var stream = request.File!.OpenReadStream();
            var url = await this.storage.UploadImageAsync(stream, check.ContentType, "profile-pictures", check.Extension);

            user.ProfilePictureUrl = url;
            await this.db.SaveChangesAsync();

            return Ok(new ImageUploadResponse { ImageUrl = url });
        }
        catch (Exception ex)
        {
            return ServerError(ex);
        }
    }
}
