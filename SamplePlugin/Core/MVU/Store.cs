using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace SamplePlugin.Core.MVU;

public delegate UpdateResult<TState> UpdateFunction<TState>(TState state, IAction action) 
    where TState : IState;

public delegate Task MiddlewareDelegate<TState>(TState state, IAction action, Func<Task> next) 
    where TState : IState;

public class Store<TState> : IStore<TState>, IDisposable where TState : IState
{
    private readonly UpdateFunction<TState> update;
    private readonly List<MiddlewareDelegate<TState>> middlewares = new();
    private readonly Dictionary<Type, object> effectHandlers = new();
    private readonly BehaviorSubject<TState> stateSubject;
    private readonly Subject<IAction> actionSubject = new();
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private TState currentState;
    private long version;
    
    public TState State => currentState;
    public IObservable<TState> StateChanged => stateSubject;
    public IObservable<IAction> ActionDispatched => actionSubject;
    
    public Store(TState initialState, UpdateFunction<TState> updateFunction)
    {
        currentState = initialState;
        update = updateFunction;
        stateSubject = new BehaviorSubject<TState>(initialState);
    }
    
    public void UseMiddleware(MiddlewareDelegate<TState> middleware)
    {
        middlewares.Add(middleware);
    }
    
    public void RegisterEffectHandler<TEffect>(IEffectHandler<TEffect> handler) 
        where TEffect : IEffect
    {
        effectHandlers[typeof(TEffect)] = handler;
    }
    
    public void Dispatch(IAction action)
    {
        Task.Run(() => DispatchAsync(action)).Wait();
    }
    
    public async Task DispatchAsync(IAction action)
    {
        await semaphore.WaitAsync();
        try
        {
            actionSubject.OnNext(action);
            
            var middlewarePipeline = BuildMiddlewarePipeline(action);
            await middlewarePipeline();
        }
        finally
        {
            semaphore.Release();
        }
    }
    
    private Func<Task> BuildMiddlewarePipeline(IAction action)
    {
        async Task CoreUpdate()
        {
            var result = update(currentState, action);
            
            if (!ReferenceEquals(result.NewState, currentState))
            {
                version++;
                currentState = SetVersion(result.NewState, version);
                stateSubject.OnNext(currentState);
            }
            
            foreach (var effect in result.Effects)
            {
                await HandleEffect(effect);
            }
        }
        
        return middlewares
            .Reverse<MiddlewareDelegate<TState>>()
            .Aggregate(
                (Func<Task>)CoreUpdate,
                (next, middleware) => () => middleware(currentState, action, next)
            );
    }
    
    private async Task HandleEffect(IEffect effect)
    {
        var effectType = effect.GetType();
        var handlerType = typeof(IEffectHandler<>).MakeGenericType(effectType);
        
        if (effectHandlers.TryGetValue(effectType, out var handler))
        {
            var handleMethod = handlerType.GetMethod("HandleAsync");
            if (handleMethod != null)
            {
                var task = handleMethod.Invoke(handler, new object[] { effect, this }) as Task;
                if (task != null)
                    await task;
            }
        }
    }
    
    private TState SetVersion(TState state, long newVersion)
    {
        // Use reflection to create a new instance with updated version
        var type = state.GetType();
        
        // For record types, we can use the copy constructor via reflection
        if (type.IsRecord())
        {
            var copyConstructor = type.GetConstructors()
                .FirstOrDefault(c => c.GetParameters().Length == 1 && 
                                     c.GetParameters()[0].ParameterType == type);
            
            if (copyConstructor != null)
            {
                var newState = (TState)copyConstructor.Invoke(new object[] { state });
                var versionProp = type.GetProperty("Version");
                if (versionProp != null && versionProp.CanWrite)
                {
                    versionProp.SetValue(newState, newVersion);
                }
                return newState;
            }
        }
        
        // Fallback: create new instance and copy all properties
        var instance = (TState)Activator.CreateInstance(type)!;
        foreach (var prop in type.GetProperties())
        {
            if (prop.CanRead && prop.CanWrite)
            {
                var value = prop.Name == "Version" ? newVersion : prop.GetValue(state);
                prop.SetValue(instance, value);
            }
        }
        return instance;
    }
    
    public void Dispose()
    {
        stateSubject.Dispose();
        actionSubject.Dispose();
        semaphore.Dispose();
        GC.SuppressFinalize(this);
    }
}
