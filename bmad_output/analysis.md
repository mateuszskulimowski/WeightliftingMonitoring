# Project Analysis: WeightliftingMonitoring

## 1. Project Purpose

The project appears to be a backend service for tracking weightlifting training. Evidence:

- `WeightliftingMonitoring.Api` — an ASP.NET Core (net9.0) Web API project.
- `Controllers/PlansController.cs`, `Services/PlanService.cs`, `Models/plan.cs`, `Models/planDocument.cs` — a single domain concept: **training plans** (CRUD over plan entities).
- `bin/Debug/net9.0/firebase-adminsdk.json` — persistence and/or authentication is delegated to **Google Firebase** (most likely Firestore, given the `planDocument` model naming, which implies document-oriented storage).

Inferred purpose: expose HTTP endpoints that let a client (mobile/web app, not present in this repo) create and retrieve weightlifting plans stored in Firebase, enabling athletes or coaches to monitor training programs.

Current maturity: **early-stage prototype / proof of concept.** One domain entity, one controller, one service, no tests, no client application.

## 2. Codebase Health

**Positive indicators**
- Clear layered separation already in place: Controller → Service → Model.
- Separate DTO/persistence model split (`plan` vs `planDocument`) — a good instinct for isolating the Firestore document shape from the API contract.
- Environment-specific configuration (`appsettings.json` / `appsettings.Development.json`).
- `.http` request file present — supports manual endpoint verification.
- Solution file present, so the project is set up to grow into multiple projects.

**Concerns**
- **No test project of any kind.** Effective automated test coverage is 0%. There is no `*.Tests` project, no CI configuration, no test framework reference.
- **No CI/CD** — no GitHub Actions, Azure Pipelines, Dockerfile, or deployment manifest.
- **Inconsistent naming conventions.** `plan.cs` and `planDocument.cs` use camelCase filenames/types, violating .NET PascalCase conventions, while `PlansController` and `PlanService` follow them. This will compound as the model count grows.
- **Build artifacts are committed.** `weightlifting-monitoring-api/bin/Debug/net9.0/firebase-adminsdk.json` is inside a `bin/` output directory, meaning `.gitignore` coverage is incomplete or the file was force-added. The directory name also differs from the project directory (`WeightliftingMonitoring.Api`), suggesting a leftover from a renamed/abandoned project.
- **`WeightliftingMonitoring.sln.DotSettings.user`** is a per-developer JetBrains Rider file and should not be versioned.
- **No documentation.** No `README.md`, no architecture notes, no setup instructions, no license.
- No visible abstraction over data access (no repository interface or `IPlanService`), so the service is likely coupled directly to the Firebase SDK.

## 3. Key Risks

| Risk | Severity | Evidence / Rationale |
|---|---|---|
| **Committed Firebase service-account credentials** | **Critical** | `firebase-adminsdk.json` is the standard filename for a Google service-account private key with admin-level project access. Its presence in the repo (even under `bin/`) implies the secret may be in Git history. Must be treated as compromised. |
| Zero automated tests | High | No regression safety net; any refactor is manual-verification-only. |
| Single point of failure: Firebase | High | All persistence and likely auth depend on one external SaaS. No local/in-memory fallback, no repository abstraction to swap it out. |
| No onboarding documentation | Medium | A new developer cannot run the project without knowing how to obtain and place Firebase credentials. |
| Tight coupling to Firebase SDK in `PlanService` | Medium | Likely no interface boundary, making the service untestable without network access. |
| Naming/convention drift | Low–Medium | Mixed casing will spread as the model folder grows. |
| Committed build output | Low–Medium | Causes merge conflicts, repo bloat, and in this case secret leakage. |

## 4. Dependencies

Inferred from the file tree (the `.csproj` contents are the authoritative source and should be reviewed):

| Dependency | Role | Stability |
|---|---|---|
| **.NET 9.0 / ASP.NET Core** | Runtime and web framework | Stable, but .NET 9 is an **STS release** (18-month support). Plan a migration to .NET 10 LTS. |
| **Firebase Admin SDK / Google.Cloud.Firestore** | Data persistence, likely auth | Mature and well-maintained, but introduces vendor lock-in and a hard network dependency. |
| **Swagger/OpenAPI (likely)** | API exploration | Standard; note that .NET 9 templates ship `Microsoft.AspNetCore.OpenApi` rather than Swashbuckle. |

External surface is small — a positive. The main dependency risk is concentration (everything rides on Firebase) rather than breadth.

## 5. Module Map

| Path | Description |
|---|---|
| `WeightliftingMonitoring.sln` | Solution container; currently holds a single project. |
| `WeightliftingMonitoring.Api/` | The only application project — an ASP.NET Core Web API. |
| `WeightliftingMonitoring.Api/Program.cs` | Entry point; minimal-hosting startup, DI registration, middleware pipeline, and Firebase initialization. |
| `WeightliftingMonitoring.Api/Controllers/PlansController.cs` | HTTP endpoints for training plans. Sole public API surface. |
| `WeightliftingMonitoring.Api/Services/PlanService.cs` | Business logic and Firebase data access for plans. The functional core of the application. |
| `WeightliftingMonitoring.Api/Models/plan.cs` | API-facing plan model/DTO. |
| `WeightliftingMonitoring.Api/Models/planDocument.cs` | Firestore document representation of a plan. |
| `WeightliftingMonitoring.Api/appsettings*.json` | Configuration and environment overrides. |
| `WeightliftingMonitoring.Api/Properties/launchSettings.json` | Local debug profiles (ports, environment). |
| `WeightliftingMonitoring.Api/weightlifting-monitoring-api.http` | Manual HTTP request collection for smoke-testing endpoints. |
| `weightlifting-monitoring-api/bin/...` | **Stale committed build output containing credentials. Should be removed.** |

## 6. Change Hotspots

Ranked by expected change frequency:

1. **`Models/` directory** — Highest. A weightlifting domain needs Exercise, Set, Rep, Workout/Session, Athlete, Progress/Measurement, and Personal Record entities. Currently only `Plan` exists, so the model layer will expand substantially.
2. **`PlanService.cs`** — Very high. All new business rules (volume calculations, progression logic, plan validation) and query patterns land here. Already the most complex file by responsibility.
3. **`Controllers/`** — High. Each new entity brings a new controller; `PlansController` will also gain nested routes (e.g. plan → workouts → sets).
4. **`Program.cs`** — Moderate but frequent-churn. Every new service registration, auth setup, CORS policy for the eventual frontend, and validation/logging concern modifies this file. Risk of becoming a long, unstructured startup method.
5. **`appsettings.json`** — Moderate. Grows with each new integration and secret reference.
6. **Authentication/authorization** — Not yet present but inevitable. Plans are user-owned data, so an auth layer (likely Firebase Auth token validation) will cut across controllers and services.

## 7. Recommendations

**Immediate (security)**
1. **Rotate the Firebase service-account key now.** Assume `firebase-adminsdk.json` is compromised.
2. Remove the file from the working tree and purge it from Git history (`git filter-repo` or BFG), then force-push and notify collaborators.
3. Delete the stale `weightlifting-monitoring-api/` directory entirely.
4. Move credentials to User Secrets locally and environment variables / a secret manager in deployment; load via `GOOGLE_APPLICATION_CREDENTIALS` or configuration rather than a repo file.

**Short term (hygiene)**
5. Harden `.gitignore`: ensure `bin/`, `obj/`, `*.user`, `*.DotSettings.user`, and `*firebase-adminsdk*.json` are all excluded. Remove `WeightliftingMonitoring.sln.DotSettings.user` from version control.
6. Add a `README.md` covering: project purpose, prerequisites, how to obtain/configure Firebase credentials, how to run, and available endpoints.
7. Rename `plan.cs` → `Plan.cs` and `planDocument.cs` → `PlanDocument.cs` (types included) to align with .NET conventions before the model count grows.

**Medium term (quality & architecture)**
8. Add a `WeightliftingMonitoring.Api.Tests` project (xUnit + FluentAssertions). Start with `PlanService` unit tests and controller integration tests via `WebApplicationFactory`.
9. Introduce an `IPlanRepository` (or `IPlanStore`) abstraction over Firestore so the service is testable without network access and the datastore is replaceable.
10. Add `IPlanService` and register it in DI to decouple the controller from the concrete implementation.
11. Add a CI workflow (GitHub Actions): restore → build → test on every push/PR.
12. Introduce request validation (DataAnnotations or FluentValidation) and a consistent error-response contract / global exception handler.
13. Add structured logging and a `/health` endpoint ahead of any deployment.

**Longer term**
14. Plan the .NET 9 (STS) → .NET 10 (LTS) upgrade.
15. Design the authentication/authorization model before adding more entities, so ownership checks are consistent from the start.
16. If the domain grows as expected, split into `.Domain`, `.Application`, and `.Infrastructure` projects — the solution file is already structured to accommodate this.
17. Document the intended API contract (OpenAPI spec committed or published) for the future client application.