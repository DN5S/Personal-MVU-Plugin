namespace SamplePlugin.Core.MVU;

public interface IState
{
    string Id { get; init; }
    long Version { get; init; }
}