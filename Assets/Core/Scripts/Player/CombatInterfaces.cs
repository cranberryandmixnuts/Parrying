using UnityEngine;

public struct ParryCandidate
{
    public IParryReactive attacker;
    public Vector2 hitPoint;
    public float sqrDistance;
    public int ImperfectParryDamage;
}

public struct DashCandidate
{
    public Vector2 point;
    public int frame;
}

public interface IParryReactive
{
    void OnPerfectParry(Vector2 hitPoint);
    void OnImperfectParry(Vector2 hitPoint);
    void OnCounterParry(Vector2 hitPoint);
}