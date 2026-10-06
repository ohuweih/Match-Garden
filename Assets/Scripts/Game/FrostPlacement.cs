using System;
using UnityEngine;

[Serializable]
public sealed class FrostPlacement
{
    [Tooltip("A playable cell, measured from the bottom-left (0, 0). Frost stays on this cell.")]
    public Vector2Int Cell;
    [Range(1, 2)] public int Layers = 1;
}
