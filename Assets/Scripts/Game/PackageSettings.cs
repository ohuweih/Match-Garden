using System;
using System.Collections.Generic;
using UnityEngine;

public enum PackageSpawnMode { RandomChance, EveryPlayerMoves }
public enum PackageContent { Frost, FrostMachine, HorizontalLine, VerticalLine, Bomb, Target, ColorClear, Bird }

[Serializable]
public sealed class PackageContentOption
{
    public PackageContent Content;
    [Min(1)] public int Weight = 1;
}

[Serializable]
public sealed class PackageSettings
{
    public bool Enabled;
    public PackageSpawnMode SpawnMode = PackageSpawnMode.EveryPlayerMoves;
    [Tooltip("Used by Random Chance: one roll after each complete player action.")]
    [Range(0, 100)] public int ChancePercent = 20;
    [Tooltip("Used by Every Player Moves. Cascades and bonus moves do not advance this timer.")]
    [Min(1)] public int PlayerMovesPerSpawn = 3;
    [Tooltip("Skip spawning while this many unopened packages remain on the board.")]
    [Min(1)] public int MaxActivePackages = 3;
    [Tooltip("Contents are rolled when opened. Higher weight means more likely.")]
    public PackageContentOption[] Contents = CreateDefaultContents();
    [Min(1)] public int BirdTravelDistance = 8;
    [Min(1)] public int FrostMachineHits = 2;
    [Min(1)] public int FrostMachineMovesPerSpawn = 1;

    public static PackageContentOption[] CreateDefaultContents() => Array.ConvertAll(
        (PackageContent[])Enum.GetValues(typeof(PackageContent)), content => new PackageContentOption { Content = content });

    public PackageSettings Copy() => new PackageSettings
    {
        Enabled = Enabled, SpawnMode = SpawnMode, ChancePercent = ChancePercent,
        PlayerMovesPerSpawn = PlayerMovesPerSpawn, MaxActivePackages = MaxActivePackages,
        BirdTravelDistance = BirdTravelDistance, FrostMachineHits = FrostMachineHits,
        FrostMachineMovesPerSpawn = FrostMachineMovesPerSpawn,
        Contents = Contents == null ? null : Array.ConvertAll(Contents, item => item == null ? null
            : new PackageContentOption { Content = item.Content, Weight = item.Weight })
    };

    public List<string> Validate()
    {
        var errors = new List<string>();
        if (!Enabled) return errors;
        if (!Enum.IsDefined(typeof(PackageSpawnMode), SpawnMode)) errors.Add("Unknown package spawn mode.");
        if (ChancePercent < 0 || ChancePercent > 100) errors.Add("Package chance must be 0 to 100 percent.");
        if (PlayerMovesPerSpawn < 1 || MaxActivePackages < 1) errors.Add("Package interval and active limit must be positive.");
        if (BirdTravelDistance < 1 || FrostMachineHits < 1 || FrostMachineMovesPerSpawn < 1)
            errors.Add("Package bird and machine settings must be positive.");
        if (Contents == null || Contents.Length == 0) errors.Add("Packages need at least one content option.");
        else foreach (var option in Contents)
            if (option == null || !Enum.IsDefined(typeof(PackageContent), option.Content) || option.Weight < 1)
                errors.Add("Package contents need valid types and positive weights.");
        return errors;
    }
}
