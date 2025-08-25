using System;

namespace SamplePlugin.Core.Chat;

public record ChatMessageData
{
    public required string Message { get; init; }
    public required string Sender { get; init; }
    public DateTime Timestamp { get; init; }
}