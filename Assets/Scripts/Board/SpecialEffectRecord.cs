using System.Collections.Generic;
using UnityEngine;

// Captured during resolution: presentation must never reroll targets on the refilled board.
public sealed class SpecialEffectRecord
{
    public readonly SpecialType Type;
    public readonly Vector2Int Origin;
    public readonly SpecialEffectRecord Parent;
    public readonly int Radius;
    public readonly List<Vector2Int> Targets = new List<Vector2Int>();

    public SpecialEffectRecord(SpecialType type, int x, int y, SpecialEffectRecord parent = null, int radius = 2)
    { Type = type; Origin = new Vector2Int(x, y); Parent = parent; Radius = radius; }

    public void AddTarget(BoardCell cell)
    {
        var point = new Vector2Int(cell.X, cell.Y);
        if (!Targets.Contains(point)) Targets.Add(point);
    }
}
