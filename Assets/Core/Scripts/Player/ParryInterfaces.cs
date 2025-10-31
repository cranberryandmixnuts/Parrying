using UnityEngine;

public enum ProjectileHitResponse
{
    Consume,
    IgnoreContinue,
    NeutralizeContinue,
    ReflectToSource,
    ConsumedAlready
}

public interface IParryStack
{
    void AddOrRemove(int delta);
}