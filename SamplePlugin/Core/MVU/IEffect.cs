using System.Threading.Tasks;

namespace SamplePlugin.Core.MVU;

public interface IEffect
{
    string Type { get; }
}

public interface IEffectHandler<TEffect> where TEffect : IEffect
{
    Task HandleAsync(TEffect effect, IStore store);
}