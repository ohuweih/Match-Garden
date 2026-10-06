using System.Collections.Generic;

public class ResolutionRound
{
    public List<SpecialEffectRecord> SpecialEffects { get; } = new List<SpecialEffectRecord>();
    public List<PackageChange> PackageChanges { get; } = new List<PackageChange>();
    internal List<BoardCell> HitPackages { get; } = new List<BoardCell>();
    private readonly HashSet<PieceData> packageHits = new HashSet<PieceData>();
    public void HitPackage(BoardCell cell)
    {
        if (cell?.Piece != null && cell.Piece.IsPackage && packageHits.Add(cell.Piece)) HitPackages.Add(cell);
    }

    public List<GeneratorChange> GeneratorChanges { get; } = new List<GeneratorChange>();
    public List<GeneratorActivation> GeneratorActivations { get; } = new List<GeneratorActivation>();
    private readonly HashSet<BoardCell> generatorHits = new HashSet<BoardCell>();

    public void HitGenerator(BoardCell cell)
    {
        var generator = cell?.Generator;
        if (generator == null || !generator.IsAlive || !generatorHits.Add(cell)) return;
        int before = generator.RemainingHits;
        generator.Damage();
        GeneratorChanges.Add(new GeneratorChange(cell.X, cell.Y, generator.Type, before, generator.RemainingHits));
    }

    public bool IsMatchRound { get; }
    public List<FrostTileChange> FrostChanges { get; } = new List<FrostTileChange>();
    private readonly HashSet<BoardCell> frostHits = new HashSet<BoardCell>();

    public void HitFrost(BoardCell cell)
    {
        if (cell == null || cell.FrostLayers == 0 || !frostHits.Add(cell)) return;
        int before = cell.FrostLayers;
        if (cell.RemoveFrostLayer()) FrostChanges.Add(new FrostTileChange(cell.X, cell.Y, before, cell.FrostLayers));
    }

    public List<BirdRescueRecord> RescuedBirds { get; } = new List<BirdRescueRecord>();
    private readonly List<ClearedPieceRecord> clearedPieces = new List<ClearedPieceRecord>();
    public IReadOnlyList<ClearedPieceRecord> ClearedPieces => clearedPieces.AsReadOnly();
    public List<BoardCell> ClearedCells { get; }

    public List<GravityMove> GravityMoves { get; }

    public List<SpawnRecord> Spawns { get; }
    public List<LockedTileChange> LockedTileChanges { get; } = new List<LockedTileChange>();

    public ResolutionRound(bool isMatchRound = true)
    {
        IsMatchRound = isMatchRound;
        ClearedCells =
            new List<BoardCell>();

        GravityMoves =
            new List<GravityMove>();

        Spawns =
            new List<SpawnRecord>();
    }

    public void AddClearedCell(
        BoardCell cell)
    {
        HitFrost(cell);
        if (!cell.IsEmpty && !ClearedCells.Contains(cell))
        {
            if (cell.Piece.IsColored) clearedPieces.Add(new ClearedPieceRecord(cell));
            ClearedCells.Add(cell);
        }
    }
}