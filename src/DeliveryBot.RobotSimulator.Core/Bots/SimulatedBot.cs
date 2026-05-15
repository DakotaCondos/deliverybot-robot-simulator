using DeliveryBot.RobotSimulator.Core.Orders;
using DeliveryBot.RobotSimulator.Core.Simulation;
using DeliveryBot.RobotSimulator.Core.Stock;

namespace DeliveryBot.RobotSimulator.Core.Bots;

public sealed class SimulatedBot
{
    private readonly Dictionary<string, BotStockItem> _stock;
    private readonly Queue<OrderAssignment> _queuedOrders = new();

    private OrderAssignment? _activeOrder;

    public string BotId { get; }
    public string Model { get; }
    public BotStatus Status { get; private set; }
    public GeoLocation CurrentLocation { get; private set; }
    public double PowerLevel { get; private set; }
    public double ExternalTemperature { get; private set; }
    public double InternalStorageTemperature { get; private set; }

    public SimulatedBot(
        string botId,
        string model,
        GeoLocation currentLocation,
        IEnumerable<BotStockItem> stock)
    {
        BotId = botId;
        Model = model;
        CurrentLocation = currentLocation;
        Status = BotStatus.Available;
        PowerLevel = 100;
        ExternalTemperature = 72;
        InternalStorageTemperature = 38;

        _stock = stock.ToDictionary(item => item.ItemId, StringComparer.OrdinalIgnoreCase);
    }

    public BotSnapshot ToSnapshot()
    {
        return new BotSnapshot(
            BotId,
            Model,
            Status,
            CurrentLocation,
            PowerLevel,
            ExternalTemperature,
            InternalStorageTemperature,
            _stock.Values.ToList(),
            _activeOrder?.OrderId,
            _queuedOrders.Count);
    }

    public BotOrderAssignmentResult AssignOrder(OrderAssignment assignment)
    {
        if (!string.Equals(assignment.BotId, BotId, StringComparison.OrdinalIgnoreCase))
        {
            return BotOrderAssignmentResult.Rejected(
                assignment.OrderId,
                BotId,
                "Order was assigned to a different bot.");
        }

        foreach (var item in assignment.Items)
        {
            if (!_stock.TryGetValue(item.ItemId, out var stockItem))
            {
                return BotOrderAssignmentResult.Rejected(
                    assignment.OrderId,
                    BotId,
                    $"Item {item.ItemId} is not stocked by this bot.");
            }

            if (!stockItem.CanReserve(item.Quantity))
            {
                return BotOrderAssignmentResult.Rejected(
                    assignment.OrderId,
                    BotId,
                    $"Insufficient available stock for item {item.ItemId}.");
            }
        }

        foreach (var item in assignment.Items)
        {
            _stock[item.ItemId].Reserve(item.Quantity);
        }

        if (Status == BotStatus.Available)
        {
            _activeOrder = assignment;
            Status = BotStatus.OnDelivery;

            return BotOrderAssignmentResult.Accepted(
                assignment.OrderId,
                BotId,
                "Order accepted and delivery started.");
        }

        _queuedOrders.Enqueue(assignment);

        return BotOrderAssignmentResult.Queued(
            assignment.OrderId,
            BotId,
            "Order accepted and queued.");
    }
}