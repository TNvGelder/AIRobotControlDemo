---
applyTo: "**"
---
- Before adding any package either:
    - search for the latest version with dotnet nuget search SomeLib --take 1 or alternatively
    - or use dotnet add package command to install the package
  Do not add packages through modifying csproj directly, unless you have checked for the latest version following the above step.
- Make sure to follow the structure and keep thinking carefully about code quality, naming and placement of files when working. 
- Use Vertical Slice Architecture with FastEndPoints
  Do not use MediatR; keep handlers separate from the endpoints.
- For Db use an in memory sqlite for now.
  Use Request and Response models for the DTOs.
- FastEndpoints supports FluentValidation, use this validation.
- AIRobotControl.Server.Tests should be used for tests.
  Use Test Driven Development when practical: write a minimal failing test, implement, make it pass.
- Use EntityFamework in the backend.
- Use EFNamingcCnventions in the domain entities so we dont have to manually configure all the relationship and properties.
- Do not put multiple classes in one file, unless the extra classes are private classes.
  Do not put multiple public classes in one file (private helper classes inside are fine).

- Naming (Vertical Slice + FastEndpoints):
  - Use one of:
    - Prefixed per slice: CreatePersonaEndpoint/CreatePersonaRequest/CreatePersonaResponse
  - Use specific names for queries, e.g., GetPersonaByIdRequest/Response.

- DTO policy:
  - Not every slice needs a Response DTO.
  - Commands:
    - Create: return HTTP 201 Created with Location header; body optional (ID-only if returned).
    - Update/Delete: return 204 No Content unless a representation is explicitly needed.
  - Queries: return a Response DTO.
  - Errors: return RFC 7807 ProblemDetails.

- Validation:
  - Use FluentValidation with FastEndpoints. Validation failures should map to ProblemDetails.

- REST conventions:
  - Resource URIs: /api/personas, /api/personas/{id}
  - Methods: GET (list/get), POST (create), PUT/PATCH (update), DELETE (delete)
  - Status codes: 200/201/204, 400/404/409 as applicable, errors as ProblemDetails.

## Registration & setup (consistent pattern)

- Startup wiring (Program.cs):
  - Services: AddDbContext with in-memory SQLite, AddFastEndpoints, AddHandlers (auto-registers all classes implementing IHandler).
  - Pipeline: UseFastEndpoints with `c.Errors.UseProblemDetails()`; EnsureCreated() once at startup for the in-memory DB.
  - Example calls used today: `options.UseSqlite("DataSource=:memory:");` then `app.UseFastEndpoints(...)` and `db.Database.EnsureCreated()`.

- Slice conventions:
  - Endpoint derives from `Endpoint<TRequest, TResponse>` (queries) or `EndpointWithoutRequest<TResponse>` (list queries) or `Endpoint<TRequest>` (commands).
  - Endpoint Configure(): map the route (e.g., `Post("/api/personas")`, `Get("/api/personas/{id}")`), call `AllowAnonymous()` for now, and attach validator via `Validator<TValidator>()`.
  - Construction: endpoints inject exactly one handler (scoped) that implements `IHandler` and encapsulates the slice logic.
  - Results: POST returns `Send.CreatedAtAsync<GetPersonaByIdEndpoint>(new { Id = createdId }, null)`; GET returns `Send.OkAsync(result)`; missing resources `ThrowError("...", 404)`.

- Data & timestamps:
  - Use `ApplicationDbContext` for access; handler writes rely on `SaveChangesAsync(ct)` which sets `CreatedAt`/`UpdatedAt` for Persona entities.

- Testing:
  - Integration tests use `WebApplicationFactory<Program>` with a single open `SqliteConnection`; schema is created via `EnsureCreated()`.

For brief examples, see `Docs/Backend-Vertical-Slices.md`.


## Modules folder structure

Below is the current structure of `AIRobotControl.Server/Modules` discovered in the workspace:

The backend uses .NET 9

```
AIRobotControl.Server/Modules
└─ RobotManagement/
   ├─ Domain/
   └─ Features/
      ├─ Personas/
      │  ├─ CreatePersona/
      │  ├─ DeletePersona/
  │  ├─ GetAllPersonas/
  │  ├─ GetPersonaById/
      │  └─ UpdatePersona/
      └─ Robots/
```




## Test project structure
```
AIRobotControl.Server.Tests
├─ Unit/
│  └─ Modules/RobotManagement/Features/Personas/
├─ Integration/
│  └─ Modules/RobotManagement/Features/Personas/
│     ├─ CreatePersona.EndpointTests.cs
│     └─ ...
└─ Shared/
```

