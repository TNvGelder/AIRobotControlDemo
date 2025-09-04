namespace AIRobotControl.Server.Modules.RobotManagement.Domain;

public class RobotGroup
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Instructions { get; set; }
    public int? RobotKingId { get; set; }
    public int? GroupStrategistId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation properties
    public List<Robot> Robots { get; set; } = new();
}
