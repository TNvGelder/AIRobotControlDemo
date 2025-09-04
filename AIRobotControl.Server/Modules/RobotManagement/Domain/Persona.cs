namespace AIRobotControl.Server.Modules.RobotManagement.Domain;

public class Persona
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Instructions { get; set; }
    public string? Tags { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    
    // Navigation property
    public List<Robot> Robots { get; set; } = new();
}