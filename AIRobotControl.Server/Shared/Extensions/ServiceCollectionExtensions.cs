using AIRobotControl.Server.Shared.Abstractions;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace AIRobotControl.Server.Shared.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHandlers(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var types = assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract).ToList();

        foreach (var type in types)
        {
            var interfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType &&
                            (i.GetGenericTypeDefinition() == typeof(IHandler<>) ||
                             i.GetGenericTypeDefinition() == typeof(IHandler<,>)))
                .ToList();

            foreach (var itf in interfaces)
            {
                services.TryAddScoped(itf, type);
            }

            // Back-compat: also register legacy marker implementations (temporary)
            if (typeof(IHandler).IsAssignableFrom(type))
            {
                services.TryAddScoped(type);
            }
        }

        return services;
    }
}