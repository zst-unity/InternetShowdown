using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu()]
public class UsableItem : ScriptableObject
{
    [Header("Base Settings")]
    public GameObject ItemVisual;
    public Rarity ItemRarity = Rarity.Common;
    [Tooltip("health\nspeed_a speed_m\nbounce_a bounce_m\ndamage_a damage_m\nluck_a luck_m")] public string CanDropCondition;

    [Header("Use Settings")]
    [Tooltip("Should player hold to use the item?")] public bool HoldToUse = false;
    [ShowIf(nameof(HoldToUse)), AllowNesting(), Min(0), Tooltip("Hold to use time")] public float UseTime = 1;

    [Header("On Use")]
    [Tooltip("Player will get these mutations when item is used")] public List<InspectorMutation> Mutations = new();
    [Tooltip("These projectiles will spawn when item is used")] public List<ProjectileBase> Projectiles = new();
    [Tooltip("How much health would player gain when item is used")] public float HealAmount;
}

[Serializable]
public class InspectorMutation
{
    [Tooltip("What to mutate?")] public MutationType Type = MutationType.Speed;
    [Tooltip("How to mutate?")] public ChangeType ChangeAs = ChangeType.Add;
    [Tooltip("How many to mutate?")] public float Amount = 10;
    [Tooltip("Mutation duration")] public float Time = 5;
}

public enum Rarity
{
    Legendary,
    Epic,
    Unique,
    Rare,
    Quaint,
    Common
}

public enum MutationType
{
    Damage,
    Speed,
    Bounce,
    Luck
}

public enum ChangeType
{
    Add,
    Multiply
}
