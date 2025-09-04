#if false
namespace AIRobotControl.Server.Modules.RobotManagement.Features.Personas.GetPersona;

public class GetPersonaResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Instructions { get; set; } = "";
    public string? Tags { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
#endif