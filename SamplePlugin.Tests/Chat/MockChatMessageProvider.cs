using System;
using SamplePlugin.Core.Chat;

namespace SamplePlugin.Tests.Chat;

public class MockChatMessageProvider : IChatMessageProvider
{
    public event EventHandler<ChatMessageEventArgs>? OnChatMessageReceived;
    
    public void Initialize() { }
    
    public void Dispose() { }
    
    public void SimulateChatMessage(string message, string sender)
    {
        OnChatMessageReceived?.Invoke(this, new ChatMessageEventArgs
        {
            Message = message,
            Sender = sender,
            Timestamp = DateTime.Now
        });
    }
}