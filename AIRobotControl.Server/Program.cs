using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using ModelContextProtocol.AspNetCore;
using AIRobotControl.Server.Mcp;
using AIRobotControl.Server.Hubs;
using AIRobotControl.Server.Services;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using AIRobotControl.Server.Data;
using AIRobotControl.Server.Shared.Extensions;
using AIRobotControl.Server.AI.Services;
using AIRobotControl.Server.AI.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for container environments
var isContainer = Environment.GetEnvironmentVariable("DEVCONTAINER") == "true" ||
                  Environment.GetEnvironmentVariable("CODESPACES") == "true" ||
                  Environment.GetEnvironmentVariable("REMOTE_CONTAINERS") == "true";

// Check if ASPNETCORE_URLS is already set to bind to 0.0.0.0
var aspnetcoreUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
var shouldConfigureForContainer = isContainer || 
    (aspnetcoreUrls != null && aspnetcoreUrls.Contains("0.0.0.0"));

if (shouldConfigureForContainer)
{
    // In containers, bind to all interfaces
    // This overrides any launchSettings.json applicationUrl
    builder.WebHost.UseUrls("http://0.0.0.0:7038");
    Console.WriteLine("Container environment detected - binding to http://0.0.0.0:7038");
}

// Add services to the container.

// Configure SQLite In-Memory Database with shared connection
var keepAliveConnection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
keepAliveConnection.Open();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(keepAliveConnection);
});

// Add FastEndpoints
builder.Services.AddFastEndpoints();

// Add SignalR
builder.Services.AddSignalR();

// Auto-register all handlers implementing IHandler
builder.Services.AddHandlers();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Background services
builder.Services.AddHostedService<BatteryService>();

// Configure AI settings
builder.Services.Configure<AISettings>(options =>
{
    var aiSection = builder.Configuration.GetSection("AI");
    aiSection.Bind(options);
    
    // Try to get API key from various sources
    options.OpenRouterApiKey = 
        builder.Configuration["OpenRouter:ApiKey"] ?? // User secrets or appsettings
        Environment.GetEnvironmentVariable("OpenRouter__ApiKey") ?? // Environment variable
        aiSection["OpenRouterApiKey"] ?? // From AI section in appsettings
        string.Empty;
});

// Register AI services
builder.Services.AddSingleton<IRobotAIService, RobotAIService>();

// MCP: register server with HTTP transport (implements SSE) and scan for tools in this assembly
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly(typeof(RobotMcpTools).Assembly);

var app = builder.Build();

// Ensure database is created for in-memory SQLite
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
    
    // Initialize MCP tools with service provider
    RobotMcpTools.Initialize(app.Services);
}

app.UseDefaultFiles();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Only use HTTPS redirection in non-container environments
if (!shouldConfigureForContainer)
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

// Map FastEndpoints
app.UseFastEndpoints(c =>
{
    c.Errors.UseProblemDetails();
});

app.MapControllers();

// MCP endpoint (Streamable HTTP transport)
app.MapMcp("/mcp");

// Map SignalR hubs
app.MapHub<RobotHub>("/robotHub");

app.MapFallbackToFile("/index.html");

app.Run();

public partial class Program { }
