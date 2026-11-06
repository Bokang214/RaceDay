using System.Globalization;
using RaceDay.Mvc.Services;

// Numbers and dates typed into the forms always use "." and yyyy-MM-dd (HTML inputs), so the app uses one
// fixed culture. Without this, a Windows machine set to a comma-decimal region would reject "42.2".
CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

var builder = WebApplication.CreateBuilder(args);

// ---------------- MVC ----------------
builder.Services.AddControllersWithViews(options =>
{
    // Every POST form must carry the anti-forgery token.
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());

    // Fields such as Email on the profile page are display-only; do not treat every non-nullable string as [Required].
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

// ---------------- Session management ----------------
// The MVC app keeps the logged-in user (id, role, name) and the API's session cookie in SERVER-SIDE session state.
var idleMinutes = builder.Configuration.GetValue<int?>("Session:IdleTimeoutMinutes") ?? 120;
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Mvc.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(idleMinutes);
});
builder.Services.AddHttpContextAccessor();

// ---------------- The Part 2 API ----------------
// The MVC project has NO database access and no business logic: every operation is an HTTP call to the API.
var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "https://localhost:52944/";
builder.Services
    .AddHttpClient<IApiClient, ApiClient>(client =>
    {
        client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    // The API session cookie is added by hand for each user, so the shared cookie jar is switched off.
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false });

var app = builder.Build();

// Users never see a raw exception page: unexpected errors and unknown URLs get a friendly page.
app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/HttpStatus", "?code={0}");

// Switched off in Docker (the container only listens on HTTP): set UseHttpsRedirection=false.
if (builder.Configuration.GetValue("UseHttpsRedirection", true))
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
