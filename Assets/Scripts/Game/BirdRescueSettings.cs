using System;
using UnityEngine;

[Serializable]
public class BirdRescueSettings
{
    public BirdRescueCondition Condition = BirdRescueCondition.TravelDistance;
    [Min(1)] public int RequiredDistance = 8;
    [Tooltip("Used by Reach Destination. Bottom-left is (0, 0); must be a playable cell.")]
    public Vector2Int Destination;

    public BirdRescueGoal CreateGoal(BoardModel board)
    {
        if (Condition == BirdRescueCondition.ReachDestination &&
            !board.IsPlayable(Destination.x, Destination.y))
            throw new ArgumentException($"Nest {Destination} must be on a playable tile.");
        return new BirdRescueGoal(Condition, RequiredDistance, Destination.x, Destination.y);
    }
}

[Serializable]
public class BirdRescueOverride
{
    [Tooltip("An existing entry in Movable Objective Cells. This overrides only that bird's rescue rule.")]
    public Vector2Int StartingCell;
    public BirdRescueSettings Rescue = new BirdRescueSettings();
}
