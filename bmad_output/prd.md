# Product Requirements Document — Weightlifting Monitoring

## 1. Product Overview

Weightlifting Monitoring is a backend REST API service for creating, storing, and retrieving weightlifting training plans. It exposes HTTP endpoints that allow client applications (web or mobile) to manage structured training plan data persisted in a cloud document store (Firebase/Firestore, as evidenced by the `firebase-adminsdk.json` service-account credential file).

The current repository contains the API tier only (`WeightliftingMonitoring.Api`); no client application is present yet. The scope of this PRD therefore focuses on the API as the system of record and integration point for future front-end clients.

## 2. Core Features

| # | Feature | Evidence |
|---|---------|----------|
| F1 | **Training plan CRUD endpoints** — create, read, update, delete weightlifting plans over HTTP/JSON | `Controllers/PlansController.cs` |
| F2 | **Plan domain model** — a canonical representation of a training plan (name, exercises, sets/reps/load, scheduling) | `Models/plan.cs` |
| F3 | **Document persistence mapping** — a separate document-shaped model used for serialization to the document database, keeping the API contract decoupled from storage | `Models/planDocument.cs` |
| F4 | **Plan service layer** — business logic and data-access abstraction injected into controllers | `Services/PlanService.cs` |
| F5 | **Cloud document database integration** — Firebase Admin SDK credentials for Firestore access | `bin/.../firebase-adminsdk.json` |
| F6 | **Environment-based configuration** — separate development and default settings (connection/project IDs, logging levels) | `appsettings.json`, `appsettings.Development.json` |
| F7 | **API request catalogue / manual test harness** — ready-to-run HTTP requests for every endpoint | `weightlifting-monitoring-api.http` |
| F8 | **Local launch profiles** — HTTP/HTTPS profiles for local debugging | `Properties/launchSettings.json` |

### Out of scope (not present in the codebase)
- Authentication/authorization of end users
- Workout logging / actual lifted-weight tracking and progress analytics
- Front-end UI
- Automated test project

## 3. User Personas

**P1 — Athlete / Lifter**
Follows a prescribed weightlifting plan. Needs to retrieve their current plan and see prescribed exercises, sets, reps, and loads. Consumes the API via a future client app.

**P2 — Coach / Programmer**
Authors and maintains training plans for one or more athletes. Needs to create, edit, version, and delete plans. Primary writer of data via `POST`/`PUT`/`DELETE` on `/plans`.

**P3 — Client Application Developer**
Builds the web/mobile front end. Needs a stable, documented, JSON-based contract, predictable status codes, and local run instructions (`.http` file, launch profiles).

**P4 — Maintainer / Operator**
Runs and configures the service. Needs environment-specific configuration, secret management for Firebase credentials, and logs.

## 4. User Flows

**UF1 — Coach creates a plan**
1. Coach (via client) submits plan payload to `POST /api/plans`.
2. Controller validates the request and binds to the `Plan` model.
3. `PlanService` maps `Plan` → `PlanDocument` and writes it to Firestore.
4. API returns `201 Created` with the generated plan identifier.

**UF2 — Athlete views plans**
1. Client calls `GET /api/plans` (list) or `GET /api/plans/{id}` (detail).
2. `PlanService` queries Firestore, maps `PlanDocument` → `Plan`.
3. API returns `200 OK` with JSON, or `404 Not Found` for unknown ids.

**UF3 — Coach updates a plan**
1. Client calls `PUT /api/plans/{id}` with the modified plan.
2. Service resolves the existing document, applies changes, persists.
3. API returns `204 No Content` (or updated resource), `404` if missing.

**UF4 — Coach deletes a plan**
1. Client calls `DELETE /api/plans/{id}`.
2. Service removes the document from Firestore.
3. API returns `204 No Content`.

**UF5 — Developer explores the API locally**
1. Developer runs the project using a launch profile from `launchSettings.json`.
2. Developer opens `weightlifting-monitoring-api.http` in the IDE and executes sample requests against the local instance.

## 5. Non-Functional Requirements

**Performance**
- Single-plan read/write operations should complete in under 300 ms (p95) excluding network latency to Firestore.
- List endpoints must support pagination/limits to avoid unbounded document reads.

**Security**
- Firebase service-account credentials must never be committed to source control; they must be supplied via environment variables, user secrets, or a secret manager. The presence of `firebase-adminsdk.json` under `bin/` indicates a build-output copy — root `.gitignore` entries must cover it.
- All traffic served over HTTPS; HTTPS redirection enabled outside development.
- Input validation on all write endpoints; reject malformed/oversized payloads.
- Future requirement: per-user authentication (e.g., Firebase Auth bearer tokens) and ownership checks so a coach/athlete can only access their own plans.
- CORS policy restricted to known client origins.

**Scalability & Reliability**
- Stateless API — horizontally scalable behind a load balancer; no in-process session state.
- Firestore client registered as a singleton to reuse connections.
- Graceful error handling with consistent problem-details responses; no stack traces in non-development environments.

**Maintainability & Observability**
- Layered architecture (Controller → Service → Document model) must be preserved; no direct database access from controllers.
- Structured logging with configurable levels per environment (`appsettings*.json`).
- Health-check endpoint recommended for container orchestration.
- Recommended: add a unit/integration test project and OpenAPI/Swagger document generation.

## 6. Tech Stack

| Layer | Technology | Evidence |
|-------|-----------|----------|
| Language | C# | `.cs` files |
| Runtime / Framework | .NET 9 — ASP.NET Core Web API | `bin/Debug/net9.0/`, `Program.cs`, `.csproj` |
| API style | REST / JSON, attribute-routed controllers | `PlansController.cs` |
| Hosting pattern | Minimal hosting + DI container (`Program.cs`) | `Program.cs` |
| Persistence | Firebase / Google Cloud Firestore via Firebase Admin SDK | `firebase-adminsdk.json`, `PlanDocument` |
| Configuration | ASP.NET Core configuration + environment overrides | `appsettings.json`, `appsettings.Development.json` |
| Local tooling | `.http` request file, launch profiles, Visual Studio/Rider solution | `weightlifting-monitoring-api.http`, `launchSettings.json`, `.sln`, `.sln.DotSettings.user` |
| Source control | Git | `.gitignore` files |