using System;
using System.Collections.Generic;

/// <summary>
/// Finds playable actions on a settled board. Call after resolution finishes.
/// Includes direct special activation as supported by InputController.
/// </summary>
public static class BoardMoveFinder
{
    public static bool HasAnyValidMove(BoardModel board)
    {
        return TryFindMove(board, out _, out _);
    }

    // A null second cell means double-click the special in the first cell.
    public static bool TryFindMove(BoardModel board, out BoardCell first, out BoardCell second)
    {
        first = null;
        second = null;
        if (board == null)
        {
            throw new ArgumentNullException(nameof(board));
        }

        for (int y = 0; y < board.Height; y++)
        {
            for (int x = 0; x < board.Width; x++)
            {
                BoardCell cell = board.GetCell(x, y);
                if (!cell.IsPlayable || cell.IsEmpty)
                {
                    continue;
                }

                // Every special can be activated directly, even without a swap.
                // This also covers special/normal swaps and special combinations.
                if (cell.Piece.IsSpecial)
                {
                    first = cell;
                    return true;
                }

                // Check each adjacent pair only once.
                if (x + 1 < board.Width &&
                    CreatesMatch(board, cell, board.GetCell(x + 1, y)))
                {
                    first = cell;
                    second = board.GetCell(x + 1, y);
                    return true;
                }

                if (y + 1 < board.Height &&
                    CreatesMatch(board, cell, board.GetCell(x, y + 1)))
                {
                    first = cell;
                    second = board.GetCell(x, y + 1);
                    return true;
                }
            }
        }

        return false;
    }

    public static List<(BoardCell First, BoardCell Second)> FindAllMoves(BoardModel board)
    {
        if (board == null) throw new ArgumentNullException(nameof(board));
        var moves = new List<(BoardCell First, BoardCell Second)>();
        for (int y = 0; y < board.Height; y++)
        {
            for (int x = 0; x < board.Width; x++)
            {
                BoardCell cell = board.GetCell(x, y);
                if (!cell.IsPlayable || cell.IsEmpty) continue;

                // Direct activation and adjacent swaps are distinct actions.
                if (cell.Piece.IsSpecial) moves.Add((cell, null));
                if (x + 1 < board.Width && CreatesMatch(board, cell, board.GetCell(x + 1, y)))
                    moves.Add((cell, board.GetCell(x + 1, y)));
                if (y + 1 < board.Height && CreatesMatch(board, cell, board.GetCell(x, y + 1)))
                    moves.Add((cell, board.GetCell(x, y + 1)));
            }
        }
        return moves;
    }

    // Special swaps invoke the activation pipeline rather than create a match.
    // Normal swaps may still match existing specials and trigger their effects.
    public static List<(BoardCell First, BoardCell Second)> FindMatchProducingMoves(BoardModel board)
    {
        var moves = FindAllMoves(board);
        moves.RemoveAll(move => move.Second == null ||
            move.First.Piece.IsSpecial || move.Second.Piece.IsSpecial);
        return moves;
    }

    private static bool CreatesMatch(
        BoardModel board,
        BoardCell first,
        BoardCell second)
    {
        if (!first.CanSwap || !second.CanSwap)
        {
            return false;
        }

        PieceData firstPiece = first.Piece;
        PieceData secondPiece = second.Piece;

        if (firstPiece.IsSpecial && secondPiece.IsSpecial)
        {
            return SpecialComboRules.GetComboType(firstPiece.Special, secondPiece.Special)
                != SpecialComboType.None;
        }

        if (firstPiece.IsSpecial || secondPiece.IsSpecial)
        {
            return true;
        }

        // Normal same-color swaps are rejected by BoardModel.TrySwap.
        if (firstPiece.IsColored && secondPiece.IsColored && firstPiece.Color == secondPiece.Color)
        {
            return false;
        }

        try
        {
            first.Piece = secondPiece;
            second.Piece = firstPiece;

            var matches = MatchFinder.FindMatches(board);

            // An unrelated existing match must not make this swap look valid.
            return matches.Contains(first) || matches.Contains(second);
        }
        finally
        {
            first.Piece = firstPiece;
            second.Piece = secondPiece;
        }
    }
}
