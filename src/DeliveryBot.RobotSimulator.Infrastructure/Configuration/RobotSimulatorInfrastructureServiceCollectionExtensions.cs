using DeliveryBot.RobotSimulator.Infrastructure.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DeliveryBot.RobotSimulator.Infrastructure.Configuration;

public static class RobotSimulatorInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddRobotEventTransport(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection("EventTransport")
            .Get<EventTransportOptions>() ?? new EventTransportOptions();

        services.AddSingleton(options);
        services.AddSingleton<RecentRobotEventStore>();

        if (string.Equals(options.Mode, EventTransportModes.Local, StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IRobotEventPublisher, LocalRobotEventPublisher>();
            return services;
        }

        if (string.Equals(options.Mode, EventTransportModes.AzureEventHub, StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IRobotEventPublisher, AzureRobotEventPublisher>();
            return services;
        }

        throw new InvalidOperationException(
            $"Unsupported event transport mode '{options.Mode}'. Supported modes: {EventTransportModes.Local}, {EventTransportModes.AzureEventHub}.");
    }
}