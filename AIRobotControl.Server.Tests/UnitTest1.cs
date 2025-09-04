using AIRobotControl.Server.Mcp;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace AIRobotControl.Server.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void EchoTool_ReturnsExpected()
        {
            var result = EchoTool.Echo("world");
            Assert.Equal("hello world", result);
        }

        [Fact]
        public void McpServices_RegisterEchoTool()
        {
            var services = new ServiceCollection();
            services.AddMcpServer()
                    .WithToolsFromAssembly(typeof(EchoTool).Assembly);

            using var provider = services.BuildServiceProvider();
            var tools = provider.GetServices<McpServerTool>();

            Assert.NotNull(tools);
            Assert.Contains(tools, t => t.ProtocolTool.Name == "Echo" || t.ProtocolTool.Name.Equals("echo", StringComparison.OrdinalIgnoreCase));
        }
    }
}
