using System.ComponentModel;
using ModelContextProtocol.Server;

namespace AIRobotControl.Server.Mcp;

[McpServerToolType]
public static class EchoTool
{
    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string Echo(string message) => $"hello {message}";
}

