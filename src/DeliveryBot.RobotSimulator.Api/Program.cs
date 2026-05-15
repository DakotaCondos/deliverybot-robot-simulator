using DeliveryBot.RobotSimulator.Core.Bots;
using DeliveryBot.RobotSimulator.Core.Orders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<BotFleet>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var fleet = app.Services.GetRequiredService<BotFleet>();
fleet.AddDefaultBot("bot-001");
fleet.AddDefaultBot("bot-002");
fleet.AddDefaultBot("bot-003");

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

app.MapPost("/orders/assignments", (
    OrderAssignment assignment,
    BotFleet botFleet) =>
{
    if (!botFleet.TryGetBot(assignment.BotId, out var bot))
    {
        return Results.NotFound(new
        {
            assignment.OrderId,
            assignment.BotId,
            result = "Rejected",
            reason = "BotNotFound"
        });
    }

    var result = bot.AssignOrder(assignment);

    return Results.Ok(result);
});

app.Run();