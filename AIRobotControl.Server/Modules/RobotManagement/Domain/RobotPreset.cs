namespace AIRobotControl.Server.Modules.RobotManagement.Domain;

public class RobotPreset
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Instructions { get; set; }
    public string? Tags { get; set; }
    public float MeshScale { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
