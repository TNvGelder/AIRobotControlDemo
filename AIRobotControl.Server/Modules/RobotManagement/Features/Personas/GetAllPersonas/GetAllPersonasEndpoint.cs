using FastEndpoints;
using AIRobotControl.Server.Shared.Abstractions;
using AIRobotControl.Server.Modules.RobotManagement.Features.Personas.Shared;

namespace AIRobotControl.Server.Modules.RobotManagement.Features.Personas.GetAllPersonas;

public class GetAllPersonasEndpoint : EndpointWithoutRequest<GetPersonasResponse>
{
    private readonly IHandler<NoRequest, GetPersonasResponse> _handler;

    public GetAllPersonasEndpoint(IHandler<NoRequest, GetPersonasResponse> handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Get("/api/personas");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var personas = await _handler.Handle(NoRequest.Instance, ct);
        await Send.OkAsync(personas, ct);
    }
}
