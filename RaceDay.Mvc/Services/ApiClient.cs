using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using RaceDay.Mvc.Models.Api;

namespace RaceDay.Mvc.Services;

/// <summary>Everything the MVC app can ask the RaceDay API to do. Controllers depend on this, never on HttpClient.</summary>
public interface IApiClient
{
    // Authentication
    Task<ApiResult<LoginResponse>> LoginAsync(LoginRequest request);
    Task<ApiResult<RegisterResponse>> RegisterAsync(RegisterRequest request);
    Task LogoutAsync();

    // Events
    Task<ApiResult<List<EventListItemResponse>>> GetEventsAsync(EventFilter? filter = null);
    Task<ApiResult<EventDetailResponse>> GetEventAsync(int id);
    Task<ApiResult<EventDetailResponse>> CreateEventAsync(EventRequest request);
    Task<ApiResult<EventDetailResponse>> UpdateEventAsync(int id, EventRequest request);
    Task<ApiResult<Unit>> DeleteEventAsync(int id);
    Task<ApiResult<ImageUploadResponse>> UploadBannerAsync(int eventId, IFormFile file);

    // Organiser dashboard
    Task<ApiResult<OrganiserDashboardResponse>> GetDashboardAsync();

    // Categories
    Task<ApiResult<List<CategoryResponse>>> GetCategoriesAsync(int eventId);
    Task<ApiResult<CategoryResponse>> CreateCategoryAsync(int eventId, CategoryRequest request);
    Task<ApiResult<CategoryResponse>> UpdateCategoryAsync(int categoryId, CategoryRequest request);
    Task<ApiResult<Unit>> DeleteCategoryAsync(int categoryId);

    // Enrolments
    Task<ApiResult<EnrolmentResponse>> EnrolAsync(int categoryId);
    Task<ApiResult<List<EnrolmentDetailResponse>>> GetMyEnrolmentsAsync();
    Task<ApiResult<List<EnrolmentDetailResponse>>> GetEventEnrolmentsAsync(int eventId);
    Task<ApiResult<Unit>> WithdrawAsync(int enrolmentId);

    // Results
    Task<ApiResult<ResultResponse>> CreateResultAsync(int enrolmentId, ResultRequest request);
    Task<ApiResult<ResultResponse>> UpdateResultAsync(int resultId, ResultRequest request);
    Task<ApiResult<List<LeaderboardEntryResponse>>> GetMyResultsAsync();
    Task<ApiResult<List<LeaderboardEntryResponse>>> GetEventResultsAsync(int eventId);

    // Profile
    Task<ApiResult<ProfileResponse>> GetProfileAsync();
    Task<ApiResult<ProfileResponse>> UpdateProfileAsync(UpdateProfileRequest request);
    Task<ApiResult<ProfilePictureResponse>> GetProfilePictureAsync();
    Task<ApiResult<ImageUploadResponse>> UploadProfilePictureAsync(IFormFile file);
}

/// <summary>Typed HttpClient that calls the Part 2 API and turns every outcome into an <see cref="ApiResult{T}"/>.</summary>
public sealed class ApiClient : IApiClient
{
    private const string ApiCookieName = ".RaceDay.Session";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient http;
    private readonly IHttpContextAccessor accessor;
    private readonly ILogger<ApiClient> logger;

    public ApiClient(HttpClient http, IHttpContextAccessor accessor, ILogger<ApiClient> logger)
    {
        this.http = http;
        this.accessor = accessor;
        this.logger = logger;
    }

    private ISession? Session => this.accessor.HttpContext?.Session;

    // ------------------------------------------------------------------ Authentication

    public async Task<ApiResult<LoginResponse>> LoginAsync(LoginRequest request)
    {
        try
        {
            using var message = this.BuildRequest(HttpMethod.Post, "api/auth/login", ToJson(request), withSession: false);
            using var response = await this.http.SendAsync(message);

            if (!response.IsSuccessStatusCode)
            {
                return await this.FailureAsync<LoginResponse>(response);
            }

            var login = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
            var cookie = ExtractSessionCookie(response);
            if (login is null || cookie is null || this.Session is null)
            {
                return ApiResult<LoginResponse>.Fail(500, "We could not start your session. Please try again.");
            }

            // From now on the user is "logged in" for the MVC app: remember who they are and the API's cookie.
            this.Session.SignIn(login, cookie);
            return ApiResult<LoginResponse>.Ok(login);
        }
        catch (Exception ex)
        {
            return this.Unreachable<LoginResponse>(ex);
        }
    }

    public Task<ApiResult<RegisterResponse>> RegisterAsync(RegisterRequest request) =>
        this.SendAsync<RegisterResponse>(HttpMethod.Post, "api/auth/register", ToJson(request), withSession: false);

    public async Task LogoutAsync()
    {
        try
        {
            // Ends the session on the API side too. Failure is not important: the MVC session is cleared either way.
            await this.SendAsync<Unit>(HttpMethod.Post, "api/auth/logout");
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "API logout failed; clearing the MVC session anyway.");
        }
        finally
        {
            this.Session?.Clear();
        }
    }

    // ------------------------------------------------------------------ Events

    public Task<ApiResult<List<EventListItemResponse>>> GetEventsAsync(EventFilter? filter = null)
    {
        var query = new List<string>();
        void Add(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                query.Add($"{key}={Uri.EscapeDataString(value.Trim())}");
            }
        }

        if (filter is not null)
        {
            Add("search", filter.Search);
            Add("location", filter.Location);
            Add("type", filter.Type);
            Add("from", filter.From?.ToString("yyyy-MM-dd"));
            Add("to", filter.To?.ToString("yyyy-MM-dd"));
        }

        var url = query.Count == 0 ? "api/events" : "api/events?" + string.Join('&', query);
        return this.SendAsync<List<EventListItemResponse>>(HttpMethod.Get, url);
    }

    public Task<ApiResult<EventDetailResponse>> GetEventAsync(int id) =>
        this.SendAsync<EventDetailResponse>(HttpMethod.Get, $"api/events/{id}");

    public Task<ApiResult<EventDetailResponse>> CreateEventAsync(EventRequest request) =>
        this.SendAsync<EventDetailResponse>(HttpMethod.Post, "api/events", ToJson(request));

    public Task<ApiResult<EventDetailResponse>> UpdateEventAsync(int id, EventRequest request) =>
        this.SendAsync<EventDetailResponse>(HttpMethod.Put, $"api/events/{id}", ToJson(request));

    public Task<ApiResult<Unit>> DeleteEventAsync(int id) =>
        this.SendAsync<Unit>(HttpMethod.Delete, $"api/events/{id}");

    public Task<ApiResult<ImageUploadResponse>> UploadBannerAsync(int eventId, IFormFile file) =>
        this.SendAsync<ImageUploadResponse>(HttpMethod.Post, $"api/events/{eventId}/banner", FileContent(file));

    // ------------------------------------------------------------------ Dashboard

    public Task<ApiResult<OrganiserDashboardResponse>> GetDashboardAsync() =>
        this.SendAsync<OrganiserDashboardResponse>(HttpMethod.Get, "api/organiser/dashboard");

    // ------------------------------------------------------------------ Categories

    public Task<ApiResult<List<CategoryResponse>>> GetCategoriesAsync(int eventId) =>
        this.SendAsync<List<CategoryResponse>>(HttpMethod.Get, $"api/events/{eventId}/categories");

    public Task<ApiResult<CategoryResponse>> CreateCategoryAsync(int eventId, CategoryRequest request) =>
        this.SendAsync<CategoryResponse>(HttpMethod.Post, $"api/events/{eventId}/categories", ToJson(request));

    public Task<ApiResult<CategoryResponse>> UpdateCategoryAsync(int categoryId, CategoryRequest request) =>
        this.SendAsync<CategoryResponse>(HttpMethod.Put, $"api/categories/{categoryId}", ToJson(request));

    public Task<ApiResult<Unit>> DeleteCategoryAsync(int categoryId) =>
        this.SendAsync<Unit>(HttpMethod.Delete, $"api/categories/{categoryId}");

    // ------------------------------------------------------------------ Enrolments

    public Task<ApiResult<EnrolmentResponse>> EnrolAsync(int categoryId) =>
        this.SendAsync<EnrolmentResponse>(HttpMethod.Post, $"api/categories/{categoryId}/enrolments");

    public Task<ApiResult<List<EnrolmentDetailResponse>>> GetMyEnrolmentsAsync() =>
        this.SendAsync<List<EnrolmentDetailResponse>>(HttpMethod.Get, "api/enrolments/me");

    public Task<ApiResult<List<EnrolmentDetailResponse>>> GetEventEnrolmentsAsync(int eventId) =>
        this.SendAsync<List<EnrolmentDetailResponse>>(HttpMethod.Get, $"api/events/{eventId}/enrolments");

    public Task<ApiResult<Unit>> WithdrawAsync(int enrolmentId) =>
        this.SendAsync<Unit>(HttpMethod.Delete, $"api/enrolments/{enrolmentId}");

    // ------------------------------------------------------------------ Results

    public Task<ApiResult<ResultResponse>> CreateResultAsync(int enrolmentId, ResultRequest request) =>
        this.SendAsync<ResultResponse>(HttpMethod.Post, $"api/enrolments/{enrolmentId}/results", ToJson(request));

    public Task<ApiResult<ResultResponse>> UpdateResultAsync(int resultId, ResultRequest request) =>
        this.SendAsync<ResultResponse>(HttpMethod.Put, $"api/results/{resultId}", ToJson(request));

    public Task<ApiResult<List<LeaderboardEntryResponse>>> GetMyResultsAsync() =>
        this.SendAsync<List<LeaderboardEntryResponse>>(HttpMethod.Get, "api/results/me");

    public Task<ApiResult<List<LeaderboardEntryResponse>>> GetEventResultsAsync(int eventId) =>
        this.SendAsync<List<LeaderboardEntryResponse>>(HttpMethod.Get, $"api/events/{eventId}/results");

    // ------------------------------------------------------------------ Profile

    public Task<ApiResult<ProfileResponse>> GetProfileAsync() =>
        this.SendAsync<ProfileResponse>(HttpMethod.Get, "api/users/me");

    public Task<ApiResult<ProfileResponse>> UpdateProfileAsync(UpdateProfileRequest request) =>
        this.SendAsync<ProfileResponse>(HttpMethod.Put, "api/users/me", ToJson(request));

    public Task<ApiResult<ProfilePictureResponse>> GetProfilePictureAsync() =>
        this.SendAsync<ProfilePictureResponse>(HttpMethod.Get, "api/users/me/picture");

    public Task<ApiResult<ImageUploadResponse>> UploadProfilePictureAsync(IFormFile file) =>
        this.SendAsync<ImageUploadResponse>(HttpMethod.Post, "api/users/me/picture", FileContent(file));

    // ------------------------------------------------------------------ Plumbing

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, HttpContent? content = null, bool withSession = true)
    {
        try
        {
            using var message = this.BuildRequest(method, url, content, withSession);
            using var response = await this.http.SendAsync(message);

            if (!response.IsSuccessStatusCode)
            {
                return await this.FailureAsync<T>(response);
            }

            var status = (int)response.StatusCode;
            if (typeof(T) == typeof(Unit) || response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
            {
                return ApiResult<T>.Ok(default!, status);
            }

            var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
            return data is null
                ? ApiResult<T>.Fail(502, "The RaceDay service returned an empty response.")
                : ApiResult<T>.Ok(data, status);
        }
        catch (Exception ex)
        {
            return this.Unreachable<T>(ex);
        }
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string url, HttpContent? content, bool withSession)
    {
        var message = new HttpRequestMessage(method, url) { Content = content };
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (withSession)
        {
            // Forward the API session cookie that belongs to the logged-in MVC user.
            var cookie = this.Session?.GetApiCookie();
            if (!string.IsNullOrEmpty(cookie))
            {
                message.Headers.TryAddWithoutValidation("Cookie", cookie);
            }
        }

        return message;
    }

    private static StringContent ToJson(object body) =>
        new(JsonSerializer.Serialize(body, JsonOptions), System.Text.Encoding.UTF8, "application/json");

    private static MultipartFormDataContent FileContent(IFormFile file)
    {
        var form = new MultipartFormDataContent();
        var stream = new StreamContent(file.OpenReadStream());
        stream.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);

        // "File" is the property name on the API's ImageUploadRequest.
        form.Add(stream, "File", Path.GetFileName(file.FileName));
        return form;
    }

    /// <summary>Finds ".RaceDay.Session=xxxx" in the API's Set-Cookie headers.</summary>
    private static string? ExtractSessionCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            return null;
        }

        foreach (var header in values)
        {
            var pair = header.Split(';', 2)[0].Trim();
            if (pair.StartsWith(ApiCookieName + "=", StringComparison.Ordinal))
            {
                return pair;
            }
        }

        return null;
    }

    private async Task<ApiResult<T>> FailureAsync<T>(HttpResponseMessage response)
    {
        var status = (int)response.StatusCode;
        string? message = null;
        var errors = new Dictionary<string, string[]>();

        try
        {
            var body = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(body))
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in doc.RootElement.EnumerateObject())
                    {
                        var name = property.Name.ToLowerInvariant();
                        if (name == "errors" && property.Value.ValueKind == JsonValueKind.Object)
                        {
                            foreach (var field in property.Value.EnumerateObject())
                            {
                                if (field.Value.ValueKind == JsonValueKind.Array)
                                {
                                    errors[field.Name] = field.Value.EnumerateArray()
                                        .Select(item => item.GetString() ?? string.Empty)
                                        .Where(text => text.Length > 0)
                                        .ToArray();
                                }
                            }
                        }
                        else if ((name == "message" || name == "detail") && property.Value.ValueKind == JsonValueKind.String)
                        {
                            message ??= property.Value.GetString();
                        }
                        else if (name == "title" && property.Value.ValueKind == JsonValueKind.String && status != 400)
                        {
                            message ??= property.Value.GetString();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            this.logger.LogDebug(ex, "Could not read the API error body.");
        }

        message ??= status switch
        {
            400 => "Some of the details are not valid. Please check the form.",
            401 => "Please log in to continue.",
            403 => "You do not have permission to do that.",
            404 => "We could not find what you were looking for.",
            409 => "That conflicts with something that already exists.",
            503 => "This feature is not available right now. Please try again later.",
            >= 500 => "Something went wrong on our side. Please try again.",
            _ => "The request could not be completed."
        };

        return ApiResult<T>.Fail(status, message, errors);
    }

    private ApiResult<T> Unreachable<T>(Exception ex)
    {
        this.logger.LogError(ex, "Could not reach the RaceDay API.");
        return ApiResult<T>.Fail(503, "We could not reach the RaceDay service. Please try again in a moment.");
    }
}
