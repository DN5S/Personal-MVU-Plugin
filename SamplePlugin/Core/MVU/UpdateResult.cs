using System.Collections.Generic;

namespace SamplePlugin.Core.MVU;

public record UpdateResult<TState>(TState NewState, IReadOnlyList<IEffect> Effects) 
    where TState : IState
{
    public static UpdateResult<TState> NoChange(TState state) => 
        new(state, new List<IEffect>());
    
    public static UpdateResult<TState> StateOnly(TState state) => 
        new(state, new List<IEffect>());
    
    public static UpdateResult<TState> WithEffects(TState state, params IEffect[] effects) => 
        new(state, effects);
}