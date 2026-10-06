using System;
using System.Collections.Generic;

public sealed class CascadeBonusState
{
    public bool IsActive { get; private set; }
    public int CompletedMoves { get; private set; }
    public int TotalMoves { get; private set; }
    private readonly HashSet<Guid> rewardedResolutions = new HashSet<Guid>();

    public bool TryBegin(ResolutionSequence trigger, bool enabled, int threshold, int moves)
    {
        if (threshold < 1 || moves < 1) throw new ArgumentOutOfRangeException();
        if (trigger == null) throw new ArgumentNullException(nameof(trigger));
        if (!enabled || IsActive || trigger.Source != MoveSource.Player ||
            trigger.CascadeCount < threshold || !rewardedResolutions.Add(trigger.Id)) return false;
        IsActive = true;
        CompletedMoves = 0;
        TotalMoves = moves;
        return true;
    }

    public void CompleteMove()
    {
        if (!IsActive) throw new InvalidOperationException("No active Cascade Bonus.");
        CompletedMoves++;
        if (CompletedMoves == TotalMoves) IsActive = false;
    }

    public void Stop() { IsActive = false; }
}
