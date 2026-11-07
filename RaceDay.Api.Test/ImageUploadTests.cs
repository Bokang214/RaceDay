using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace RaceDay.Api.Tests;

// Part 3: image uploads. The API receives the file and hands it to blob storage (a fake in these tests).
public class ImageUploadTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory factory;

    public ImageUploadTests(RaceDayApiFactory factory)
    {
        this.factory = factory;
    }

    // A tiny byte sequence that starts with the PNG signature - enough to count as an image.
    private static readonly byte[] PngBytes =
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

    private static MultipartFormDataContent FileForm(byte[] bytes, string fileName, string contentType)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);
        return form;
    }

    private static MultipartFormDataContent TextFileForm()
        => FileForm(Encoding.UTF8.GetBytes("this is not an image"), "notes.png", "image/png");

    // ---------------- Event banner ----------------

    [Fact]
    public async Task Organiser_CanUploadBanner_AndUrlAppearsOnTheEvent()
    {
        var organiser = this.factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(organiser, "Organiser");
        var eventId = await TestAuthHelper.CreateEventAsync(organiser);

        var response = await organiser.PostAsync($"/api/events/{eventId}/banner", FileForm(PngBytes, "banner.png", "image/png"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var url = body.GetProperty("imageUrl").GetString();
        Assert.StartsWith("https://fake-blob.test/", url);
        Assert.EndsWith(".png", url);
        Assert.Contains(url, this.factory.Storage.Uploads);

        var details = await organiser.GetFromJsonAsync<JsonElement>($"/api/events/{eventId}");
        Assert.Equal(url, details.GetProperty("bannerImageUrl").GetString());
    }

    [Fact]
    public async Task OtherOrganiser_CannotUploadBanner_ForSomeoneElsesEvent()
    {
        var owner = this.factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(owner, "Organiser", "owner");
        var eventId = await TestAuthHelper.CreateEventAsync(owner);

        var other = this.factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(other, "Organiser", "other");

        var response = await other.PostAsync($"/api/events/{eventId}/banner", FileForm(PngBytes, "banner.png", "image/png"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotUploadBanner()
    {
        var organiser = this.factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(organiser, "Organiser");
        var eventId = await TestAuthHelper.CreateEventAsync(organiser);

        var participant = this.factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(participant, "Participant");

        var response = await participant.PostAsync($"/api/events/{eventId}/banner", FileForm(PngBytes, "banner.png", "image/png"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotUploadBanner()
    {
        var anonymous = this.factory.CreateClient();

        var response = await anonymous.PostAsync("/api/events/1/banner", FileForm(PngBytes, "banner.png", "image/png"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_WithAFileThatIsNotAnImage_ReturnsBadRequest()
    {
        var organiser = this.factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(organiser, "Organiser");
        var eventId = await TestAuthHelper.CreateEventAsync(organiser);

        var response = await organiser.PostAsync($"/api/events/{eventId}/banner", TextFileForm());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_WithNoFile_ReturnsBadRequest()
    {
        var organiser = this.factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(organiser, "Organiser");
        var eventId = await TestAuthHelper.CreateEventAsync(organiser);

        var form = new MultipartFormDataContent();
        form.Add(new StringContent("nothing here"), "note");

        var response = await organiser.PostAsync($"/api/events/{eventId}/banner", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_ForAnEventThatDoesNotExist_ReturnsNotFound()
    {
        var organiser = this.factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(organiser, "Organiser");

        var response = await organiser.PostAsync("/api/events/999999/banner", FileForm(PngBytes, "banner.png", "image/png"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_WhenStorageIsNotConfigured_ReturnsServiceUnavailable()
    {
        var organiser = this.factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(organiser, "Organiser");
        var eventId = await TestAuthHelper.CreateEventAsync(organiser);

        this.factory.Storage.Configured = false;
        try
        {
            var response = await organiser.PostAsync($"/api/events/{eventId}/banner", FileForm(PngBytes, "banner.png", "image/png"));

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }
        finally
        {
            this.factory.Storage.Configured = true;
        }
    }

    // ---------------- Profile picture ----------------

    [Fact]
    public async Task Participant_CanUploadProfilePicture_AndReadItBack()
    {
        var participant = this.factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(participant, "Participant");

        var before = await participant.GetFromJsonAsync<JsonElement>("/api/users/me/picture");
        Assert.Equal(JsonValueKind.Null, before.GetProperty("profilePictureUrl").ValueKind);

        var upload = await participant.PostAsync("/api/users/me/picture", FileForm(PngBytes, "me.png", "image/png"));
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        var body = await upload.Content.ReadFromJsonAsync<JsonElement>();
        var url = body.GetProperty("imageUrl").GetString();
        Assert.StartsWith("https://fake-blob.test/", url);

        var after = await participant.GetFromJsonAsync<JsonElement>("/api/users/me/picture");
        Assert.Equal(url, after.GetProperty("profilePictureUrl").GetString());
    }

    [Fact]
    public async Task Organiser_CannotUploadProfilePicture()
    {
        var organiser = this.factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(organiser, "Organiser");

        var response = await organiser.PostAsync("/api/users/me/picture", FileForm(PngBytes, "me.png", "image/png"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ProfilePicture_WithAFileThatIsNotAnImage_ReturnsBadRequest()
    {
        var participant = this.factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(participant, "Participant");

        var response = await participant.PostAsync("/api/users/me/picture", TextFileForm());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProfilePicture_Anonymous_ReturnsUnauthorized()
    {
        var anonymous = this.factory.CreateClient();

        var response = await anonymous.PostAsync("/api/users/me/picture", FileForm(PngBytes, "me.png", "image/png"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
