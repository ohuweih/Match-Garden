using System;
using UnityEngine;

[Serializable]
public sealed class CascadeBonusSettings
{
    public bool Enabled = true;
    [Min(1)] public int TriggerCascades = 10;
    [Min(1)] public int BonusMoves = 5;

    public CascadeBonusSettings Copy() => new CascadeBonusSettings
    {
        Enabled = Enabled, TriggerCascades = TriggerCascades, BonusMoves = BonusMoves
    };
}

// Serializable authoring data. Each game receives a deep copy, never the asset's arrays.
[Serializable]
public sealed class LevelSettings
{
    [Header("Board")]
    [Min(1)] public int Width = 8;
    [Min(1)] public int Height = 8;
    public BoardShape Shape = BoardShape.Rectangle;
    public RefillMode RefillMode = RefillMode.Normal;
    [Tooltip("Extra dead cells. Coordinates start at the bottom-left (0, 0).")]
    public Vector2Int[] BlockedCells = new Vector2Int[0];
    [Tooltip("Overrides Shape and Blocked Cells at these coordinates.")]
    public Vector2Int[] LockedCells = new Vector2Int[0];
    [Min(1)] public int MatchesToUnlock = 2;

    [Header("Packages")]
    public PackageSettings Packages = new PackageSettings();

    [Header("Generators")]
    public GeneratorPlacement[] Generators = new GeneratorPlacement[0];

    [Header("Frost")]
    [Tooltip("Stationary one- or two-layer terrain on playable cells.")]
    public FrostPlacement[] FrostCells = new FrostPlacement[0];

    [Header("Colors and score")]
    public ColorRaritySetting[] ColorRarity = ColorRarityTable.CreateDefaults();
    [Min(0)] public int PointsPerPiece = 10;
    public bool AwardOpeningPoints = true;

    [Header("Move budget")]
    [Tooltip("Complete all objectives before the move budget runs out. Disable for unlimited play.")]
    public bool UseMoveLimit = false;
    [Min(1)] public int MoveLimit = 30;

    [Header("Objectives")]
    public bool CountOpeningCascadesForObjectives = true;
    public ObjectiveDefinition[] Objectives = new[]
    {
        new ObjectiveDefinition { Type = ObjectiveType.Score, Target = 500 },
        new ObjectiveDefinition { Type = ObjectiveType.ClearColor, Color = PieceType.Red, Target = 20 }
    };

    [Header("Birds")]
    public Vector2Int[] MovableObjectiveCells = new Vector2Int[0];
    public BirdRescueSettings DefaultBirdRescue = new BirdRescueSettings();
    public BirdRescueOverride[] BirdRescueOverrides = new BirdRescueOverride[0];

    [Header("Hot Streak")]
    public CascadeBonusSettings CascadeBonus = new CascadeBonusSettings();

    public LevelSettings Copy()
    {
        return new LevelSettings
        {
            Width = Width, Height = Height, Shape = Shape, RefillMode = RefillMode,
            BlockedCells = CopyArray(BlockedCells), LockedCells = CopyArray(LockedCells),
            MatchesToUnlock = MatchesToUnlock, PointsPerPiece = PointsPerPiece,
            Packages = Packages?.Copy(),
            Generators = Generators == null ? null : Array.ConvertAll(Generators, item => item?.Copy()),
            FrostCells = FrostCells == null ? null : Array.ConvertAll(FrostCells,
                item => item == null ? null : new FrostPlacement { Cell = item.Cell, Layers = item.Layers }),
            AwardOpeningPoints = AwardOpeningPoints,
            UseMoveLimit = UseMoveLimit, MoveLimit = MoveLimit,
            CountOpeningCascadesForObjectives = CountOpeningCascadesForObjectives,
            ColorRarity = ColorRarity == null ? null : Array.ConvertAll(ColorRarity,
                item => item == null ? null : new ColorRaritySetting { Color = item.Color, Tier = item.Tier }),
            Objectives = Objectives == null ? null : Array.ConvertAll(Objectives,
                item => item == null ? null : new ObjectiveDefinition { Type = item.Type, Color = item.Color, Target = item.Target }),
            MovableObjectiveCells = CopyArray(MovableObjectiveCells),
            DefaultBirdRescue = CopyRescue(DefaultBirdRescue),
            BirdRescueOverrides = BirdRescueOverrides == null ? null : Array.ConvertAll(BirdRescueOverrides,
                item => item == null ? null : new BirdRescueOverride { StartingCell = item.StartingCell, Rescue = CopyRescue(item.Rescue) }),
            CascadeBonus = CascadeBonus?.Copy()
        };
    }

    private static Vector2Int[] CopyArray(Vector2Int[] source) => source == null ? null : (Vector2Int[])source.Clone();
    private static BirdRescueSettings CopyRescue(BirdRescueSettings source) => source == null ? null : new BirdRescueSettings
    {
        Condition = source.Condition, RequiredDistance = source.RequiredDistance, Destination = source.Destination
    };
}
