using System;
using System.Collections.Generic;

public enum ColorTier { Tier1 = 1, Tier2, Tier3, Tier4, Tier5, Tier6 }

[Serializable]
public class ColorRaritySetting
{
    public PieceType Color;
    public ColorTier Tier = ColorTier.Tier1;
}

// Immutable per-session rules shared by spawning and scoring.
public sealed class ColorRarityTable
{
    private readonly PieceType[] colors;
    private readonly Dictionary<PieceType, int> tiers = new Dictionary<PieceType, int>();
    private readonly int totalWeight;

    public ColorRarityTable(ColorRaritySetting[] settings = null)
    {
        colors = (PieceType[])Enum.GetValues(typeof(PieceType));
        foreach (var setting in settings ?? CreateDefaults())
        {
            if (setting == null || !Enum.IsDefined(typeof(PieceType), setting.Color) ||
                !Enum.IsDefined(typeof(ColorTier), setting.Tier))
                throw new ArgumentException("Every color rarity entry needs a valid color and tier.");
            if (tiers.ContainsKey(setting.Color))
                throw new ArgumentException($"Duplicate rarity entry for {setting.Color}.");
            tiers.Add(setting.Color, (int)setting.Tier);
            totalWeight += 7 - (int)setting.Tier;
        }
        if (tiers.Count != colors.Length)
            throw new ArgumentException("Color rarity must contain each piece color exactly once.");
    }

    public int ScoreMultiplier(PieceType color) => tiers[color];
    public int SpawnWeight(PieceType color) => 7 - tiers[color];

    public PieceType ChooseColor(Random random)
    {
        if (random == null) throw new ArgumentNullException(nameof(random));
        int roll = random.Next(totalWeight);
        foreach (var color in colors)
        {
            roll -= SpawnWeight(color);
            if (roll < 0) return color;
        }
        throw new InvalidOperationException("Invalid color weight total.");
    }

    public static ColorRaritySetting[] CreateDefaults() => new[]
    {
        new ColorRaritySetting { Color = PieceType.Yellow, Tier = ColorTier.Tier1 },
        new ColorRaritySetting { Color = PieceType.Green, Tier = ColorTier.Tier2 },
        new ColorRaritySetting { Color = PieceType.Blue, Tier = ColorTier.Tier3 },
        new ColorRaritySetting { Color = PieceType.Orange, Tier = ColorTier.Tier4 },
        new ColorRaritySetting { Color = PieceType.Purple, Tier = ColorTier.Tier5 },
        new ColorRaritySetting { Color = PieceType.Red, Tier = ColorTier.Tier6 }
    };
}
