namespace DeliveryBot.RobotSimulator.Core.Bots;

public sealed record BotOrderAssignmentResult(
    string OrderId,
    string BotId,
    string Result,
    string Message)
{
    public static BotOrderAssignmentResult Accepted(
        string orderId,
        string botId,
        string message)
    {
        return new BotOrderAssignmentResult(orderId, botId, "Accepted", message);
    }

    public static BotOrderAssignmentResult Queued(
        string orderId,
        string botId,
        string message)
    {
        return new BotOrderAssignmentResult(orderId, botId, "Queued", message);
    }

    public static BotOrderAssignmentResult Rejected(
        string orderId,
        string botId,
        string message)
    {
        return new BotOrderAssignmentResult(orderId, botId, "Rejected", message);
    }
}