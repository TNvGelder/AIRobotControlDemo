namespace AIRobotControl.Server.Modules.RobotManagement.Domain;

public class Battery
{
    public int Id { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Energy { get; set; }
    public DateTime LastRespawnTime { get; set; }
}