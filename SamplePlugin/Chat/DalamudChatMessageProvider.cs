using System;
using Dalamud.Plugin.Services;
using Dalamud.Game.Text;
using SamplePlugin.Core.Chat;

namespace SamplePlugin.Chat;

public class DalamudChatMessageProvider(IChatGui chatGui) : IChatMessageProvider
{
    public event EventHandler<ChatMessageEventArgs>? OnChatMessageReceived;
    
    public void Initialize()
    {
        chatGui.ChatMessage += OnChatMessage;
    }
    
    public void Dispose()
    {
        chatGui.ChatMessage -= OnChatMessage;
    }
    
    private void OnChatMessage(XivChatType type, int timestamp, ref Dalamud.Game.Text.SeStringHandling.SeString sender, 
                               ref Dalamud.Game.Text.SeStringHandling.SeString message, ref bool isHandled)
    {
        // Convert SeString to plain string for Core
        OnChatMessageReceived?.Invoke(this, new ChatMessageEventArgs
        {
            Message = message.TextValue,
            Sender = sender.TextValue,
            Timestamp = DateTime.Now
        });
    }
}
