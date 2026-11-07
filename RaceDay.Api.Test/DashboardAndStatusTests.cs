using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDay.Api.Tests;

// Part 3: the Organiser dashboard, enrolment statuses, richer "my results", and the event filters.
public class DashboardAndStatusTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory factory;

    public DashboardAndStatusTests(RaceDayApiFactory factory)
    {
        this.factory = factory;
    }

    private static string DaysFromToday(int days) => DateTime.UtcNow.Date.AddDays(days).ToString("yyyy-MM-dd");

    private static async Task<int> CreateEventOnAsync(HttpClient organiser, string name, string date, string type = "Run")
    {
        var response = await organiser.PostAsJsonAsync("/api/events", new
        {
            eventName = name,
            description = "Test event",
            eventDate = date,
            location = "Cape Town",
            distance = 10,
            eventType = type
        });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("eventId").GetInt32();
    }

    // ---------------- Dashboard ----------------

    [Fact]
    public async Task Dashboard_ShowsOnlyMyEvents_WithEnrolmentCounts()
    {
        var organiser = this.factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(organiser, "Organiser", "mine");
        var upcomingId = await CreateEventOnAsync(organiser, "Dash Upcoming", DaysFromToday(30));
        await CreateEventOnAsync(organiser, "Dash Past", DaysFromToday(-30));
        var categoryId = await TestAuthHelper.CreateCategoryAsync(organiser, upcomingId);

        var participant = this.factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(participant, "Participant");
        await TestAuthHelper.EnrolAsync(participant, categoryId);

        // Another organiser's event must not appear.
        var other = this.factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(other, "Organiser", "other");
        await CreateEventOnAsync(other, "Not Mine", DaysFromToday(10));

        var response = await organiser.GetAsync("/api/organiser/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dashboard = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, dashboard.GetProperty("totalEvents").GetInt32());
        Assert.Equal(1, dashboard.GetProperty("totalEnrolments").GetInt32());
        Assert.Equal(1, dashboard.GetProperty("upcomingEvents").GetInt32());

        var events = dashboard.GetProperty("events");
        Assert.Equal(2, events.GetArrayLength());
        Assert.DoesNotContain(events.EnumerateArray(), e => e.GetProperty("eventName").GetString() == "Not Mine");
        var upcoming = events.EnumerateArray().First(e => e.GetProperty("eventId").GetInt32() == upcomingId);
        Assert.Equal(1, upcoming.GetProperty("enrolmentCount").GetInt32());
        Assert.True(upcoming.GetProperty("isUpcoming").GetBoolean());
    }

    [Fact]
    public async Task Dashboard_Participant_ReturnsForbidden()
    {
        var participant = this.factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(participant, "Participant");

        var response = await participant.GetAsync("/api/organiser/dashboard");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_Anonymous_ReturnsUnauthorized()
    {
        var anonymous = this.factory.CreateClient();

        var response = await anonymous.GetAsync("/api/organiser/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------------- Enrolment status ----------------

    [Fact]
    public async Task Enrolment_ForAFutureEvent_IsConfirmed()
    {
        var organiser = this.factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(organiser, "Organiser");
        var eventId = await CreateEventOnAsync(organiser, "Future Race", DaysFromToday(20));
        var categoryId = await TestAuthHelper.CreateCategoryAsync(organiser, eventId);

        var participant = this.factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(participant, "Participant");
        await TestAuthHelper.EnrolAsync(participant, categoryId);

        var mine = await participant.GetFromJsonAsync<JsonElement>("/api/enrolments/me");

        Assert.Equal(1, mine.GetArrayLength());
        Assert.Equal("Confirmed", mine[0].GetProperty("status").GetString());
        Assert.Equal("Future Race", mine[0].GetProperty("eventName").GetString());
    }

    [Fact]
    public async Task Enrolment_ForAPastEventWithoutResult_IsPending()
    {
        var organiser = this.factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(organiser, "Organiser");
        var eventId = await CreateEventOnAsync(organiser, "Past Race", DaysFromToday(-5));
        var categoryId = await TestAuthHelper.CreateCategoryAsync(organiser, eventId);

        var participant = this.factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(participant, "Participant");
        await TestAuthHelper.EnrolAsync(participant, categoryId);

        var mine = await participant.GetFromJsonAsync<JsonElement>("/api/enrolments/me");

        Assert.Equal("Pending", mine[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task Enrolment_AfterAResultIsCaptured_IsCompleted_AndOrganiserSeesTheResult()
    {
        var organiser = this.factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(organiser, "Organiser");
        var eventId = await CreateEventOnAsync(organiser, "Finished Race", DaysFromToday(-2));
        var categoryId = await TestAuthHelper.CreateCategoryAsync(organiser, eventId);

        var participant = this.factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(participant, "Participant");
        var enrolmentId = await TestAuthHelper.EnrolAsync(participant, categoryId);

        var capture = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results",
            new { finishTime = "00:48:10", position = 4 });
        Assert.Equal(HttpStatusCode.Created, capture.StatusCode);

        var mine = await participant.GetFromJsonAsync<JsonElement>("/api/enrolments/me");
        Assert.Equal("Completed", mine[0].GetProperty("status").GetString());

        var forEvent = await organiser.GetFromJsonAsync<JsonElement>($"/api/events/{eventId}/enrolments");
        Assert.Equal(1, forEvent.GetArrayLength());
        Assert.Equal(4, forEvent[0].GetProperty("position").GetInt32());
        Assert.StartsWith("00:48:10", forEvent[0].GetProperty("finishTime").GetString());
        Assert.NotEqual(JsonValueKind.Null, forEvent[0].GetProperty("resultId").ValueKind);
    }

    // ---------------- My results ----------------

    [Fact]
    public async Task MyResults_IncludeEventDetails_AndTheNumberOfFinishers()
    {
        var organiser = this.factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(organiser, "Organiser");
        var eventId = await CreateEventOnAsync(organiser, "History Race", DaysFromToday(-10));
        var categoryId = await TestAuthHelper.CreateCategoryAsync(organiser, eventId, "Open 10K");

        var first = this.factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(first, "Participant", "first");
        var firstEnrolment = await TestAuthHelper.EnrolAsync(first, categoryId);

        var second = this.factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(second, "Participant", "second");
        var secondEnrolment = await TestAuthHelper.EnrolAsync(second, categoryId);

        await organiser.PostAsJsonAsync($"/api/enrolments/{firstEnrolment}/results", new { finishTime = "00:40:00", position = 1 });
        await organiser.PostAsJsonAsync($"/api/enrolments/{secondEnrolment}/results", new { finishTime = "00:45:00", position = 2 });

        var mine = await second.GetFromJsonAsync<JsonElement>("/api/results/me");

        Assert.Equal(1, mine.GetArrayLength());
        Assert.Equal("History Race", mine[0].GetProperty("eventName").GetString());
        Assert.Equal("Open 10K", mine[0].GetProperty("categoryName").GetString());
        Assert.Equal(2, mine[0].GetProperty("position").GetInt32());
        Assert.Equal(2, mine[0].GetProperty("totalFinishers").GetInt32());
        Assert.Equal(DaysFromToday(-10), mine[0].GetProperty("eventDate").GetString());
    }

    // ---------------- Event filters ----------------

    [Fact]
    public async Task Events_CanBeFilteredByType_AndByName()
    {
        var organiser = this.factory.CreateClient();
        await TestAuthHelper.RegisterAndLoginAsync(organiser, "Organiser");
        var tag = Guid.NewGuid().ToString("N")[..8];
        await CreateEventOnAsync(organiser, $"Ride {tag}", DaysFromToday(15), "Cycle");
        await CreateEventOnAsync(organiser, $"Jog {tag}", DaysFromToday(16), "Run");

        var cycles = await organiser.GetFromJsonAsync<JsonElement>($"/api/events?type=Cycle&search={tag}");
        Assert.Equal(1, cycles.GetArrayLength());
        Assert.Equal($"Ride {tag}", cycles[0].GetProperty("eventName").GetString());

        var both = await organiser.GetFromJsonAsync<JsonElement>($"/api/events?search={tag}");
        Assert.Equal(2, both.GetArrayLength());

        var none = await organiser.GetFromJsonAsync<JsonElement>($"/api/events?type=Walk&search={tag}");
        Assert.Equal(0, none.GetArrayLength());
    }
}
