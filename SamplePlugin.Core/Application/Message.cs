using SamplePlugin.Core.Chat;

namespace SamplePlugin.Core.Application;

public abstract record Message;

public record IncrementCounter : Message;

public record ChatMessageReceived(ChatMessageData Data) : Message;