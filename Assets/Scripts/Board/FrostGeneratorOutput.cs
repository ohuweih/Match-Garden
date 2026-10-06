using System;
using System.Collections.Generic;

public sealed class FrostGeneratorOutput : IGeneratorOutput
{
    public bool TryGenerate(BoardModel board, BoardCell source, ResolutionRound round, Random random)
    {
        var eligible = new List<BoardCell>();
        var seen = new HashSet<BoardCell>();
        AddNeighbors(board, source.X, source.Y, eligible, seen);
        // Existing frost extends the possible spread across the board, including
        // separate patches. Only a surviving machine initiates this output.
        for (int y = 0; y < board.Height; y++)
            for (int x = 0; x < board.Width; x++)
                if (board.GetCell(x, y).FrostLayers > 0)
                    AddNeighbors(board, x, y, eligible, seen);

        if (eligible.Count == 0) return false;
        // Choose once after collecting the entire set. Shared neighbors get one
        // entry, and this newly generated tile cannot generate another this tick.
        var target = eligible[random.Next(eligible.Count)];
        target.SetFrostLayers(1);
        round.FrostChanges.Add(new FrostTileChange(target.X, target.Y, 0, 1));
        return true;
    }

    private static void AddNeighbors(BoardModel board, int centerX, int centerY,
        List<BoardCell> cells, HashSet<BoardCell> seen)
    {
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                int x = centerX + dx, y = centerY + dy;
                if (!board.IsPlayable(x, y)) continue;
                var cell = board.GetCell(x, y);
                if (!cell.IsEmpty && cell.FrostLayers == 0 && seen.Add(cell)) cells.Add(cell);
            }
    }
}
