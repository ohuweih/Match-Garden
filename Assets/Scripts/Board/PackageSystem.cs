using System;
using System.Collections.Generic;

// One per loaded board. Packages use the same completed-player-action clock as generators.
public sealed class PackageSystem
{
    private readonly PackageSettings settings;
    private readonly RefillMode refillMode;
    private readonly HashSet<Guid> processedActions = new HashSet<Guid>();
    private int movesSinceSpawn, nextBirdNumber;

    public PackageSystem(PackageSettings settings = null, RefillMode refillMode = RefillMode.Normal)
    {
        this.refillMode = refillMode;
        this.settings = (settings ?? new PackageSettings()).Copy();
        var errors = this.settings.Validate();
        if (errors.Count > 0) throw new ArgumentException(string.Join("\n", errors));
    }

    public bool TrySpawnAfterPlayerAction(BoardModel board, ResolutionSequence action, ResolutionRound round, Random random)
    {
        if (!settings.Enabled || action.Source != MoveSource.Player || !processedActions.Add(action.Id)) return false;
        if (settings.SpawnMode == PackageSpawnMode.EveryPlayerMoves)
        {
            if (++movesSinceSpawn < settings.PlayerMovesPerSpawn) return false;
            movesSinceSpawn = 0;
        }
        else if (random.NextDouble() * 100 >= settings.ChancePercent) return false;
        var candidates = new List<BoardCell>();
        int active = 0;
        for (int y = 0; y < board.Height; y++)
            for (int x = 0; x < board.Width; x++)
            {
                var cell = board.GetCell(x, y);
                if (cell.Piece != null && cell.Piece.IsPackage) active++;
                if (cell.IsPlayable && !cell.IsEmpty && cell.Piece.IsColored && !cell.Piece.IsSpecial)
                    candidates.Add(cell);
            }
        if (active >= settings.MaxActivePackages || candidates.Count == 0) return false;
        var target = candidates[random.Next(candidates.Count)];
        target.Piece = PieceData.CreatePackage(target.Piece.Color);
        round.PackageChanges.Add(new PackageChange(target, false));
        return true;
    }

    public static void HitAdjacentMatches(BoardModel board, List<MatchResult> matches, ResolutionRound round)
    {
        if (round == null || matches == null) return;
        foreach (var match in matches)
            foreach (var cell in match.Cells)
            {
                Hit(board, cell.X - 1, cell.Y, round); Hit(board, cell.X + 1, cell.Y, round);
                Hit(board, cell.X, cell.Y - 1, round); Hit(board, cell.X, cell.Y + 1, round);
            }
    }

    private static void Hit(BoardModel board, int x, int y, ResolutionRound round)
    {
        if (board.IsPlayable(x, y)) round.HitPackage(board.GetCell(x, y));
    }

    public void OpenHitPackages(BoardModel board, ResolutionRound round, Random random)
    {
        if (round == null) return;
        foreach (var cell in round.HitPackages)
        {
            if (cell.IsEmpty || !cell.Piece.IsPackage) continue;
            var color = cell.Piece.Color;
            var content = ChooseContent(random);
            switch (content)
            {
                case PackageContent.Frost:
                    int before = cell.FrostLayers;
                    cell.SetFrostLayers(Math.Min(2, before + 1));
                    if (cell.FrostLayers != before)
                        round.FrostChanges.Add(new FrostTileChange(cell.X, cell.Y, before, cell.FrostLayers));
                    cell.Piece = new PieceData(color);
                    break;
                case PackageContent.FrostMachine:
                    // Frost beneath a moving package stays on the cell beneath its contents.
                    cell.PlaceGenerator(new BoardGenerator(GeneratorType.FrostMachine,
                        settings.FrostMachineHits, settings.FrostMachineMovesPerSpawn), allowExistingFrost: true);
                    round.GeneratorChanges.Add(new GeneratorChange(cell.X, cell.Y, GeneratorType.FrostMachine, 0, settings.FrostMachineHits));
                    break;
                case PackageContent.Bird:
                    for (int y = 0; y < board.Height; y++)
                        for (int x = 0; x < board.Width; x++)
                            nextBirdNumber = Math.Max(nextBirdNumber, board.GetCell(x, y).Piece?.BirdNumber ?? 0);
                    var birdGoal = refillMode == RefillMode.Classic
                        ? new BirdRescueGoal(BirdRescueCondition.ReachBottom)
                        : new BirdRescueGoal(BirdRescueCondition.TravelDistance, settings.BirdTravelDistance);
                    cell.Piece = PieceData.CreateMovableObjective(birdGoal, ++nextBirdNumber);
                    break;
                default:
                    cell.Piece = new PieceData(color, SpecialFor(content));
                    break;
            }
            round.PackageChanges.Add(new PackageChange(cell, true, content));
        }
    }

    private PackageContent ChooseContent(Random random)
    {
        var contents = settings.Contents;
        if (contents == null || contents.Length == 0) contents = PackageSettings.CreateDefaultContents();
        long total = 0;
        foreach (var option in contents) total += option.Weight;
        double roll = random.NextDouble() * total;
        foreach (var option in contents)
        {
            roll -= option.Weight;
            if (roll < 0) return option.Content;
        }
        return contents[contents.Length - 1].Content;
    }

    private static SpecialType SpecialFor(PackageContent content)
    {
        switch (content)
        {
            case PackageContent.HorizontalLine: return SpecialType.LineHorizontal;
            case PackageContent.VerticalLine: return SpecialType.LineVertical;
            case PackageContent.Bomb: return SpecialType.Bomb;
            case PackageContent.Target: return SpecialType.Target;
            case PackageContent.ColorClear: return SpecialType.ColorClear;
            default: throw new ArgumentException("Unsupported package content.");
        }
    }
}
