using System;
using System.Collections.Generic;
using UnityEngine;

public static class RarityJobs
{
    public static readonly Dictionary<string, byte> Rarities = new()
    {
        { "Legendary", 7 },
        { "Epic", 25 },
        { "Unique", 65 },
        { "Rare", 105 },
        { "Quaint", 155 },
        { "Common", 255 },
    };

    public static Rarity RarityFromName(this string value) => (Rarity)Enum.Parse(typeof(Rarity), value, true);

    public static List<UsableItem> GetAllWithRarity(List<UsableItem> input, Rarity rarity)
    {
        List<UsableItem> output = new();
        foreach (UsableItem item in input)
        {
            if (item.ItemRarity == rarity) output.Add(item);
        }

        return output;
    }

    public static List<UsableItem> Sort(List<UsableItem> input)
    {
        List<UsableItem> output = input;
        output.Sort((first, second) => (byte)first.ItemRarity < (byte)second.ItemRarity ? -1 : 1);

        return output;
    }

    public static Rarity KeyValuePairToRarity(KeyValuePair<string, byte> input) => RarityFromName(input.Key);

    public static byte Select(byte modifier)
    {
        bool isPositive = modifier > 0;
        byte absoluteModifier = (byte)Math.Abs(modifier);

        if (isPositive)
            return (byte)UnityEngine.Random.Range(0, 255 - absoluteModifier);
        else
            return (byte)UnityEngine.Random.Range(absoluteModifier, 255);
    }
}
