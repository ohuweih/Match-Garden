using System;

public enum BirdRescueCondition { TravelDistance, ReachDestination, ReachBottom }

// Immutable runtime rules, independent of Inspector settings.
public sealed class BirdRescueGoal
{
    public BirdRescueCondition Condition { get; }
    public int RequiredDistance { get; }
    public int DestinationX { get; }
    public int DestinationY { get; }

    public BirdRescueGoal(BirdRescueCondition condition, int requiredDistance = 8,
        int destinationX = 0, int destinationY = 0)
    {
        if (!Enum.IsDefined(typeof(BirdRescueCondition), condition))
            throw new ArgumentException("Unknown bird rescue condition.");
        if (condition == BirdRescueCondition.TravelDistance && requiredDistance < 1)
            throw new ArgumentOutOfRangeException(nameof(requiredDistance));
        Condition = condition;
        RequiredDistance = requiredDistance;
        DestinationX = destinationX;
        DestinationY = destinationY;
    }

    public bool IsSatisfied(long distance, int x, int y)
    {
        if (Condition == BirdRescueCondition.ReachBottom) return false; // Requires board shape; see BirdRescueRules.
        return Condition == BirdRescueCondition.TravelDistance
            ? distance >= RequiredDistance
            : x == DestinationX && y == DestinationY;
    }
}
