namespace AIRobotControl.Server.Modules.RobotManagement.Features.Personas.CreatePersona;

public class CreatePersonaRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Instructions { get; set; }
    public string? Tags { get; set; }
}