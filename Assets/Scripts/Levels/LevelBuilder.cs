using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class LevelBuildResult
{
    public BoardModel Board { get; }
    public ColorRarityTable ColorRarity { get; }
    internal LevelBuildResult(BoardModel board, ColorRarityTable rarity) { Board = board; ColorRarity = rarity; }
}

public static class LevelBuilder
{
    // Fatal rule errors stop loading. Placement errors preserve the old scene
    // behavior: report the issue, skip that entry, and keep the rest of the board.
    public static List<string> Validate(LevelSettings settings)
    {
        var errors = new List<string>();
        if (settings == null) { errors.Add("Level settings are missing."); return errors; }
        if (settings.Width < 1 || settings.Height < 1) errors.Add("Board width and height must be positive.");
        if (!Enum.IsDefined(typeof(BoardShape), settings.Shape)) errors.Add("Unknown board shape.");
        if (!Enum.IsDefined(typeof(RefillMode), settings.RefillMode)) errors.Add("Unknown refill mode.");
        if (settings.MatchesToUnlock < 1) errors.Add("Matches To Unlock must be at least 1.");
        if (settings.UseMoveLimit && settings.MoveLimit < 1) errors.Add("Move Limit must be at least 1.");
        if (settings.UseMoveLimit && (settings.Objectives == null || settings.Objectives.Length == 0))
            errors.Add("A move-limited level needs at least one objective.");
        if (settings.PointsPerPiece < 0) errors.Add("Points Per Piece cannot be negative.");
        if (settings.CascadeBonus == null || settings.CascadeBonus.TriggerCascades < 1 || settings.CascadeBonus.BonusMoves < 1)
            errors.Add("Hot Streak needs positive trigger cascades and bonus moves.");
        try { new ColorRarityTable(settings.ColorRarity); }
        catch (ArgumentException error) { errors.Add("Color Rarity: " + error.Message); }
        if (settings.Packages != null) errors.AddRange(settings.Packages.Validate());
        if (settings.Generators != null)
            foreach (var generator in settings.Generators)
                if (generator != null && (!Enum.IsDefined(typeof(GeneratorType), generator.Type) ||
                    generator.HitsToDestroy < 1 || generator.PlayerMovesPerSpawn < 1))
                    errors.Add("Generators need a valid type, positive health, and positive spawn cadence.");
        if (settings.FrostCells != null)
            foreach (var frost in settings.FrostCells)
                if (frost != null && (frost.Layers < 1 || frost.Layers > 2))
                    errors.Add($"Frost at {frost.Cell} needs one or two layers.");
        if (settings.Objectives != null)
            for (int i = 0; i < settings.Objectives.Length; i++)
            {
                var goal = settings.Objectives[i];
                if (goal == null || (goal.Type != ObjectiveType.ClearFrost && goal.Type != ObjectiveType.DestroyFrostMachines && goal.Target < 1) || !Enum.IsDefined(typeof(ObjectiveType), goal.Type))
                    errors.Add($"Objective {i + 1} needs a valid type and positive target.");
                else if (goal.Type == ObjectiveType.ClearColor && !Enum.IsDefined(typeof(PieceType), goal.Color))
                    errors.Add($"Objective {i + 1} has an invalid color.");
            }
        if (errors.Count == 0)
        {
            var mask = CreateMask(settings, null, out _);
            foreach (var generator in GeneratorPlacements(settings, null)) mask[generator.Cell.x, generator.Cell.y] = false;
            bool anyPlayable = false;
            foreach (bool playable in mask) anyPlayable |= playable;
            if (!anyPlayable) errors.Add("Board layout must contain at least one playable cell.");
            int machines = CountGenerators(settings, GeneratorType.FrostMachine);
            if (settings.Objectives != null)
                foreach (var goal in settings.Objectives)
                {
                    if (goal.Type == ObjectiveType.ClearFrost && CountFrostLayers(settings) == 0 && machines == 0)
                        errors.Add("Clear Frost needs at least one valid frost placement or Frost Machine.");
                    if (goal.Type == ObjectiveType.DestroyFrostMachines && machines == 0)
                        errors.Add("Destroy Frost Machines needs at least one valid Frost Machine placement.");
                }
        }
        return errors;
    }

    public static LevelBuildResult Build(LevelSettings settings, Action<string> warn = null, bool prepareCampaignOpening = false)
    {
        var errors = Validate(settings);
        if (errors.Count > 0) throw new ArgumentException(string.Join("\n", errors));
        var mask = CreateMask(settings, warn, out var locks);
        var rarity = new ColorRarityTable(settings.ColorRarity);
        var board = new BoardModel(settings.Width, settings.Height, mask, locks, rarity);
        foreach (var generator in GeneratorPlacements(settings, warn))
            board.GetCell(generator.Cell.x, generator.Cell.y).PlaceGenerator(
                new BoardGenerator(generator.Type, generator.HitsToDestroy, generator.PlayerMovesPerSpawn));
        foreach (var frost in FrostPlacements(settings, warn))
            board.GetCell(frost.Cell.x, frost.Cell.y).SetFrostLayers(frost.Layers);
        int birdNumber = 0;
        var placed = new HashSet<Vector2Int>();
        if (settings.MovableObjectiveCells != null)
            foreach (var position in settings.MovableObjectiveCells)
            {
                if (!board.IsPlayable(position.x, position.y))
                {
                    warn?.Invoke($"Bird {position} must start on a playable tile; ignored.");
                    continue;
                }
                if (!placed.Add(position))
                {
                    warn?.Invoke($"Duplicate bird at {position}; ignored.");
                    continue;
                }
                var rescue = settings.DefaultBirdRescue ?? new BirdRescueSettings();
                if (settings.BirdRescueOverrides != null)
                    foreach (var entry in settings.BirdRescueOverrides)
                        if (entry != null && entry.StartingCell == position && entry.Rescue != null) rescue = entry.Rescue;
                try
                {
                    var goal = settings.RefillMode == RefillMode.Classic
                        ? new BirdRescueGoal(BirdRescueCondition.ReachBottom)
                        : rescue.CreateGoal(board);
                    board.GetCell(position.x, position.y).Piece = PieceData.CreateMovableObjective(goal, ++birdNumber);
                }
                catch (ArgumentException error) { warn?.Invoke($"Bird at {position} ignored: {error.Message}"); }
            }
        if (settings.BirdRescueOverrides != null)
            foreach (var entry in settings.BirdRescueOverrides)
                if (entry == null || !placed.Contains(entry.StartingCell))
                    warn?.Invoke("Bird rescue override has no corresponding playable bird placement; ignored.");
        if (prepareCampaignOpening && !CampaignOpeningBoard.TryPrepare(board))
            throw new ArgumentException("Campaign layout cannot produce a match-free opening with a legal move.");
        return new LevelBuildResult(board, rarity);
    }

    public static int CountGenerators(LevelSettings settings, GeneratorType type)
    {
        int count = 0;
        foreach (var generator in GeneratorPlacements(settings, null)) if (generator.Type == type) count++;
        return count;
    }

    private static IEnumerable<GeneratorPlacement> GeneratorPlacements(LevelSettings settings, Action<string> warn)
    {
        if (settings.Generators == null) yield break;
        var mask = CreateMask(settings, null, out _);
        var placed = new HashSet<Vector2Int>();
        foreach (var generator in settings.Generators)
        {
            if (generator == null) { warn?.Invoke("Empty generator placement; ignored."); continue; }
            var cell = generator.Cell;
            if (!Inside(settings, cell) || !mask[cell.x, cell.y])
            { warn?.Invoke($"Generator {cell} needs a playable cell; ignored."); continue; }
            if (!placed.Add(cell)) { warn?.Invoke($"Duplicate generator at {cell}; ignored."); continue; }
            yield return generator;
        }
    }

    public static int CountFrostLayers(LevelSettings settings)
    {
        int count = 0;
        foreach (var frost in FrostPlacements(settings, null)) count += frost.Layers;
        return count;
    }

    public static ObjectiveDefinition[] CreateObjectives(LevelSettings settings)
    {
        var goals = settings.Objectives == null ? new ObjectiveDefinition[0]
            : Array.ConvertAll(settings.Objectives, goal => new ObjectiveDefinition
                { Type = goal.Type, Color = goal.Color, Target = goal.Target });
        foreach (var goal in goals)
        {
            if (goal.Type == ObjectiveType.ClearFrost) goal.Target = CountFrostLayers(settings);
            if (goal.Type == ObjectiveType.DestroyFrostMachines) goal.Target = CountGenerators(settings, GeneratorType.FrostMachine);
        }
        return goals;
    }

    private static IEnumerable<FrostPlacement> FrostPlacements(LevelSettings settings, Action<string> warn)
    {
        if (settings.FrostCells == null) yield break;
        var mask = CreateMask(settings, null, out _);
        foreach (var generator in GeneratorPlacements(settings, null)) mask[generator.Cell.x, generator.Cell.y] = false;
        var placed = new HashSet<Vector2Int>();
        foreach (var frost in settings.FrostCells)
        {
            if (frost == null) { warn?.Invoke("Empty frost placement; ignored."); continue; }
            var cell = frost.Cell;
            if (!Inside(settings, cell) || !mask[cell.x, cell.y])
            { warn?.Invoke($"Frost {cell} must be on a playable cell; ignored."); continue; }
            if (!placed.Add(cell)) { warn?.Invoke($"Duplicate frost at {cell}; ignored."); continue; }
            yield return frost;
        }
    }

    private static bool[,] CreateMask(LevelSettings settings, Action<string> warn, out int[,] locks)
    {
        var mask = BoardLayout.CreateMask(settings.Width, settings.Height, settings.Shape);
        locks = new int[settings.Width, settings.Height];
        if (settings.BlockedCells != null)
            foreach (var cell in settings.BlockedCells)
            {
                if (Inside(settings, cell)) mask[cell.x, cell.y] = false;
                else warn?.Invoke($"Blocked cell {cell} lies outside the board and was ignored.");
            }
        if (settings.LockedCells != null)
            foreach (var cell in settings.LockedCells)
            {
                if (Inside(settings, cell))
                {
                    mask[cell.x, cell.y] = false;
                    locks[cell.x, cell.y] = settings.MatchesToUnlock;
                }
                else warn?.Invoke($"Locked cell {cell} lies outside the board and was ignored.");
            }
        return mask;
    }

    private static bool Inside(LevelSettings settings, Vector2Int cell) =>
        cell.x >= 0 && cell.x < settings.Width && cell.y >= 0 && cell.y < settings.Height;
}
