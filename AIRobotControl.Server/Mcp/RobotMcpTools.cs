using System.ComponentModel;
using ModelContextProtocol.Server;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using AIRobotControl.Server.Data;
using AIRobotControl.Server.Hubs;
using AIRobotControl.Server.Modules.RobotManagement.Domain;
using System.Collections.Concurrent;
using AIRobotControl.Server.AI.Services;

namespace AIRobotControl.Server.Mcp;

[McpServerToolType]
public static class RobotMcpTools
{
    private static readonly ConcurrentDictionary<int, ChatHistory> _chatHistories = new();
    private static readonly ConcurrentDictionary<int, List<RangeEvent>> _rangeEvents = new();
    private static IServiceProvider? _serviceProvider;

    public static void Initialize(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [McpServerTool, Description("Get overview of all personas in the system with their robots")]
    public static async Task<string> GetPersonaOverview()
    {
        if (_serviceProvider == null) return "Service not initialized";

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var personas = await dbContext.Set<Persona>()
            .Include(p => p.Robots)
                .ThenInclude(r => r.State)
            .Include(p => p.Robots)
                .ThenInclude(r => r.RobotGroup)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Instructions,
                p.Tags,
                RobotCount = p.Robots.Count,
                Robots = p.Robots.Select(r => new
                {
                    r.Id,
                    GroupName = r.RobotGroup != null ? r.RobotGroup.Name : "No Group",
                    State = r.State != null ? new
                    {
                        r.State.Health,
                        r.State.Energy,
                        r.State.MaxEnergy,
                        r.State.Happiness,
                        Position = new { r.State.X, r.State.Y, r.State.Z }
                    } : null
                })
            })
            .ToListAsync();

        return JsonSerializer.Serialize(personas, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Get detailed information about a specific persona by ID")]
    public static async Task<string> GetPersonaDetails(int personaId)
    {
        if (_serviceProvider == null) return "Service not initialized";

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var persona = await dbContext.Set<Persona>()
            .Include(p => p.Robots)
                .ThenInclude(r => r.State)
            .Include(p => p.Robots)
                .ThenInclude(r => r.RobotGroup)
            .Include(p => p.Robots)
                .ThenInclude(r => r.RobotPreset)
            .FirstOrDefaultAsync(p => p.Id == personaId);

        if (persona == null)
        {
            return JsonSerializer.Serialize(new { Error = $"Persona with ID {personaId} not found" });
        }

        var details = new
        {
            persona.Id,
            persona.Name,
            persona.Description,
            persona.Instructions,
            persona.Tags,
            persona.CreatedAt,
            persona.UpdatedAt,
            Robots = persona.Robots.Select(r => new
            {
                r.Id,
                r.Instructions,
                r.Length,
                Group = r.RobotGroup != null ? new { r.RobotGroup.Id, r.RobotGroup.Name } : null,
                Preset = r.RobotPreset != null ? new { r.RobotPreset.Id, r.RobotPreset.Name } : null,
                State = r.State != null ? new
                {
                    r.State.Health,
                    r.State.Energy,
                    r.State.MaxEnergy,
                    r.State.Happiness,
                    Position = new { r.State.X, r.State.Y, r.State.Z }
                } : null
            })
        };

        return JsonSerializer.Serialize(details, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Send a message from a persona to nearby robots (AI-enhanced if enabled)")]
    public static async Task<string> SendMessage(int personaId, string message)
    {
        if (_serviceProvider == null) return "Service not initialized";

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<RobotHub>>();

        var robot = await dbContext.Set<Robot>()
            .Include(r => r.State)
            .Include(r => r.Persona)
            .FirstOrDefaultAsync(r => r.PersonaId == personaId);

        if (robot == null || robot.State == null)
        {
            return JsonSerializer.Serialize(new { Error = $"No robot found for persona ID {personaId}" });
        }

        var nearbyRobots = await GetNearbyRobotIds(dbContext, robot);

        // Process message through AI if enabled
        var aiService = _serviceProvider?.GetService<IRobotAIService>();
        string finalMessage = message;
        
        if (aiService != null && aiService.IsEnabled && robot.Persona != null)
        {
            // Generate AI-enhanced response based on persona
            var context = new Dictionary<string, object>
            {
                ["nearbyRobots"] = nearbyRobots.Count,
                ["health"] = robot.State.Health,
                ["energy"] = robot.State.Energy,
                ["happiness"] = robot.State.Happiness
            };
            
            try
            {
                finalMessage = await aiService.GenerateRobotResponseAsync(
                    robot.Persona, robot, message, context);
            }
            catch
            {
                // Fall back to original message if AI fails
                finalMessage = message;
            }
        }
        
        // Store in chat history
        var chatHistory = _chatHistories.GetOrAdd(robot.Id, new ChatHistory());
        chatHistory.AddMessage(robot.Id, finalMessage);

        await hubContext.Clients.All.SendAsync("ReceiveMessage", new
        {
            FromRobotId = robot.Id,
            FromPersonaName = robot.Persona?.Name,
            Message = finalMessage,
            Timestamp = DateTime.UtcNow,
            VisibleToRobots = nearbyRobots
        });

        return JsonSerializer.Serialize(new
        {
            Success = true,
            RobotId = robot.Id,
            PersonaName = robot.Persona?.Name,
            Message = finalMessage,
            AIEnhanced = aiService != null && aiService.IsEnabled,
            NearbyRobotsCount = nearbyRobots.Count,
            NearbyRobots = nearbyRobots,
            Timestamp = DateTime.UtcNow
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Attack another robot with damage calculation")]
    public static async Task<string> AttackPersona(int attackerPersonaId, int targetPersonaId)
    {
        if (_serviceProvider == null) return "Service not initialized";

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<RobotHub>>();

        var attacker = await dbContext.Set<Robot>()
            .Include(r => r.State)
            .Include(r => r.Persona)
            .FirstOrDefaultAsync(r => r.PersonaId == attackerPersonaId);

        var target = await dbContext.Set<Robot>()
            .Include(r => r.State)
            .Include(r => r.Persona)
            .FirstOrDefaultAsync(r => r.PersonaId == targetPersonaId);

        if (attacker == null || target == null)
        {
            return JsonSerializer.Serialize(new { Error = "One or both robots not found" });
        }

        if (attacker.State == null || target.State == null)
        {
            return JsonSerializer.Serialize(new { Error = "Robot states not initialized" });
        }

        const float attackRange = 15f;
        const float damage = 10f;

        var distance = GetDistance(attacker.State, target.State);
        if (distance > attackRange)
        {
            return JsonSerializer.Serialize(new 
            { 
                Error = $"Target is too far away ({distance:F1} units, max range is {attackRange})",
                Distance = distance,
                MaxRange = attackRange
            });
        }

        var oldHealth = target.State.Health;
        target.State.Health = Math.Max(0, target.State.Health - damage);
        
        await dbContext.SaveChangesAsync();

        await hubContext.Clients.All.SendAsync("RobotAttack", attacker.Id, target.Id, damage);

        return JsonSerializer.Serialize(new
        {
            Success = true,
            AttackerRobot = new { Id = attacker.Id, PersonaName = attacker.Persona?.Name },
            TargetRobot = new { Id = target.Id, PersonaName = target.Persona?.Name },
            Damage = damage,
            PreviousHealth = oldHealth,
            NewHealth = target.State.Health,
            Distance = distance,
            Timestamp = DateTime.UtcNow
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Move a robot towards target coordinates")]
    public static async Task<string> WalkTowards(int personaId, float targetX, float targetY, float targetZ)
    {
        if (_serviceProvider == null) return "Service not initialized";

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<RobotHub>>();

        var robot = await dbContext.Set<Robot>()
            .Include(r => r.State)
            .Include(r => r.Persona)
            .FirstOrDefaultAsync(r => r.PersonaId == personaId);

        if (robot == null || robot.State == null)
        {
            return JsonSerializer.Serialize(new { Error = $"No robot found for persona ID {personaId}" });
        }

        const float moveSpeed = 2f;
        var oldPosition = new { X = robot.State.X, Y = robot.State.Y, Z = robot.State.Z };
        
        var direction = Normalize(targetX - robot.State.X, targetY - robot.State.Y, targetZ - robot.State.Z);

        robot.State.X += direction.x * moveSpeed;
        robot.State.Y += direction.y * moveSpeed;
        robot.State.Z += direction.z * moveSpeed;

        robot.State.Energy = Math.Max(0, robot.State.Energy - 0.5f);

        await dbContext.SaveChangesAsync();

        await hubContext.Clients.All.SendAsync("RobotPositionUpdated",
            robot.Id, robot.State.X, robot.State.Y, robot.State.Z);

        return JsonSerializer.Serialize(new
        {
            Success = true,
            RobotId = robot.Id,
            PersonaName = robot.Persona?.Name,
            OldPosition = oldPosition,
            NewPosition = new { X = robot.State.X, Y = robot.State.Y, Z = robot.State.Z },
            TargetPosition = new { X = targetX, Y = targetY, Z = targetZ },
            EnergyUsed = 0.5f,
            RemainingEnergy = robot.State.Energy,
            Timestamp = DateTime.UtcNow
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Update happiness level for a robot (0-100)")]
    public static async Task<string> UpdateHappiness(int personaId, float happiness)
    {
        if (_serviceProvider == null) return "Service not initialized";

        happiness = Math.Clamp(happiness, 0, 100);

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<RobotHub>>();

        var robot = await dbContext.Set<Robot>()
            .Include(r => r.State)
            .Include(r => r.Persona)
            .FirstOrDefaultAsync(r => r.PersonaId == personaId);

        if (robot == null || robot.State == null)
        {
            return JsonSerializer.Serialize(new { Error = $"No robot found for persona ID {personaId}" });
        }

        var oldHappiness = robot.State.Happiness;
        robot.State.Happiness = (int)happiness;
        
        await dbContext.SaveChangesAsync();

        await hubContext.Clients.All.SendAsync("RobotStateUpdated", robot.Id, new
        {
            robot.State.Health,
            robot.State.Energy,
            robot.State.MaxEnergy,
            robot.State.Happiness,
            robot.State.X,
            robot.State.Y,
            robot.State.Z
        });

        return JsonSerializer.Serialize(new
        {
            Success = true,
            RobotId = robot.Id,
            PersonaName = robot.Persona?.Name,
            OldHappiness = oldHappiness,
            NewHappiness = robot.State.Happiness,
            Timestamp = DateTime.UtcNow
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Get chat data for a persona including nearby robots and message history")]
    public static async Task<string> GetChatDataForPersona(int personaId)
    {
        if (_serviceProvider == null) return "Service not initialized";

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var robot = await dbContext.Set<Robot>()
            .Include(r => r.State)
            .Include(r => r.Persona)
            .Include(r => r.RobotGroup)
            .FirstOrDefaultAsync(r => r.PersonaId == personaId);

        if (robot == null)
        {
            return JsonSerializer.Serialize(new { Error = $"No robot found for persona ID {personaId}" });
        }

        var nearbyRobots = await GetNearbyRobots(dbContext, robot);
        var chatHistory = _chatHistories.GetOrAdd(robot.Id, new ChatHistory());
        var rangeEvents = GetRangeEvents(robot.Id);

        var result = new
        {
            Robot = new
            {
                robot.Id,
                Persona = robot.Persona?.Name,
                Group = robot.RobotGroup?.Name,
                State = robot.State != null ? new
                {
                    robot.State.Health,
                    robot.State.Energy,
                    robot.State.MaxEnergy,
                    robot.State.Happiness,
                    Position = new { robot.State.X, robot.State.Y, robot.State.Z }
                } : null
            },
            NearbyRobots = nearbyRobots,
            ChatHistory = chatHistory.Messages.TakeLast(10), // Last 10 messages
            RangeEvents = rangeEvents,
            Timestamp = DateTime.UtcNow
        };

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    // Helper methods
    private static async Task<List<object>> GetNearbyRobots(ApplicationDbContext dbContext, Robot robot)
    {
        if (robot.State == null) return new List<object>();

        var nearbyRobots = await dbContext.Set<Robot>()
            .Include(r => r.State)
            .Include(r => r.Persona)
            .Include(r => r.RobotGroup)
            .Where(r => r.Id != robot.Id && r.State != null)
            .ToListAsync();

        const float chatRange = 10f;
        return nearbyRobots
            .Where(r => GetDistance(robot.State, r.State!) <= chatRange)
            .Select(r => new
            {
                r.Id,
                Name = r.Persona?.Name,
                Group = r.RobotGroup?.Name,
                Distance = GetDistance(robot.State, r.State!),
                State = new
                {
                    r.State!.Health,
                    r.State.Energy,
                    r.State.Happiness,
                    Position = new { r.State.X, r.State.Y, r.State.Z }
                }
            })
            .Cast<object>()
            .ToList();
    }

    private static async Task<List<int>> GetNearbyRobotIds(ApplicationDbContext dbContext, Robot robot)
    {
        const float chatRange = 10f;

        var nearbyRobots = await dbContext.Set<Robot>()
            .Include(r => r.State)
            .Where(r => r.Id != robot.Id && r.State != null)
            .ToListAsync();

        return nearbyRobots
            .Where(r => GetDistance(robot.State!, r.State!) <= chatRange)
            .Select(r => r.Id)
            .ToList();
    }

    private static List<RangeEvent> GetRangeEvents(int robotId)
    {
        if (_rangeEvents.TryGetValue(robotId, out var events))
        {
            var recentEvents = events.Where(e => e.Timestamp > DateTime.UtcNow.AddMinutes(-5)).ToList();
            _rangeEvents[robotId] = recentEvents;
            return recentEvents;
        }
        return new List<RangeEvent>();
    }

    private static float GetDistance(RobotState state1, RobotState state2)
    {
        var dx = state1.X - state2.X;
        var dy = state1.Y - state2.Y;
        var dz = state1.Z - state2.Z;
        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    private static (float x, float y, float z) Normalize(float x, float y, float z)
    {
        var length = (float)Math.Sqrt(x * x + y * y + z * z);
        if (length > 0)
        {
            return (x / length, y / length, z / length);
        }
        return (0, 0, 0);
    }
}

public class ChatHistory
{
    public List<ChatHistoryMessage> Messages { get; } = new();

    public void AddMessage(int fromRobotId, string message)
    {
        Messages.Add(new ChatHistoryMessage
        {
            FromRobotId = fromRobotId,
            Message = message,
            Timestamp = DateTime.UtcNow
        });

        if (Messages.Count > 100)
        {
            Messages.RemoveAt(0);
        }
    }
}

public class ChatHistoryMessage
{
    public int FromRobotId { get; set; }
    public string Message { get; set; } = "";
    public DateTime Timestamp { get; set; }
}

public class RangeEvent
{
    public int RobotId { get; set; }
    public string EventType { get; set; } = "";
    public DateTime Timestamp { get; set; }
}