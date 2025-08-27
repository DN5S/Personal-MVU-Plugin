using System;
using System.Threading.Tasks;

namespace SamplePlugin.Core.MVU;

public interface IStore
{
    void Dispatch(IAction action);
    Task DispatchAsync(IAction action);
}

public interface IStore<TState> : IStore where TState : IState
{
    TState State { get; }
    IObservable<TState> StateChanged { get; }
    IObservable<IAction> ActionDispatched { get; }
}