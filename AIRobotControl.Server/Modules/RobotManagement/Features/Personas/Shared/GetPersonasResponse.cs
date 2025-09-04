namespace AIRobotControl.Server.Modules.RobotManagement.Features.Personas.Shared;

public class GetPersonasResponse
{
    public List<GetPersonaResponse> Personas { get; set; } = new();
}
