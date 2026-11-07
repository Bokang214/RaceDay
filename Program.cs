using System.Reflection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------- Database (EF Core, Code-First against SQL Server) ----------------
// The unit-test project replaces this registration with an in-memory database.
builder.Services.AddDbContext<RaceDayContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RaceDayContext")));

// ---------------- Azure Blob Storage (Part 3) ----------------
// Images (event banners, profile pictures) are uploaded to the API, which stores them in Azure Blob Storage.
// The MVC front end never talks to Azure directly.
builder.Services.Configure<BlobStorageOptions>(builder.Configuration.GetSection(BlobStorageOptions.SectionName));
builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();

// ---------------- Session management + authentication ----------------
// Login stores the user's id and role in SERVER-SIDE session state; the client only
// receives an opaque session cookie. SessionAuthenticationHandler turns that session
// back into a ClaimsPrincipal on every request.
var idleMinutes = builder.Configuration.GetValue<int?>("Session:IdleTimeoutMinutes") ?? 120;
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(idleMinutes);
});

builder.Services
    .AddAuthentication(SessionAuthenticationDefaults.Scheme)
    .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(
        SessionAuthenticationDefaults.Scheme, _ => { });

builder.Services.AddAuthorization();

// ---------------- MVC / Controllers ----------------
builder.Services.AddControllers();
builder.Services.AddProblemDetails();

// ---------------- Swagger ----------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "RESTful API for the RaceDay event management system: Authentication, User Profile, " +
                      "Events, Categories, Event Enrolments and Results. There are two roles, Organiser and Participant. " +
                      "To test protected endpoints, call POST /api/auth/login first - the server-side session cookie " +
                      "is then sent automatically by the browser on every request made from this page."
    });

    // Use the XML doc comments on the controllers/DTOs as endpoint descriptions.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});

// ---------------- CORS (open for the MVC front end in Part 3) ----------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// Create / update the database on start-up (Code-First). EF migrations are applied when they exist;
// otherwise the schema is created straight from the model. In Docker the SQL Server container can take a
// little while to accept connections, so the first attempts are retried.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RaceDayContext>();
    if (db.Database.IsRelational()) // skipped by the unit tests (in-memory provider)
    {
        const int maxAttempts = 10;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                if (db.Database.GetMigrations().Any())
                    db.Database.Migrate();
                else
                    db.Database.EnsureCreated();
                break;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                // A database from an earlier run (created without migrations) already has the tables, so the first
                // migration fails with "There is already an object named ...". For a development database the easy
                // fix is to start again from scratch. Switched on by ResetDatabaseOnSchemaConflict (Development only).
                var schemaConflict = ex.Message.Contains("already an object named", StringComparison.OrdinalIgnoreCase);
                if (schemaConflict && builder.Configuration.GetValue("ResetDatabaseOnSchemaConflict", false))
                {
                    app.Logger.LogWarning("The existing database does not match the migrations. Recreating it (development setting). {Message}", ex.Message);
                    db.Database.EnsureDeleted();
                    continue;
                }

                app.Logger.LogWarning("Database not ready yet (attempt {Attempt} of {Max}): {Message}", attempt, maxAttempts, ex.Message);
                Thread.Sleep(TimeSpan.FromSeconds(3));
            }
        }
    }
}

// Swagger stays enabled in every environment so the API can always be verified in a browser.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
    options.DocumentTitle = "RaceDay API";
});

app.UseExceptionHandler();
// Switched off in Docker (the container only listens on HTTP): set UseHttpsRedirection=false.
if (builder.Configuration.GetValue("UseHttpsRedirection", true))
{
    app.UseHttpsRedirection();
}
app.UseCors("Default");

app.UseSession();          // must come before authentication
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposes Program for WebApplicationFactory<Program> in the test project.
public partial class Program { }
