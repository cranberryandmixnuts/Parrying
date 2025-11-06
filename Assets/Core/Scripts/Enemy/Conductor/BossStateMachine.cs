using UnityEngine;

public sealed class BossStateMachine
{
    private BossState current;

    public void ChangeState(BossState next)
    {
        if (current != null) current.Exit();
        current = next;
        if (current != null) current.Enter();
    }

    public void Tick()
    {
        if (current != null) current.Tick();
    }

    public void FixedTick()
    {
        if (current != null) current.FixedTick();
    }

    public BossState Current
    {
        get { return current; }
    }
}
