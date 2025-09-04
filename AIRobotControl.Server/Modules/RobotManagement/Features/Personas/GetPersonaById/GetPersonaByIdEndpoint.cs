using FastEndpoints;
using AIRobotControl.Server.Shared.Abstractions;
using AIRobotControl.Server.Modules.RobotManagement.Features.Personas.Shared;

namespace AIRobotControl.Server.Modules.RobotManagement.Features.Personas.GetPersonaById;

public class GetPersonaByIdEndpoint : Endpoint<GetPersonaByIdRequest, GetPersonaResponse>
{
    private readonly IHandler<GetPersonaByIdRequest, GetPersonaResponse?> _handler;

    public GetPersonaByIdEndpoint(IHandler<GetPersonaByIdRequest, GetPersonaResponse?> handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Get("/api/personas/{id}");
        AllowAnonymous();
        Validator<GetPersonaByIdValidator>();
    }

    public override async Task HandleAsync(GetPersonaByIdRequest req, CancellationToken ct)
    {
        var persona = await _handler.Handle(req, ct);
        
        if (persona == null)
        {
            ThrowError("Persona not found", 404);
        }

        await Send.OkAsync(persona, ct);
    }
}
