using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class MutationJobs
{
    public static Mutation InspectorToMutation(InspectorMutation input)
    {
        return input.Type switch
        {
            MutationType.Speed => new SpeedMutation(input.ChangeAs, input.Amount, input.Time),
            MutationType.Bounce => new BounceMutation(input.ChangeAs, input.Amount, input.Time),
            MutationType.Luck => new LuckMutation(input.ChangeAs, input.Amount, input.Time),
            MutationType.Damage => new DamageMutation(input.ChangeAs, input.Amount, input.Time),
            _ => throw new Exception("invalid mutation type"),
        };
    }
}

[Serializable]
public abstract class Mutation
{
    public ChangeType ChangeAs { get; protected set; }
    public float Amount { get; protected set; }
    public float Time { get; protected set; }

    public CancellationTokenSource Source { get; protected set; } = new();

    protected abstract void OnAdd();
    protected abstract void OnMultiply();

    protected abstract void OnDecrease();
    protected abstract void OnDivide();

    protected bool _isCanceled;
    protected float _changedStats;

    protected float MultiplyTool(float s) => (s * Amount) - s;

    public void Mutate()
    {
        int milliseconds = (int)TimeSpan.FromSeconds(Time).TotalMilliseconds;

        if (ChangeAs == ChangeType.Add) OnAdd();
        else if (ChangeAs == ChangeType.Multiply) OnMultiply();

        Task.Delay(milliseconds, Source.Token).ContinueWith(o =>
        {
            if (_isCanceled) return;
            if (ChangeAs == ChangeType.Add) OnDecrease();
            else if (ChangeAs == ChangeType.Multiply) OnDivide();
        });
    }

    public void CancelMutation()
    {
        _isCanceled = true;
        Source.Cancel();
    }

    public Mutation(ChangeType change, float amount, float time)
    {
        ChangeAs = change;
        Amount = amount;
        Time = time;
    }

    ~Mutation()
    {
        Debug.Log("GC: Mutation has been disposed!");
    }
}

[Serializable]
public class SpeedMutation : Mutation
{
    public SpeedMutation(ChangeType change, float amount, float time) : base(change, amount, time) { }

    protected override void OnAdd()
    {
        _changedStats = Amount;

        PlayerMutationStats.Singleton.Speed += _changedStats;
    }

    protected override void OnMultiply()
    {
        _changedStats = MultiplyTool(PlayerCurrentStats.Singleton.Speed);

        PlayerMutationStats.Singleton.Speed += _changedStats;
    }

    protected override void OnDecrease()
    {
        PlayerMutationStats.Singleton.Speed -= _changedStats;
    }

    protected override void OnDivide()
    {
        PlayerMutationStats.Singleton.Speed -= _changedStats;
    }
}

[Serializable]
public class BounceMutation : Mutation
{
    public BounceMutation(ChangeType change, float amount, float time) : base(change, amount, time) { }

    protected override void OnAdd()
    {
        _changedStats = Amount;

        PlayerMutationStats.Singleton.Bounce += _changedStats;
    }

    protected override void OnMultiply()
    {
        _changedStats = MultiplyTool(PlayerCurrentStats.Singleton.Bounce);

        PlayerMutationStats.Singleton.Bounce += _changedStats;
    }

    protected override void OnDecrease()
    {
        PlayerMutationStats.Singleton.Bounce -= _changedStats;
    }

    protected override void OnDivide()
    {
        PlayerMutationStats.Singleton.Bounce -= _changedStats;
    }
}

[Serializable]
public class LuckMutation : Mutation
{
    public LuckMutation(ChangeType change, float amount, float time) : base(change, amount, time) { }

    protected override void OnAdd()
    {
        _changedStats = Amount;

        PlayerMutationStats.Singleton.Luck += (byte)_changedStats;
    }

    protected override void OnMultiply()
    {
        _changedStats = MultiplyTool(PlayerCurrentStats.Singleton.Luck);

        PlayerMutationStats.Singleton.Luck += (byte)_changedStats;
    }

    protected override void OnDecrease()
    {
        PlayerMutationStats.Singleton.Luck -= (byte)_changedStats;
    }

    protected override void OnDivide()
    {
        PlayerMutationStats.Singleton.Luck -= (byte)_changedStats;
    }
}

[Serializable]
public class DamageMutation : Mutation
{
    public DamageMutation(ChangeType change, float amount, float time) : base(change, amount, time) { }

    protected override void OnAdd()
    {
        _changedStats = Amount;

        PlayerMutationStats.Singleton.Damage += _changedStats;
    }

    protected override void OnMultiply()
    {
        _changedStats = MultiplyTool(PlayerCurrentStats.Singleton.Damage);

        PlayerMutationStats.Singleton.Damage += _changedStats;
    }

    protected override void OnDecrease()
    {
        PlayerMutationStats.Singleton.Damage -= _changedStats;
    }

    protected override void OnDivide()
    {
        PlayerMutationStats.Singleton.Damage -= _changedStats;
    }
}
