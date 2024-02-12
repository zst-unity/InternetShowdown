using System;
using System.Threading;
using System.Threading.Tasks;

public class MutationStats
{
    public readonly MutationStat speed = new();
    public readonly MutationStat bounce = new();
    public readonly MutationStat luck = new();
    public readonly MutationStat damage = new();

    public void Reset()
    {
        speed.Reset();
        bounce.Reset();
        luck.Reset();
        damage.Reset();
    }

    public float Mutate(float original, MutationStat mutation)
    {
        return original * (mutation.multiplied <= 0 ? 1 : mutation.multiplied) + mutation.added;
    }
}

public class MutationStat
{
    public float added;
    public float multiplied;

    public void Reset()
    {
        added = 0;
        multiplied = 0;
    }
}

public static class MutationJobs
{
    public static Mutation InspectorToMutation(MutationStats stats, InspectorMutation input)
    {
        return input.Type switch
        {
            MutationType.Speed => new SpeedMutation(stats, input.ChangeAs, input.Amount, input.Time),
            MutationType.Bounce => new BounceMutation(stats, input.ChangeAs, input.Amount, input.Time),
            MutationType.Luck => new LuckMutation(stats, input.ChangeAs, input.Amount, input.Time),
            MutationType.Damage => new DamageMutation(stats, input.ChangeAs, input.Amount, input.Time),
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

    protected MutationStats _stats;

    public Mutation(MutationStats stats, ChangeType change, float amount, float time)
    {
        ChangeAs = change;
        Amount = amount;
        Time = time;

        _stats = stats;
    }

    protected float _added;
    protected float _multiplied;

    public abstract MutationStat StatModifying { get; }

    public void Mutate()
    {
        int milliseconds = (int)TimeSpan.FromSeconds(Time).TotalMilliseconds;

        if (ChangeAs == ChangeType.Add) StatModifying.added += Amount;
        else if (ChangeAs == ChangeType.Multiply) StatModifying.multiplied += Amount;

        Task.Delay(milliseconds, Source.Token).ContinueWith(o =>
        {
            if (_isCanceled) return;
            if (ChangeAs == ChangeType.Add) StatModifying.added -= Amount;
            else if (ChangeAs == ChangeType.Multiply) StatModifying.multiplied -= Amount;
        });
    }

    public CancellationTokenSource Source { get; protected set; } = new();
    protected bool _isCanceled;

    public void CancelMutation()
    {
        _isCanceled = true;
        Source.Cancel();
    }
}

[Serializable]
public class SpeedMutation : Mutation
{
    public SpeedMutation(MutationStats stats, ChangeType change, float amount, float time) : base(stats, change, amount, time)
    {
    }

    public override MutationStat StatModifying => _stats.speed;
}

[Serializable]
public class BounceMutation : Mutation
{
    public BounceMutation(MutationStats stats, ChangeType change, float amount, float time) : base(stats, change, amount, time)
    {
    }

    public override MutationStat StatModifying => _stats.bounce;
}

[Serializable]
public class LuckMutation : Mutation
{
    public LuckMutation(MutationStats stats, ChangeType change, float amount, float time) : base(stats, change, amount, time)
    {
    }

    public override MutationStat StatModifying => _stats.luck;
}

[Serializable]
public class DamageMutation : Mutation
{
    public DamageMutation(MutationStats stats, ChangeType change, float amount, float time) : base(stats, change, amount, time)
    {
    }

    public override MutationStat StatModifying => _stats.damage;
}
