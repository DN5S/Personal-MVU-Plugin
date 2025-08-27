namespace SamplePlugin.Modules.Chat.Models;

public record ChatMessageReceived(ChatMessage Message);
public record ChatFilterChanged(string Filter);
public record ChatChannelToggled(string Channel, bool IsEnabled);