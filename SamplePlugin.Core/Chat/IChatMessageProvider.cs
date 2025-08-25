using System;

namespace SamplePlugin.Core.Chat;

public interface IChatMessageProvider
{
    event EventHandler<ChatMessageEventArgs>? OnChatMessageReceived;
    void Initialize();
    void Dispose();
}

public class ChatMessageEventArgs : EventArgs
{
    public required string Message { get; init; }
    public required string Sender { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;
}
