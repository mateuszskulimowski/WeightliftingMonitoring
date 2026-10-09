# Weightlifting Monitoring — Architecture Document

## 1. System Overview

A single-service ASP.NET Core (net9.0) Web API that exposes weightlifting "plan" resources over HTTP and persists them in Firebase (Firestore), accessed via the Firebase Admin SDK with a service-account credential file.

```
[ Client / HTTP consumer ]
        │  JSON over HTTP (REST)
        ▼
┌─────────────────────────────────────────┐
│  WeightliftingMonitoring.Api (Kestrel)  │
│                                         │
│  Program.cs  (DI, config, pipeline)     │
│        │                                │
│  Controllers/PlansController            │
│        │                                │
│  Services/PlanService                   │
│        │                                │
│  Models/Plan, PlanDocument              │
└────────┬────────────────────────────────┘
         │ Firebase Admin SDK
         ▼
   [ Firebase / Firestore ]
```

Deployment unit: one process, one solution (`WeightliftingMonitoring.sln`) with one project.

## 2. Module Structure

| Module | Path | Responsibility |
|---|---|---|
| Host / Bootstrap | `Program.cs` | Minimal-hosting startup: configuration binding, Firebase app initialization, DI registration of `PlanService`, controller mapping, middleware pipeline. |
| API Layer | `Controllers/PlansController.cs` | HTTP surface for plan resources; request validation, status-code mapping, delegation to the service layer. |
| Service Layer | `Services/PlanService.cs` | Business logic and the only component that talks to Firestore; maps between API models and persistence documents. |
| Domain / Contracts | `Models/plan.cs` | `Plan` — the model exchanged with clients. |
| Persistence Model | `Models/planDocument.cs` | `PlanDocument` — Firestore-attributed document shape (collection/field mapping). |
| Configuration | `appsettings.json`, `appsettings.Development.json`, `Properties/launchSettings.json` | Environment settings (Firebase project id, credential path, logging, URLs/profiles). |
| Dev tooling | `weightlifting-monitoring-api.http` | Manual request collection for exercising endpoints. |

## 3. Data Flow

**Write path**
1. Client sends JSON to a `PlansController` action.
2. Model binding/validation produces a `Plan` instance.
3. Controller calls `PlanService`.
4. `PlanService` maps `Plan` → `PlanDocument` and writes to the Firestore collection via the Admin SDK.
5. Document id / result returned up the stack; controller emits `200/201` (or error status).

**Read path**
1. Client issues a GET (all plans or by id).
2. Controller delegates to `PlanService`.
3. `PlanService` queries Firestore, deserializes snapshots into `PlanDocument`, maps to `Plan`.
4. Controller serializes `Plan`(s) to JSON; missing document → `404`.

Key property: no direct Firestore access from controllers — all persistence is funnelled through `PlanService`.

## 4. API Layer

Attribute-routed MVC controller (`[ApiController]`), conventional REST shape under `/api/plans`:

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/plans` | List plans |
| GET | `/api/plans/{id}` | Fetch a single plan |
| POST | `/api/plans` | Create a plan |
| PUT | `/api/plans/{id}` | Update a plan |
| DELETE | `/api/plans/{id}` | Delete a plan |

Patterns: JSON request/response, `ActionResult<T>` return types for explicit status codes, async/await end-to-end against the Firestore SDK, `.http` file as the lightweight client contract/smoke-test artifact.

## 5. Data Model

Two representations of one concept:

- **`Plan`** (API/DTO) — the client-facing contract.
- **`PlanDocument`** (persistence) — Firestore-annotated (`[FirestoreData]` / `[FirestoreProperty]`), stored in a `plans` collection keyed by document id.

```
plans (Firestore collection)
└── {documentId} → PlanDocument
                    ├── Id / document key
                    ├── Name / title
                    ├── plan details (exercises, sets, reps, dates)
                    └── timestamps
```

Relationships are document-oriented: plan detail is embedded in the document rather than normalized across tables. No relational schema or migrations exist in the repository.

## 6. Infrastructure

- **Runtime:** .NET 9 (`net9.0`), ASP.NET Core, Kestrel; launch profiles in `launchSettings.json` (HTTP/HTTPS dev URLs).
- **Datastore:** Firebase / Cloud Firestore (managed, no local DB).
- **Dependencies:** `FirebaseAdmin` / `Google.Cloud.Firestore` NuGet packages declared in `WeightliftingMonitoring.Api.csproj`.
- **Credentials:** `firebase-adminsdk.json` service-account key, loaded at startup (observed in build output `weightlifting-monitoring-api/bin/Debug/net9.0/`). It is excluded from source control via `.gitignore` files at both repo and project level — the key must be supplied per environment.
- **Build/Deploy:** `dotnet build` / `dotnet publish` of a single project; deployable to any .NET 9 host (container, App Service, VM). No Dockerfile, CI pipeline, or IaC present.

## 7. Key Decisions

1. **Layered monolith (Controller → Service → Model).** Smallest viable structure that keeps HTTP concerns out of business logic; easy to grow by adding controllers/services rather than restructuring.
2. **Separate API model and Firestore document model.** `Plan` vs `PlanDocument` decouples the public contract from storage attributes, so Firestore schema changes don't leak into clients.
3. **Service as the persistence boundary (repository-ish).** `PlanService` is the single Firestore touchpoint, enabling mocking in tests and a future swap of datastore.
4. **Managed NoSQL (Firestore) over relational.** Plans are self-contained documents; avoids schema migrations and server/DB operations at the cost of ad-hoc query power and no enforced relational integrity.
5. **Constructor injection via built-in DI.** Firestore client / `PlanService` registered in `Program.cs`, keeping dependencies explicit and testable.
6. **Minimal hosting model + attribute routing.** Conventional, low-ceremony ASP.NET Core style appropriate for a single-resource API.
7. **Secrets out of VCS.** Credential JSON gitignored and resolved from configuration/path at runtime.

### Gaps / Recommended Next Steps
- No test project in the solution — add unit tests against `PlanService` with a mocked Firestore abstraction.
- No authentication/authorization layer; consider Firebase Authentication + JWT bearer validation.
- No containerization or CI/CD; add a Dockerfile and pipeline.
- Normalize file naming (`plan.cs`, `planDocument.cs` → `Plan.cs`, `PlanDocument.cs`) for consistency.
- Introduce centralized error handling (exception middleware / `ProblemDetails`) and request validation rules.