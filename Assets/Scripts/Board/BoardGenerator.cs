using System;

public enum GeneratorType { FrostMachine }

// Location belongs to the BoardCell. Runtime health/cadence never mutate level authoring data.
public sealed class BoardGenerator
{
    public GeneratorType Type { get; }
    public int RemainingHits { get; private set; }
    public int PlayerMovesPerSpawn { get; }
    public int MovesSinceSpawn { get; private set; }
    public bool IsAlive => RemainingHits > 0;

    public BoardGenerator(GeneratorType type, int hitsToDestroy = 2, int playerMovesPerSpawn = 1)
    {
        if (!Enum.IsDefined(typeof(GeneratorType), type)) throw new ArgumentException("Unknown generator type.");
        if (hitsToDestroy < 1 || playerMovesPerSpawn < 1) throw new ArgumentOutOfRangeException("Generator health and cadence must be positive.");
        Type = type; RemainingHits = hitsToDestroy; PlayerMovesPerSpawn = playerMovesPerSpawn;
    }

    internal bool Damage()
    {
        if (!IsAlive) return false;
        RemainingHits--;
        return true;
    }

    internal bool AdvancePlayerAction()
    {
        if (!IsAlive) return false;
        if (++MovesSinceSpawn < PlayerMovesPerSpawn) return false;
        MovesSinceSpawn = 0;
        return true;
    }
}
