using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.Options;
using AIRobotControl.Server.Modules.RobotManagement.Domain;
using AIRobotControl.Server.AI.Configuration;
using System.Collections.Concurrent;

#pragma warning disable SKEXP0010 // Suppress experimental API warning

namespace AIRobotControl.Server.AI.Services;

public class RobotAIService : IRobotAIService
{
    private readonly ILogger<RobotAIService> _logger;
    private readonly AISettings _aiSettings;
    private readonly ConcurrentDictionary<int, Kernel> _robotKernels = new();
    private readonly IServiceProvider _serviceProvider;

    public bool IsEnabled => _aiSettings.Enabled;

    public RobotAIService(
        ILogger<RobotAIService> logger,
        IOptions<AISettings> aiSettings,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _aiSettings = aiSettings.Value;
        _serviceProvider = serviceProvider;
    }

    public async Task<string> GenerateRobotResponseAsync(
        Persona persona,
        Robot robot,
        string userInput,
        Dictionary<string, object>? context = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("GenerateRobotResponseAsync called. IsEnabled: {IsEnabled}, PersonaId: {PersonaId}, RobotId: {RobotId}, UserInput: {UserInput}", 
            IsEnabled, persona.Id, robot.Id, userInput);
            
        if (!IsEnabled)
        {
            _logger.LogWarning("AI service is disabled, returning default response");
            return "AI service is currently disabled.";
        }

        if (string.IsNullOrEmpty(_aiSettings.OpenRouterApiKey))
        {
            _logger.LogError("OpenRouter API key is not configured");
            return "AI service configuration error.";
        }

        try
        {
            _logger.LogDebug("Creating/getting kernel for robot {RobotId} with model {Model}", 
                robot.Id, _aiSettings.DefaultModel);
                
            var kernel = GetOrCreateKernelForRobot(robot);
            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            var chatHistory = new ChatHistory();
            
            // Add system message with persona instructions - simplified for cheaper models
            var systemPrompt = $"""
                You are {persona.Name}, a robot. {persona.Instructions}
                Keep responses under 50 words.
                """;
            
            _logger.LogDebug("System prompt: {SystemPrompt}", systemPrompt);
            chatHistory.AddSystemMessage(systemPrompt);

            // Add context if provided
            if (context != null && context.Any())
            {
                var contextString = string.Join("\n", context.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
                chatHistory.AddSystemMessage($"Current context:\n{contextString}");
                _logger.LogDebug("Added context: {Context}", contextString);
            }

            // Add user message
            chatHistory.AddUserMessage(userInput);
            _logger.LogDebug("User message added: {UserInput}", userInput);

            // Generate response
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                MaxTokens = _aiSettings.MaxTokens,
                Temperature = _aiSettings.Temperature,
                TopP = 0.9
            };
            
            _logger.LogDebug("Calling OpenRouter with settings: MaxTokens={MaxTokens}, Temperature={Temperature}, Model={Model}", 
                _aiSettings.MaxTokens, _aiSettings.Temperature, _aiSettings.DefaultModel);

            var response = await chatService.GetChatMessageContentAsync(
                chatHistory, 
                executionSettings: executionSettings, 
                cancellationToken: cancellationToken);

            var responseContent = response?.Content;
            _logger.LogInformation("Generated AI response for robot {RobotId} with persona {PersonaName}. Response: {Response}", 
                robot.Id, persona.Name, responseContent);

            if (string.IsNullOrEmpty(responseContent))
            {
                _logger.LogWarning("OpenRouter returned empty response for robot {RobotId}", robot.Id);
                return "I'm not sure how to respond to that.";
            }

            return responseContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI response for robot {RobotId}. Exception: {Message}", robot.Id, ex.Message);
            return "Sorry, I encountered an error while processing your request.";
        }
    }

    public async Task<string> ExecuteMcpActionAsync(
        Persona persona,
        Robot robot,
        string action,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            _logger.LogWarning("AI service is disabled, cannot execute MCP action");
            return "AI service is currently disabled.";
        }

        try
        {
            var kernel = GetOrCreateKernelForRobot(robot);
            
            // Create a prompt that will decide what MCP action to take
            var promptTemplate = """
                You are a robot with personality: {{$personaName}}
                You need to decide what action to take based on: {{$action}}
                
                Available parameters:
                {{$parameters}}
                
                Based on your personality and the current situation, describe what you would do.
                Keep your response under 50 words and in character.
                """;

            var promptFunction = kernel.CreateFunctionFromPrompt(promptTemplate);
            
            var arguments = new KernelArguments
            {
                ["personaName"] = persona.Name,
                ["action"] = action,
                ["parameters"] = string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}"))
            };

            var result = await kernel.InvokeAsync(promptFunction, arguments, cancellationToken);
            
            _logger.LogInformation("Executed MCP action {Action} for robot {RobotId}", action, robot.Id);
            
            return result.ToString() ?? "Action completed.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing MCP action {Action} for robot {RobotId}", action, robot.Id);
            return "Failed to execute the requested action.";
        }
    }

    private Kernel GetOrCreateKernelForRobot(Robot robot)
    {
        return _robotKernels.GetOrAdd(robot.Id, _ =>
        {
            _logger.LogDebug("Creating new kernel for robot {RobotId}", robot.Id);
            _logger.LogDebug("AI Settings - Model: {Model}, ApiKey: {HasApiKey}, MaxTokens: {MaxTokens}, Temperature: {Temperature}", 
                _aiSettings.DefaultModel, 
                !string.IsNullOrEmpty(_aiSettings.OpenRouterApiKey),
                _aiSettings.MaxTokens,
                _aiSettings.Temperature);
                
            var builder = Kernel.CreateBuilder();
            
            // Configure OpenRouter through OpenAI connector
            builder.AddOpenAIChatCompletion(
                modelId: _aiSettings.DefaultModel,
                apiKey: _aiSettings.OpenRouterApiKey,
                endpoint: new Uri("https://openrouter.ai/api/v1")
            );

            var kernel = builder.Build();
            
            _logger.LogInformation("Successfully created kernel for robot {RobotId} with model {Model} via OpenRouter", 
                robot.Id, _aiSettings.DefaultModel);
            
            return kernel;
        });
    }
}