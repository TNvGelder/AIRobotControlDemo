using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using AIRobotControl.Server.Data;
using AIRobotControl.Server.Hubs;
using AIRobotControl.Server.Mcp;

namespace AIRobotControl.Server.Services;

public class BatteryService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<RobotHub> _hubContext;
    private readonly Random _random = new();
    private readonly TimeSpan _respawnInterval = TimeSpan.FromSeconds(30);

    public BatteryService(IServiceProvider serviceProvider, IHubContext<RobotHub> hubContext)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait a bit for the application to fully initialize
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Ensure database is created
                await dbContext.Database.EnsureCreatedAsync(stoppingToken);
                
                await ProcessBatteries(dbContext);
                await Task.Delay(_respawnInterval, stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Battery service error: {ex.Message}");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task ProcessBatteries(ApplicationDbContext dbContext)
    {
        var batteries = await dbContext.Batteries.ToListAsync();
        var now = DateTime.UtcNow;

        foreach (var battery in batteries)
        {
            if (battery.Energy <= 0 && now - battery.LastRespawnTime > _respawnInterval)
            {
                battery.Energy = _random.Next(25, 101);
                battery.X = _random.Next(-20, 21);
                battery.Y = 0;
                battery.Z = _random.Next(-20, 21);
                battery.LastRespawnTime = now;

                await _hubContext.Clients.All.SendAsync("BatterySpawned", 
                    battery.Id, battery.X, battery.Y, battery.Z, battery.Energy);
            }
        }

        await dbContext.SaveChangesAsync();
    }

    public static async Task<bool> TryCollectBattery(ApplicationDbContext dbContext, 
        IHubContext<RobotHub> hubContext, int robotId, float robotX, float robotY, float robotZ)
    {
        const float collectionRange = 2f;
        
        var nearbyBatteries = await dbContext.Batteries
            .Where(b => b.Energy > 0)
            .ToListAsync();

        var battery = nearbyBatteries.FirstOrDefault(b =>
        {
            var distance = Math.Sqrt(Math.Pow(b.X - robotX, 2) + Math.Pow(b.Y - robotY, 2) + Math.Pow(b.Z - robotZ, 2));
            return distance <= collectionRange;
        });

        if (battery != null)
        {
            var robot = await dbContext.Robots
                .Include(r => r.State)
                .FirstOrDefaultAsync(r => r.Id == robotId);

            if (robot?.State != null)
            {
                robot.State.Energy = Math.Min(robot.State.MaxEnergy, robot.State.Energy + battery.Energy);
                battery.Energy = 0;
                battery.LastRespawnTime = DateTime.UtcNow;

                await dbContext.SaveChangesAsync();
                await hubContext.Clients.All.SendAsync("BatteryCollected", robotId, battery.Id);
                return true;
            }
        }

        return false;
    }
}