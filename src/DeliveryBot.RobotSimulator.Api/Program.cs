using DeliveryBot.RobotSimulator.Api;
using DeliveryBot.RobotSimulator.Core.Bots;
using DeliveryBot.RobotSimulator.Core.Orders;
using DeliveryBot.RobotSimulator.Core.Simulation;
using DeliveryBot.RobotSimulator.Events;
using DeliveryBot.RobotSimulator.Infrastructure.Events;

var builder = WebApplication.CreateBuilder(args);

var simulatorOptions = builder.Configuration
    .GetSection("Simulator")
    .Get<SimulatorOptions>() ?? new SimulatorOptions();

var simulationOptions = builder.Configuration
    .GetSection("Simulation")
    .Get<SimulationOptions>() ?? new SimulationOptions();

builder.Services.AddSingleton(simulatorOptions);
builder.Services.AddSingleton(simulationOptions);

builder.Services.AddSingleton<BotFleet>();

builder.Services.AddSingleton<RecentRobotEventStore>();
builder.Services.AddSingleton<IRobotEventPublisher, LocalRobotEventPublisher>();

builder.Services.AddHostedService<SimulationWorker>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var fleet = app.Services.GetRequiredService<BotFleet>();
var startupOptions = app.Services.GetRequiredService<SimulatorOptions>();

fleet.InitializeDefaultFleet(startupOptions);

app.Logger.LogInformation(
    "Initialized robot simulator fleet. InitialBotCount={InitialBotCount} BotIdPrefix={BotIdPrefix} DefaultLocation=({Latitude}, {Longitude})",
    startupOptions.InitialBotCount,
    startupOptions.BotIdPrefix,
    startupOptions.DefaultLatitude,
    startupOptions.DefaultLongitude);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "robot-simulator"
}));

app.MapGet("/bots", (BotFleet botFleet) =>
{
    return Results.Ok(botFleet.GetAll());
});

app.MapGet("/bots/{botId}", (string botId, BotFleet botFleet) =>
{
    var bot = botFleet.Get(botId);

    return bot is null
        ? Results.NotFound(new { message = $"Bot {botId} was not found." })
        : Results.Ok(bot);
});

app.MapPost("/orders/assignments", async (
    OrderAssignment assignment,
    BotFleet botFleet,
    IRobotEventPublisher eventPublisher,
    CancellationToken cancellationToken) =>
{
    if (!botFleet.TryGetBot(assignment.BotId, out var bot))
    {
        var notFoundResponse = new
        {
            assignment.OrderId,
            assignment.BotId,
            Result = "Rejected",
            Reason = "BotNotFound"
        };

        var notFoundEvent = RobotEventEnvelope.Create(
            RobotEventTypes.RobotOrderAssignmentResponse,
            notFoundResponse,
            assignment.BotId);

        await eventPublisher.PublishAsync(
            notFoundEvent,
            cancellationToken);

        return Results.NotFound(notFoundResponse);
    }

    var result = bot.AssignOrder(assignment);

    var responseEvent = RobotEventEnvelope.Create(
        RobotEventTypes.RobotOrderAssignmentResponse,
        result,
        assignment.BotId);

    await eventPublisher.PublishAsync(
        responseEvent,
        cancellationToken);

    foreach (var generatedEvent in result.GeneratedEvents)
    {
        await eventPublisher.PublishAsync(
            generatedEvent,
            cancellationToken);
    }

    return Results.Ok(result);
});

app.MapGet("/events/recent", (
    RecentRobotEventStore eventStore,
    int count = 50) =>
{
    count = Math.Clamp(count, 1, 100);

    return Results.Ok(eventStore.GetRecent(count));
});

app.Run();