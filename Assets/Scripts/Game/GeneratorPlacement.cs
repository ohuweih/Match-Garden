using System;
using UnityEngine;

[Serializable]
public sealed class GeneratorPlacement
{
    public GeneratorType Type = GeneratorType.FrostMachine;
    [Tooltip("Stationary blocker on a playable cell. Coordinates start at the bottom-left.")]
    public Vector2Int Cell;
    [Min(1)] public int HitsToDestroy = 2;
    [Tooltip("Produce once per this many completed player actions, after all cascades and bonus moves.")]
    [Min(1)] public int PlayerMovesPerSpawn = 1;

    public GeneratorPlacement Copy() => new GeneratorPlacement
    {
        Type = Type, Cell = Cell, HitsToDestroy = HitsToDestroy, PlayerMovesPerSpawn = PlayerMovesPerSpawn
    };
}
