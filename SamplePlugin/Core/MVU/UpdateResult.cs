using System.Collections.Generic;

namespace SamplePlugin.Core.MVU;

public record UpdateResult<TState>(TState NewState, IReadOnlyList<IEffect> Effects) 
    where TState : IState
{
    private static readonly IReadOnlyList<IEffect> EmptyEffects = new List<IEffect>(0).AsReadOnly();
    
    /// <summary>
    /// Returns a result with updated state but no side effects
    /// </summary>
    public static UpdateResult<TState> StateOnly(TState state) => 
        new(state, EmptyEffects);
    
    /// <summary>
    /// Returns a result with updated state and side effects
    /// </summary>
    public static UpdateResult<TState> WithEffects(TState state, params IEffect[] effects) => 
        new(state, effects);
    
    /// <summary>
    /// Alias for StateOnly - indicates no changes were made
    /// </summary>
    public static UpdateResult<TState> NoChange(TState state) => 
        StateOnly(state);
}