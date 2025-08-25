namespace SamplePlugin.Core.Application;

public record ApplicationState
{
    public int MessageCount { get; init; }
    public string LastMessage { get; init; } = string.Empty;
    public string LastSender { get; init; } = string.Empty;
}