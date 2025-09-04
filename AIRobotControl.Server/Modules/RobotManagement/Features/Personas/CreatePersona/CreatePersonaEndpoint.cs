using FastEndpoints;
using AIRobotControl.Server.Modules.RobotManagement.Features.Personas.GetPersonaById;
using AIRobotControl.Server.Shared.Abstractions;

namespace AIRobotControl.Server.Modules.RobotManagement.Features.Personas.CreatePersona;

public class CreatePersonaEndpoint : Endpoint<CreatePersonaRequest>
{
    private readonly IHandler<CreatePersonaRequest, int> _handler;

    public CreatePersonaEndpoint(IHandler<CreatePersonaRequest, int> handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/personas");
        AllowAnonymous();
        Validator<CreatePersonaValidator>();
    }

    public override async Task HandleAsync(CreatePersonaRequest req, CancellationToken ct)
    {
    var personaId = await _handler.Handle(req, ct);
        
        await Send.CreatedAtAsync<GetPersonaByIdEndpoint>(new { Id = personaId }, null, cancellation: ct);
    }
}