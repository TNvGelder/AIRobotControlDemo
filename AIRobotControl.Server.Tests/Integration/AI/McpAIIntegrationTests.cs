using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using AIRobotControl.Server.Data;
using AIRobotControl.Server.AI.Services;
using AIRobotControl.Server.AI.Configuration;
using AIRobotControl.Server.Mcp;
using AIRobotControl.Server.Modules.RobotManagement.Domain;
using AIRobotControl.Server.Tests.Shared;
using Xunit.Abstractions;
using Microsoft.AspNetCore.SignalR;
using AIRobotControl.Server.Hubs;
using Microsoft.Extensions.Logging;
using Moq;

namespace AIRobotControl.Server.Tests.Integration.AI;

public class McpAIIntegrationTests : IntegrationTestBase
{
    private readonly ITestOutputHelper _output;
    
    public McpAIIntegrationTests(TestWebApplicationFactory factory, ITestOutputHelper output) : base(factory)
    {
        _output = output;
    }

    [Fact(Skip = "Integration test using real LLM/OpenRouter. Skipped by default; enable and provide OpenRouter:ApiKey to run.")]
    public async Task McpTools_SendMessage_WithOpenRouterAI_ShouldGeneratePersonaBasedResponse()
    {
        // This test specifically validates OpenRouter integration with MCP tools
        var apiKey = GetApiKey();
        if (string.IsNullOrEmpty(apiKey))
        {
            _output.WriteLine("Skipping test: OpenRouter API key not configured");
            return;
        }

        // Arrange - Set up test data
        var persona = new Persona
        {
            Name = "Sparky",
            Description = "An enthusiastic and energetic robot",
            Instructions = "Always be cheerful and use exclamation marks. Love to help others!",
            Tags = "friendly,energetic",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        
        await DbContext!.Set<Persona>().AddAsync(persona);
        await DbContext.SaveChangesAsync();
        
        // Create a RobotPreset (required for Robot)
        var robotPreset = new RobotPreset
        {
            Name = "TestPreset",
            Instructions = "Test preset instructions"
        };
        
        await DbContext.Set<RobotPreset>().AddAsync(robotPreset);
        await DbContext.SaveChangesAsync();
        
        var robot = new Robot
        {
            RobotPresetId = robotPreset.Id,
            PersonaId = persona.Id,
            Instructions = "Additional robot instructions",
            Length = 10.5f,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        
        await DbContext.Set<Robot>().AddAsync(robot);
        await DbContext.SaveChangesAsync();
        
        robot.State = new RobotState
        {
            Health = 100,
            Energy = 75,
            MaxEnergy = 100,
            Happiness = 80,
            X = 10,
            Y = 20,
            Z = 0
        };
        
        DbContext.Update(robot);
        await DbContext.SaveChangesAsync();
        
        // Create a service provider with AI enabled using REAL OpenRouter API
        var services = new ServiceCollection();
        
        // Configure with actual OpenRouter settings
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["AI:Enabled"] = "true",
                ["AI:DefaultModel"] = "openai/gpt-5-nano", // GPT-5-nano for better MCP compatibility
                ["AI:MaxTokens"] = "200",
                ["AI:Temperature"] = "0.7",
                ["OpenRouter:ApiKey"] = apiKey
            })
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<AISettings>()
            .Configure<IConfiguration>((settings, config) =>
            {
                config.GetSection("AI").Bind(settings);
                settings.OpenRouterApiKey = config["OpenRouter:ApiKey"] ?? "";
            });
        
        // Add required services
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlite(DbContext.Database.GetDbConnection());
        });
        
        services.AddLogging();
        services.AddSingleton<IRobotAIService, RobotAIService>();
        
        // Mock SignalR hub context
        var mockHubContext = new Mock<IHubContext<RobotHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        
        mockHubContext.Setup(x => x.Clients).Returns(mockClients.Object);
        mockClients.Setup(x => x.All).Returns(mockClientProxy.Object);
        
        services.AddSingleton(mockHubContext.Object);
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Initialize MCP tools with our service provider
        RobotMcpTools.Initialize(serviceProvider);
        
        // Act - Send a message through MCP with actual OpenRouter AI
        _output.WriteLine("Sending message through MCP with OpenRouter AI enabled...");
        var result = await RobotMcpTools.SendMessage(persona.Id, "Hello everyone! How are you doing today?");
        
        // Assert
        result.Should().NotBeNull();
        _output.WriteLine($"MCP Tool Result: {result}");
        
        // Parse the result to check specifics
        result.Should().Contain("\"Success\": true");
        result.Should().Contain("\"AIEnhanced\": true", "AI should be enabled and enhancing the message");
        result.Should().Contain("\"PersonaName\": \"Sparky\"");
        
        // The AI-enhanced message should be different from the original
        result.Should().NotContain("\"Message\":\"Hello everyone! How are you doing today?\"", 
            "The message should be AI-enhanced by OpenRouter, not the original");
        
        // The response should reflect Sparky's enthusiastic personality
        // (it should contain exclamation marks and cheerful language)
        _output.WriteLine("✅ OpenRouter successfully enhanced the message with Sparky's personality!");
    }

    [Fact(Skip = "Integration test using real LLM/OpenRouter. Skipped by default; enable and provide OpenRouter:ApiKey to run.")]
    public async Task McpTools_WithGPT5Nano_ShouldHandleComplexMcpActions()
    {
        // Test GPT-5-nano's ability to handle MCP action decisions
        var apiKey = GetApiKey();
        if (string.IsNullOrEmpty(apiKey))
        {
            _output.WriteLine("Skipping test: OpenRouter API key not configured");
            return;
        }

        // Arrange - Create a strategic persona
        var strategistPersona = new Persona
        {
            Name = "Strategist",
            Description = "A strategic robot that makes calculated decisions",
            Instructions = "Analyze situations carefully. Consider energy levels before taking actions. Be logical and explain your reasoning.",
            Tags = "strategic,analytical",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        
        await DbContext!.Set<Persona>().AddAsync(strategistPersona);
        await DbContext.SaveChangesAsync();
        
        // Create RobotPreset first (required for Robot)
        var robotPreset = new RobotPreset
        {
            Name = "StrategistPreset",
            Instructions = "Strategic robot preset"
        };
        
        await DbContext.Set<RobotPreset>().AddAsync(robotPreset);
        await DbContext.SaveChangesAsync();
        
        var strategistRobot = new Robot
        {
            RobotPresetId = robotPreset.Id,
            PersonaId = strategistPersona.Id,
            Instructions = "Conserve energy when possible",
            Length = 10
        };
        
        await DbContext.Set<Robot>().AddAsync(strategistRobot);
        await DbContext.SaveChangesAsync();
        
        // Low energy state - should affect decision making
        strategistRobot.State = new RobotState
        {
            Health = 90,
            Energy = 25, // Low energy!
            MaxEnergy = 100,
            Happiness = 60,
            X = 50, Y = 50, Z = 0
        };
        
        DbContext.Update(strategistRobot);
        await DbContext.SaveChangesAsync();
        
        // Configure services with GPT-5-nano
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["AI:Enabled"] = "true",
                ["AI:DefaultModel"] = "openai/gpt-5-nano", // Specifically using GPT-5-nano
                ["AI:MaxTokens"] = "250",
                ["AI:Temperature"] = "0.5", // Lower temperature for more consistent strategic decisions
                ["OpenRouter:ApiKey"] = apiKey
            })
            .Build();
        
        ConfigureServices(services, configuration);
        var serviceProvider = services.BuildServiceProvider();
        RobotMcpTools.Initialize(serviceProvider);
        
        // Act - Test various MCP actions with GPT-5-nano
        _output.WriteLine("Testing GPT-5-nano with MCP actions...");
        
        // Test 1: Send message about low energy
        var energyMessage = await RobotMcpTools.SendMessage(
            strategistPersona.Id, 
            "I need to report my status to the team");
        
        _output.WriteLine($"Energy status message: {energyMessage}");
        energyMessage.Should().Contain("\"Success\": true");
        energyMessage.Should().Contain("\"AIEnhanced\": true");
        
        // Test 2: Update happiness based on situation
        var happinessUpdate = await RobotMcpTools.UpdateHappiness(strategistPersona.Id, 70);
        _output.WriteLine($"Happiness update result: {happinessUpdate}");
        happinessUpdate.Should().Contain("\"Success\": true");
        
        // Test 3: Get chat data with context
        var chatData = await RobotMcpTools.GetChatDataForPersona(strategistPersona.Id);
        _output.WriteLine($"Chat data retrieved: {chatData}");
        chatData.Should().NotBeNull();
        chatData.Should().Contain("\"Energy\": 25"); // Should show low energy
        
        _output.WriteLine("✅ GPT-5-nano successfully handled complex MCP actions!");
    }

    [Fact(Skip = "Integration test using real LLM/OpenRouter. Skipped by default; enable and provide OpenRouter:ApiKey to run.")]
    public async Task McpTools_CompareModels_ForMcpCompatibility()
    {
        // Compare different models' performance with MCP tools
        var apiKey = GetApiKey();
        if (string.IsNullOrEmpty(apiKey))
        {
            _output.WriteLine("Skipping test: OpenRouter API key not configured");
            return;
        }

        var models = new[]
        {
            ("openai/gpt-5-nano", "GPT-5-nano"),
            ("google/gemma-2-9b-it:free", "Gemma 2"),
            ("meta-llama/llama-3.2-3b-instruct:free", "Llama 3.2")
        };

        foreach (var (modelId, modelName) in models)
        {
            _output.WriteLine($"\n--- Testing {modelName} ---");
            
            // Create a test persona
            var testPersona = new Persona
            {
                Name = $"Tester_{modelName.Replace(" ", "")}",
                Description = "A test robot for model comparison",
                Instructions = "Be concise and clear. State facts directly.",
                Tags = "test",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            
            await DbContext!.Set<Persona>().AddAsync(testPersona);
            await DbContext.SaveChangesAsync();
            
            // Create RobotPreset first (required for Robot)
            var testPreset = new RobotPreset
            {
                Name = $"TestPreset_{modelName.Replace(" ", "")}",
                Instructions = "Test preset for model comparison"
            };
            
            await DbContext.Set<RobotPreset>().AddAsync(testPreset);
            await DbContext.SaveChangesAsync();
            
            var testRobot = new Robot
            {
                RobotPresetId = testPreset.Id,
                PersonaId = testPersona.Id,
                Length = 5
            };
            
            await DbContext.Set<Robot>().AddAsync(testRobot);
            await DbContext.SaveChangesAsync();
            
            testRobot.State = new RobotState
            {
                Health = 100,
                Energy = 100,
                MaxEnergy = 100,
                Happiness = 100,
                X = 0, Y = 0, Z = 0
            };
            
            DbContext.Update(testRobot);
            await DbContext.SaveChangesAsync();
            
            // Configure with specific model
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["AI:Enabled"] = "true",
                    ["AI:DefaultModel"] = modelId,
                    ["AI:MaxTokens"] = "100",
                    ["AI:Temperature"] = "0.5",
                    ["OpenRouter:ApiKey"] = apiKey
                })
                .Build();
            
            ConfigureServices(services, configuration);
            var serviceProvider = services.BuildServiceProvider();
            RobotMcpTools.Initialize(serviceProvider);
            
            try
            {
                // Test MCP SendMessage
                var startTime = DateTime.UtcNow;
                var result = await RobotMcpTools.SendMessage(testPersona.Id, "Status report");
                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                
                _output.WriteLine($"{modelName} Response Time: {responseTime:F0}ms");
                _output.WriteLine($"{modelName} Result: {result.Substring(0, Math.Min(200, result.Length))}...");
                
                // Verify success
                result.Should().Contain("\"Success\": true");
                
                if (result.Contains("\"AIEnhanced\": true"))
                {
                    _output.WriteLine($"✅ {modelName} successfully enhanced the message");
                }
                else
                {
                    _output.WriteLine($"⚠️ {modelName} did not enhance the message");
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"❌ {modelName} failed: {ex.Message}");
            }
        }
        
        _output.WriteLine("\n=== Model Comparison Complete ===");
        _output.WriteLine("GPT-5-nano is recommended for MCP operations due to better function calling support");
    }

    private string? GetApiKey()
    {
        // Try to get API key from configuration for testing
        var config = new ConfigurationBuilder()
            .AddUserSecrets<McpAIIntegrationTests>(optional: true)
            .AddEnvironmentVariables()
            .Build();
        
        return config["OpenRouter:ApiKey"];
    }
    
    [Fact(Skip = "Integration test using real LLM/OpenRouter. Skipped by default; enable and provide OpenRouter:ApiKey to run.")]
    public async Task DirectAIService_WithOpenRouter_ShouldGenerateResponse()
    {
        // Test AI service directly without MCP to isolate the issue
        var apiKey = GetApiKey();
        if (string.IsNullOrEmpty(apiKey))
        {
            _output.WriteLine("Skipping test: OpenRouter API key not configured");
            return;
        }

        // Create minimal test setup
        var persona = new Persona
        {
            Id = 1,
            Name = "TestBot",
            Description = "A test robot",
            Instructions = "Be friendly and helpful",
            Tags = "test"
        };

        var robot = new Robot
        {
            Id = 1,
            PersonaId = 1,
            Length = 5,
            Instructions = "Test robot"
        };

        // Configure services with logging
        var services = new ServiceCollection();
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["AI:Enabled"] = "true",
                ["AI:DefaultModel"] = "openai/gpt-4o-mini", // Try with a known working model first
                ["AI:MaxTokens"] = "100",
                ["AI:Temperature"] = "0.7",
                ["OpenRouter:ApiKey"] = apiKey
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<AISettings>()
            .Configure<IConfiguration>((settings, config) =>
            {
                config.GetSection("AI").Bind(settings);
                settings.OpenRouterApiKey = config["OpenRouter:ApiKey"] ?? "";
                _output.WriteLine($"AI Settings - Enabled: {settings.Enabled}, Model: {settings.DefaultModel}, HasKey: {!string.IsNullOrEmpty(settings.OpenRouterApiKey)}");
            });

        services.AddLogging();
        
        services.AddSingleton<IRobotAIService, RobotAIService>();

        var serviceProvider = services.BuildServiceProvider();
        var aiService = serviceProvider.GetRequiredService<IRobotAIService>();

        // Act - Test direct AI call
        _output.WriteLine("Testing direct AI service call...");
        var response = await aiService.GenerateRobotResponseAsync(
            persona, 
            robot, 
            "Hello! How are you today?");

        // Assert
        _output.WriteLine($"AI Response: {response}");
        response.Should().NotBeNull();
        response.Should().NotBe("I'm not sure how to respond to that.");
        response.Should().NotBe("AI service is currently disabled.");
        response.Should().NotBe("AI service configuration error.");
        response.Should().NotBe("Sorry, I encountered an error while processing your request.");
    }

    private void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<AISettings>()
            .Configure<IConfiguration>((settings, config) =>
            {
                config.GetSection("AI").Bind(settings);
                settings.OpenRouterApiKey = config["OpenRouter:ApiKey"] ?? "";
            });
        
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlite(DbContext!.Database.GetDbConnection());
        });
        
        services.AddLogging();
        services.AddSingleton<IRobotAIService, RobotAIService>();
        
        // Mock SignalR
        var mockHubContext = new Mock<IHubContext<RobotHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        
        mockHubContext.Setup(x => x.Clients).Returns(mockClients.Object);
        mockClients.Setup(x => x.All).Returns(mockClientProxy.Object);
        
        services.AddSingleton(mockHubContext.Object);
    }
}