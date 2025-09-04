using Microsoft.SemanticKernel;
using AIRobotControl.Server.Modules.RobotManagement.Domain;

namespace AIRobotControl.Server.AI.Services;

public interface IRobotAIService
{
    Task<string> GenerateRobotResponseAsync(
        Persona persona,
        Robot robot,
        string userInput,
        Dictionary<string, object>? context = null,
        CancellationToken cancellationToken = default);
    
    Task<string> ExecuteMcpActionAsync(
        Persona persona,
        Robot robot,
        string action,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default);
    
    bool IsEnabled { get; }
}