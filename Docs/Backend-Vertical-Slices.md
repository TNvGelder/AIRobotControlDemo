# Backend vertical slices (concise details)

This expands on the main instruction file with brief, concrete patterns you can copy.

## Folder layout

- Module root: `AIRobotControl.Server/Modules/<Module>/`
- Feature area: `Features/<Area>/`
  - Per operation folder: `Features/<Area>/<Action>/`
    - Endpoint.cs — FastEndpoints endpoint
    - Request.cs — input DTO
    - Response.cs — output DTO (queries) or omitted (commands)
    - Validator.cs — FluentValidation validator for the Request
    - Handler.cs — application logic, implements `IHandler<TReq[, TRes]>` with a single `Handle()`
  - Shared cross-slice DTOs (optional): `Features/<Area>/Shared/`

Example (Personas):

- `Features/Personas/GetPersonaById/`
- `Features/Personas/GetAllPersonas/`
- `Features/Personas/CreatePersona/`
- `Features/Personas/Shared/` (e.g., `GetPersonaResponse`, `GetPersonasResponse`)

## Endpoint patterns

- Create (POST /api/personas)
  - In `Configure()`: `Post("/api/personas"); AllowAnonymous(); Validator<CreatePersonaValidator>();`
  - In `HandleAsync()`: call handler, then
    `await Send.CreatedAtAsync<GetPersonaByIdEndpoint>(new { Id = id }, null, cancellation: ct);`

- Query by id (GET /api/personas/{id})
  - `Endpoint<GetPersonaByIdRequest, GetPersonaResponse>`
  - `Get("/api/personas/{id}"); AllowAnonymous(); Validator<GetPersonaByIdValidator>();`
  - Return 404 via `ThrowError("Persona not found", 404);`

- Query all (GET /api/personas)
  - `EndpointWithoutRequest<GetPersonasResponse>`; call handler with `NoRequest.Instance`

## Handlers and DI

- Implement the generic interfaces with a single method:
  - `IHandler<TRequest, TResponse>`: `Task<TResponse> Handle(TRequest request, CancellationToken ct)`
  - `IHandler<TRequest>`: `Task Handle(TRequest request, CancellationToken ct)`
- `ServiceCollectionExtensions.AddHandlers()` registers all handler implementations automatically as scoped.
- Handlers focus on application logic and talk to `ApplicationDbContext`.

## DbContext and timestamps

- In-memory SQLite.
- Timestamps for `Persona` set in `SaveChanges(_Async)`.
- `Program.cs` ensures the in-memory database is created at startup.

## Validation

- Use FluentValidation; register via `Validator<TValidator>()` on the endpoint.
- Validation errors are returned as RFC 7807 ProblemDetails through FastEndpoints error pipeline.

## Tests

- Integration tests use `WebApplicationFactory<Program>` and keep a shared in-memory SQLite `SqliteConnection` open for the app lifetime.
- Test via HTTP and assert DB state with the factory-provided `ApplicationDbContext`.

## Adding a new slice (checklist)

- Create folder `Modules/<Module>/Features/<Area>/<Action>/`.
- Add Request, optional Response, Validator, Handler (implements `IHandler`), and Endpoint.
- Map the route in `Configure()` and attach the validator.
- If it writes, return appropriate status (201 with Location for create, 204 for update/delete).
- Write tests under `Server.Tests/Integration/...` first when feasible.

## Bootstrap & registration (at a glance)

- In `Program.cs`:
  - `builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseSqlite("DataSource=:memory:"));`
  - `builder.Services.AddFastEndpoints();`
  - `builder.Services.AddHandlers(); // auto-registers IHandler implementations`
  - `app.UseFastEndpoints(c => c.Errors.UseProblemDetails());`
  - `using var scope = app.Services.CreateScope(); scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();`
