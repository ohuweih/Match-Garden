using System;
using System.Collections.Generic;

public enum LevelAttemptStatus { Playing, Won, Lost }

// Resolution IDs prevent counting the same action twice if an event is replayed.
public sealed class LevelAttemptModel
{
    public bool UseMoveLimit { get; }
    public int MoveLimit { get; }
    public int MovesRemaining { get; private set; }
    public LevelAttemptStatus Status { get; private set; } = LevelAttemptStatus.Playing;
    public bool IsFinished => Status != LevelAttemptStatus.Playing;
    private readonly bool completeOnObjectives;
    private readonly HashSet<Guid> countedResolutions = new HashSet<Guid>();

    public LevelAttemptModel(bool useMoveLimit, int moveLimit = 30, bool completeOnObjectives = false)
    {
        if (useMoveLimit && moveLimit < 1) throw new ArgumentOutOfRangeException(nameof(moveLimit));
        this.completeOnObjectives = useMoveLimit || completeOnObjectives;
        UseMoveLimit = useMoveLimit;
        MoveLimit = moveLimit;
        MovesRemaining = useMoveLimit ? moveLimit : 0;
    }

    public void RecordResolution(ResolutionSequence sequence)
    {
        if (sequence == null) throw new ArgumentNullException(nameof(sequence));
        if (!UseMoveLimit || IsFinished || !sequence.ConsumesNormalMove) return;
        if (countedResolutions.Add(sequence.Id) && MovesRemaining > 0) MovesRemaining--;
    }

    // Only evaluate after the entire opening/player resolution chain is finished.
    // Objectives completed on the last move (including its bonus) take precedence.
    public void CompleteChain(bool allObjectivesComplete)
    {
        if (!completeOnObjectives || IsFinished) return;
        if (allObjectivesComplete) Status = LevelAttemptStatus.Won;
        else if (UseMoveLimit && MovesRemaining == 0) Status = LevelAttemptStatus.Lost;
    }
}
