# RaceDay
# RaceDay

RaceDay is an event management system for South African road running, walking and cycling events.

- **Part 1** - planning: ERD, API endpoint plan and SQL script (in `/docs`)
- **Part 2** - the RESTful API (ASP.NET Core, Entity Framework Core, SQL Server, session authentication, Swagger, unit tests, CI)
- **Part 3** - the MVC web application (`RaceDay.Web`), Azure Blob Storage image uploads and Docker



## The two roles

| Role | What they can do |
|---|---|
| **Organiser** | Dashboard with their events, enrolments per event and upcoming dates. Create, edit and delete their own events (name, description, date, location, distance, type, banner image). Add age or distance categories. See who has enrolled (with category and status). Capture and publish finish times and positions. |
| **Participant** | Browse and filter upcoming events (public). See full event details and categories. Enrol in an event by choosing a category. See their enrolments with colour-coded status. See their race history, e.g. "47th out of 312 finishers". Update their details and profile picture. |

Visitors who are not logged in can browse events and event details. Everything else needs a login, and each role has its own menu and pages.

## How it fits together

```
Browser  ->  RaceDay.Web (MVC)  ->  RaceDay API  ->  SQL Server
                                        |
                                        +-------->  Azure Blob Storage (images)
```

- The MVC app has **no database code and no business rules**. Every action calls the API over HTTP (`Services/RaceDayApiClient.cs`).
- **Sessions:** on login the API starts a server-side session. The MVC app stores the user's ID, role and the API's session cookie in *its own* server-side session, and attaches that cookie to each API call. `[RequireLogin]` (`Filters/RequireLoginAttribute.cs`) redirects anonymous users to the login page and users with the wrong role to an access-denied page. Logging out clears both sessions and redirects to the login page.
- **Images:** the browser uploads a file to the MVC app, the MVC app forwards it to the API, and the API stores it in Azure Blob Storage and saves the blob URL in the database. The MVC app never talks to Azure. The container is private; the API adds a short-lived read-only link (SAS) to the stored URL when it returns it, so the images display in the browser.

## Run it locally

**Prerequisites:** .NET 10 SDK, SQL Server (LocalDB comes with Visual Studio), Visual Studio 2026 (optional).

1. **Database.** Set the connection string in `appsettings.json` (`ConnectionStrings:RaceDayDb`), for example
   `Server=(localdb)\\MSSQLLocalDB;Database=RaceDayDB;Trusted_Connection=True;TrustServerCertificate=True;`.
   In Development the API applies the Entity Framework migrations and adds demo data by itself the first time it starts.
2. **Azure Blob Storage (for image uploads).** See the next section. Without it everything works except uploads, which show a friendly message.
3. **Start the API.** `dotnet run --project RaceDay.Api.csproj --launch-profile http` (listens on http://localhost:5261, Swagger at `/swagger`).
4. **Start the MVC app.** `dotnet run --project RaceDay.Web --launch-profile http` and open http://localhost:5100.
   (In Visual Studio: right-click the solution > *Configure Startup Projects* > *Multiple startup projects* > start both `RaceDay.Api` and `RaceDay.Web`.)
   The MVC app finds the API through `ApiSettings:BaseUrl` in `RaceDay.Web/appsettings.json` (default `http://localhost:5261`, the API's HTTP address).

**Demo accounts** (created by the seeder, password `Passw0rd!123`):

| Role | Email |
|---|---|
| Organiser | `thandiwe.nkosi@raceday.co.za` |
| Participant | `lerato.mokoena@gmail.com` |

You can also register new accounts on the Register page.

## Azure Blob Storage setup

1. In the Azure portal create a **Storage account** (Standard, locally-redundant is fine).
2. Open **Security + networking > Access keys** and copy a **Connection string**.
3. Give it to the API without committing it to GitHub:
   ```
   dotnet user-secrets set "AzureBlob:ConnectionString" "<paste the connection string>" --project RaceDay.Api.csproj
   ```
   (or set the environment variable `AzureBlob__ConnectionString`; for Docker Compose put it in a `.env` file, see `.env.example`).
4. Done. The API creates the private container `raceday-images` on the first upload.

To develop without an Azure account you can use the **Azurite** emulator and the connection string `UseDevelopmentStorage=true`.

## Run with Docker

**The MVC application in a container (single command).** Start the API locally first (step 3 above), then:

```
docker build -t raceday-web ./RaceDay.Web
docker run --rm -p 8080:8080 -e ApiSettings__BaseUrl=http://host.docker.internal:5261 raceday-web
```

Open http://localhost:8080. The containerised app behaves exactly like the one running locally, because the only difference is where it finds the API (`ApiSettings__BaseUrl`).
On Linux add `--add-host=host.docker.internal:host-gateway` to the `docker run` command.

**The whole system (SQL Server + API + MVC) with one command:**

```
docker compose up --build
```

- MVC app: http://localhost:8080 &nbsp; - &nbsp; API Swagger: http://localhost:5261/swagger
- Stop any locally-running API first (it uses the same port).
- Optional: copy `.env.example` to `.env` and add your Azure connection string to enable image uploads.

The Dockerfile uses a multi-stage build (SDK image to build, smaller ASP.NET runtime image to run), restores packages in their own layer so rebuilds are quick, runs as a non-root user, and takes its settings from environment variables.


## Tests

```
dotnet test RaceDay.Api.slnx
```

The tests start the real API in memory with an in-memory database and a fake blob service, so they need neither SQL Server nor Azure. They cover registration and login, hashed passwords, sessions, role enforcement (401/403), ownership checks, events, categories, enrolments, results, the Part 3 image uploads (valid, invalid and unauthorised) and the new filters and dashboard data.

## CI/CD

`.github/workflows/ci.yml` runs on every push: it checks the repository structure, restores, builds the API, the MVC app and the tests, runs the tests, and builds the Docker image.

![Green CI build](docs/ci-green-build.png)
_(add your screenshot of the green check here)_

## What changed between the parts (and why)

**Part 1 -> Part 2:** Events gained `EventType` and `DistanceKm`; Enrolments gained `EventId`; authentication moved from tokens to server-side sessions (the brief requires sessions); a logout endpoint and extra 403/409 cases were added.

**Part 2 -> Part 3** (the ERD, SQL script and endpoint plan in `/docs` were updated to match):
- `ProfilePictureUrl` on Organisers and Participants and `BannerUrl` on Events store Azure Blob URLs (migration `AddImageUrls`).
- New endpoints: `POST /api/events/{id}/banner`, `POST /api/users/me/picture`, `GET /api/organisers/me/events` (dashboard data), `GET /api/events/{id}/results` (results page).
- `GET /api/events` accepts optional `type`, `search` and `upcomingOnly` filters (home page filtering).
- Enrolment responses include `hasResult` (to show "Completed") and result responses include `totalFinishers` (to show "47th out of 312").

