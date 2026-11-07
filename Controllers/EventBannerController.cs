using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events/{id:int}/banner")]
[Authorize(Roles = "Organiser")]
public class EventBannerController : ApiControllerBase
{
    private readonly RaceDayContext db;
    private readonly IBlobStorageService storage;

    public EventBannerController(RaceDayContext db, IBlobStorageService storage)
    {
        this.db = db;
        this.storage = storage;
    }

    /// <summary>Upload or replace an event banner image (owning Organiser only).</summary>
    /// <remarks>
    /// Send a multipart form with one file field called <c>file</c> (JPG, PNG, GIF or WebP, up to 5 MB).
    /// The API stores the image in Azure Blob Storage and saves its URL on the event.
    /// </remarks>
    /// <param name="id">The event id.</param>
    /// <param name="request">The image file.</param>
    /// <response code="200">The URL of the stored image.</response>
    /// <response code="400">No file, wrong file type or file too large.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Participant, or an Organiser who does not own this event.</response>
    /// <response code="404">No event with that id.</response>
    /// <response code="503">Azure Blob Storage is not configured.</response>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImageUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Upload(int id, [FromForm] ImageUploadRequest request)
    {
        try
        {
            var ev = await this.db.Events.FirstOrDefaultAsync(e => e.EventId == id);
            if (ev is null) return NotFound();
            if (ev.OrganiserId != User.GetUserId()) return Forbid();

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
            var url = await this.storage.UploadImageAsync(stream, check.ContentType, "event-banners", check.Extension);

            ev.BannerImageUrl = url;
            await this.db.SaveChangesAsync();

            return Ok(new ImageUploadResponse { ImageUrl = url });
        }
        catch (Exception ex)
        {
            return ServerError(ex);
        }
    }
}
