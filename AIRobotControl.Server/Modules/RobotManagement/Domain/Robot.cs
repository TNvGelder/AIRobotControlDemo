namespace AIRobotControl.Server.Modules.RobotManagement.Domain;

public class Robot
{
    public int Id { get; set; }
    public int RobotPresetId { get; set; }
    public int PersonaId { get; set; }
    public int? RobotGroupId { get; set; }
    public string? Instructions { get; set; }
    public float Length { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation
    public RobotPreset? RobotPreset { get; set; }
    public Persona? Persona { get; set; }
    public RobotGroup? RobotGroup { get; set; }
    public RobotState? State { get; set; }
}
