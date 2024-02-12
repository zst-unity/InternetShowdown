using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public static class RarityUtils
{
    public static readonly Dictionary<float, Rarity> Rarities = new()
    {
        { 5f, Rarity.Legendary },
        { 15f, Rarity.Epic },
        { 25f, Rarity.Unique },
        { 40f, Rarity.Rare },
        { 60f, Rarity.Quaint },
        { 100f, Rarity.Common },
    };

    public static List<UsableItem> SortByRarity(this List<UsableItem> input)
    {
        List<UsableItem> output = input;
        output.Sort((first, second) => (byte)first.ItemRarity < (byte)second.ItemRarity ? -1 : 1);

        return output;
    }

    public static Rarity PickRarity(float luck)
    {
        float absoluteLuck = Math.Clamp(Math.Abs(luck), -100f, 100f);

        float random;
        if (luck > 0)
            random = Random.Range(0, 100f - absoluteLuck);
        else
            random = Random.Range(absoluteLuck, 100f);

        foreach (var rarity in Rarities)
        {
            if (random <= rarity.Key) return rarity.Value;
        }

        return Rarity.Common;
    }
}
