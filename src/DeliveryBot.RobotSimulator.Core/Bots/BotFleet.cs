using DeliveryBot.RobotSimulator.Core.Simulation;
using DeliveryBot.RobotSimulator.Core.Stock;

namespace DeliveryBot.RobotSimulator.Core.Bots;

public sealed class BotFleet
{
    private readonly Dictionary<string, SimulatedBot> _bots = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<BotSnapshot> GetAll()
    {
        return _bots.Values
            .Select(bot => bot.ToSnapshot())
            .ToList();
    }

    public BotSnapshot? Get(string botId)
    {
        return _bots.TryGetValue(botId, out var bot)
            ? bot.ToSnapshot()
            : null;
    }

    public bool TryGetBot(string botId, out SimulatedBot bot)
    {
        return _bots.TryGetValue(botId, out bot!);
    }

    public SimulatedBot AddDefaultBot(string botId)
    {
        var bot = new SimulatedBot(
            botId,
            model: "DeliveryBot-V1",
            currentLocation: new GeoLocation(33.4255, -111.9400),
            stock:
            [
                new BotStockItem("water", "Water", 20),
                new BotStockItem("soda", "Soda", 20),
                new BotStockItem("chips", "Chips", 15),
                new BotStockItem("sandwich", "Sandwich", 10)
            ]);

        _bots.Add(bot.BotId, bot);

        return bot;
    }
}