using System;
using System.Collections.Generic;

public sealed class ObjectiveProgress
{
    public ObjectiveType Type { get; }
    public PieceType Color { get; }
    public long Target { get; private set; }
    public int RemainingGenerators { get; private set; }
    public long Current { get; private set; }
    public bool IsComplete => Current >= Target && RemainingGenerators == 0;

    internal ObjectiveProgress(ObjectiveDefinition definition, int frostMachines = 0)
    {
        if (definition.Target < 0 || (definition.Target == 0 && definition.Type != ObjectiveType.ClearFrost)) throw new ArgumentOutOfRangeException(nameof(definition.Target));
        if (!Enum.IsDefined(typeof(ObjectiveType), definition.Type)) throw new ArgumentException("Unknown objective type.");
        Type = definition.Type;
        Color = definition.Color;
        Target = definition.Target;
        RemainingGenerators = definition.Type == ObjectiveType.ClearFrost ? frostMachines : 0;
    }

    internal bool UpdateFrostSupply(long added, int destroyedMachines, int createdMachines)
    {
        int before = RemainingGenerators;
        Target += added;
        RemainingGenerators = Math.Max(0, RemainingGenerators + createdMachines - destroyedMachines);
        return added > 0 || RemainingGenerators != before;
    }

    internal bool AddTarget(int amount)
    {
        Target += amount;
        return amount > 0;
    }

    internal bool Add(long amount)
    {
        long before = Current;
        Current += Math.Min(amount, Target - Current);
        return before != Current;
    }

    public string Label => Type == ObjectiveType.Score ? "Earn points"
        : Type == ObjectiveType.ClearColor ? $"Clear {Color} pieces"
        : Type == ObjectiveType.RescueBirds ? "Rescue birds"
        : Type == ObjectiveType.DestroyFrostMachines ? "Destroy frost machines"
        : Type == ObjectiveType.ClearFrost ? "Clear frost layers" : "Unlock tiles";
}

public sealed class ObjectiveModel
{
    public IReadOnlyList<ObjectiveProgress> Objectives { get; }
    public bool CountOpeningCascades { get; }
    public bool AllComplete { get; private set; }
    public event Action ProgressChanged;
    public event Action<ObjectiveProgress> ObjectiveCompleted;
    public event Action AllCompleted;
    private readonly HashSet<(Guid, int)> processedRounds = new HashSet<(Guid, int)>();

    public ObjectiveModel(IEnumerable<ObjectiveDefinition> definitions, bool countOpeningCascades = true, int frostMachines = 0)
    {
        if (frostMachines < 0) throw new ArgumentOutOfRangeException(nameof(frostMachines));
        if (definitions == null) throw new ArgumentNullException(nameof(definitions));
        var objectives = new List<ObjectiveProgress>();
        foreach (var definition in definitions)
        {
            if (definition == null) throw new ArgumentException("An objective definition is null.");
            // Copy configuration so Inspector changes cannot mutate an active goal.
            objectives.Add(new ObjectiveProgress(definition, frostMachines));
        }
        Objectives = objectives.AsReadOnly();
        CountOpeningCascades = countOpeningCascades;
        AllComplete = objectives.Count > 0 && objectives.TrueForAll(goal => goal.IsComplete);
    }

    public void Apply(ResolutionRoundEvent result, long pointsAwarded)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));
        if (pointsAwarded < 0) throw new ArgumentOutOfRangeException(nameof(pointsAwarded));
        if (!processedRounds.Add((result.ResolutionId, result.RoundIndex))) return;
        bool skipOpeningRewards = result.Source == MoveSource.OpeningCascade && !CountOpeningCascades;

        int destroyedMachines = 0, createdMachines = 0;
        foreach (var change in result.GeneratorChanges)
        {
            if (change.Type != GeneratorType.FrostMachine) continue;
            if (change.Destroyed) destroyedMachines++;
            if (change.Created) createdMachines++;
        }
        long frostAdded = 0;
        foreach (var change in result.FrostChanges) frostAdded += change.LayersAdded;
        bool changed = false;
        var completed = new List<ObjectiveProgress>();
        foreach (var objective in Objectives)
        {
            bool wasObjectiveComplete = objective.IsComplete;
            bool supplyChanged = objective.Type == ObjectiveType.ClearFrost
                ? objective.UpdateFrostSupply(frostAdded, destroyedMachines, createdMachines)
                : objective.Type == ObjectiveType.DestroyFrostMachines && objective.AddTarget(createdMachines);
            changed |= supplyChanged;
            if (objective.IsComplete && !supplyChanged) continue;
            // Finite board objects always count, including opening destruction.
            if (skipOpeningRewards && objective.Type != ObjectiveType.ClearFrost && objective.Type != ObjectiveType.DestroyFrostMachines) continue;
            long earned = 0;
            switch (objective.Type)
            {
                case ObjectiveType.Score:
                    // Use the points actually awarded, never recompute scoring here.
                    earned = pointsAwarded;
                    break;
                case ObjectiveType.ClearColor:
                    foreach (var piece in result.ClearedPieces)
                        if (piece.Color == objective.Color) earned++;
                    break;
                case ObjectiveType.RescueBirds:
                    earned = result.RescuedBirds.Count;
                    break;
                case ObjectiveType.DestroyFrostMachines:
                    earned = destroyedMachines;
                    break;
                case ObjectiveType.ClearFrost:
                    foreach (var change in result.FrostChanges) earned += change.LayersRemoved;
                    break;
                case ObjectiveType.UnlockTiles:
                    foreach (var change in result.LockedTileChanges)
                        if (change.RemainingMatches == 0) earned++;
                    break;
            }
            if (objective.Add(earned) || supplyChanged)
            {
                changed = true;
                if (objective.IsComplete && !wasObjectiveComplete) completed.Add(objective);
            }
        }
        bool wasComplete = AllComplete;
        AllComplete = Objectives.Count > 0;
        foreach (var objective in Objectives) AllComplete &= objective.IsComplete;
        if (changed) ProgressChanged?.Invoke();
        foreach (var objective in completed) ObjectiveCompleted?.Invoke(objective);
        if (AllComplete && !wasComplete) AllCompleted?.Invoke();
    }
}
