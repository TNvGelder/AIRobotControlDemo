using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace AIRobotControl.Server.Hubs;

public class RobotHub : Hub
{
    private static readonly ConcurrentDictionary<string, RobotConnection> _connections = new();
    private static readonly ConcurrentDictionary<int, List<string>> _robotConnections = new();
    private static readonly ConcurrentDictionary<string, ChatMessage> _recentMessages = new();
    private static readonly TimeSpan MessageRateLimit = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MessageTTL = TimeSpan.FromMinutes(5);

    public override async Task OnConnectedAsync()
    {
        var connection = new RobotConnection
        {
            ConnectionId = Context.ConnectionId,
            ConnectedAt = DateTime.UtcNow
        };
        _connections[Context.ConnectionId] = connection;
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_connections.TryRemove(Context.ConnectionId, out var connection))
        {
            if (connection.RobotId.HasValue)
            {
                await NotifyRobotDisconnected(connection.RobotId.Value);
                RemoveRobotConnection(connection.RobotId.Value, Context.ConnectionId);
            }
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task RegisterRobot(int robotId, string robotName)
    {
        if (_connections.TryGetValue(Context.ConnectionId, out var connection))
        {
            connection.RobotId = robotId;
            connection.RobotName = robotName;
            
            _robotConnections.AddOrUpdate(robotId,
                new List<string> { Context.ConnectionId },
                (key, list) => { list.Add(Context.ConnectionId); return list; });

            await Groups.AddToGroupAsync(Context.ConnectionId, $"robot_{robotId}");
            await Clients.All.SendAsync("RobotConnected", robotId, robotName);
        }
    }

    public async Task SendMessage(int fromRobotId, string message, List<int> visibleToRobots)
    {
        if (!await CheckRateLimit(fromRobotId))
        {
            await Clients.Caller.SendAsync("MessageRateLimited");
            return;
        }

        var chatMessage = new ChatMessage
        {
            FromRobotId = fromRobotId,
            Message = message,
            Timestamp = DateTime.UtcNow,
            VisibleToRobots = visibleToRobots
        };

        var messageId = Guid.NewGuid().ToString();
        _recentMessages[messageId] = chatMessage;

        foreach (var robotId in visibleToRobots)
        {
            await Clients.Group($"robot_{robotId}").SendAsync("ReceiveMessage", chatMessage);
        }

        CleanupOldMessages();
    }

    public async Task UpdateRobotPosition(int robotId, float x, float y, float z)
    {
        await Clients.Others.SendAsync("RobotPositionUpdated", robotId, x, y, z);
    }

    public async Task RobotEnteredChatRange(int robotId, int otherRobotId)
    {
        await Clients.Group($"robot_{robotId}").SendAsync("RobotEnteredRange", otherRobotId);
        await Clients.Group($"robot_{otherRobotId}").SendAsync("RobotEnteredRange", robotId);
    }

    public async Task RobotLeftChatRange(int robotId, int otherRobotId)
    {
        await Clients.Group($"robot_{robotId}").SendAsync("RobotLeftRange", otherRobotId);
        await Clients.Group($"robot_{otherRobotId}").SendAsync("RobotLeftRange", robotId);
    }

    public async Task AttackRobot(int attackerId, int targetId, float damage)
    {
        await Clients.All.SendAsync("RobotAttack", attackerId, targetId, damage);
    }

    public async Task UpdateRobotState(int robotId, RobotStateUpdate state)
    {
        await Clients.All.SendAsync("RobotStateUpdated", robotId, state);
    }

    public async Task BatteryCollected(int robotId, int batteryId)
    {
        await Clients.All.SendAsync("BatteryCollected", robotId, batteryId);
    }

    public async Task BatterySpawned(int batteryId, float x, float y, float z, float energy)
    {
        await Clients.All.SendAsync("BatterySpawned", batteryId, x, y, z, energy);
    }

    public async Task RobotSwitchedGroup(int robotId, int? newGroupId, int? oldGroupId)
    {
        await Clients.All.SendAsync("RobotGroupChanged", robotId, newGroupId, oldGroupId);
    }

    private Task<bool> CheckRateLimit(int robotId)
    {
        var now = DateTime.UtcNow;
        if (_connections.TryGetValue(Context.ConnectionId, out var connection))
        {
            if (connection.LastMessageTime.HasValue &&
                now - connection.LastMessageTime.Value < MessageRateLimit)
            {
                return Task.FromResult(false);
            }
            connection.LastMessageTime = now;
        }
        return Task.FromResult(true);
    }

    private void CleanupOldMessages()
    {
        var cutoff = DateTime.UtcNow - MessageTTL;
        var oldMessages = _recentMessages
            .Where(kvp => kvp.Value.Timestamp < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in oldMessages)
        {
            _recentMessages.TryRemove(key, out _);
        }
    }

    private void RemoveRobotConnection(int robotId, string connectionId)
    {
        if (_robotConnections.TryGetValue(robotId, out var connections))
        {
            connections.Remove(connectionId);
            if (connections.Count == 0)
            {
                _robotConnections.TryRemove(robotId, out _);
            }
        }
    }

    private async Task NotifyRobotDisconnected(int robotId)
    {
        await Clients.All.SendAsync("RobotDisconnected", robotId);
    }
}

public class RobotConnection
{
    public string ConnectionId { get; set; } = "";
    public int? RobotId { get; set; }
    public string? RobotName { get; set; }
    public DateTime ConnectedAt { get; set; }
    public DateTime? LastMessageTime { get; set; }
}

public class ChatMessage
{
    public int FromRobotId { get; set; }
    public string Message { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public List<int> VisibleToRobots { get; set; } = new();
}

public class RobotStateUpdate
{
    public float Health { get; set; }
    public float Energy { get; set; }
    public float MaxEnergy { get; set; }
    public float Happiness { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
}