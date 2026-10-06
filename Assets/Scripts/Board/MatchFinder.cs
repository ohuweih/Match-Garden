using System.Collections.Generic;

public static class MatchFinder
{
    // =========================================================
    // SIMPLE MATCH SEARCH
    //
    // BoardModel.TrySwap() uses this only to answer:
    //
    // "Did this move create any valid match?"
    //
    // It doesn't care which special should be created.
    // =========================================================

    public static List<BoardCell> FindMatches(
        BoardModel board)
    {
        List<BoardCell> matches =
            new List<BoardCell>();

        FindHorizontalMatches(
            board,
            matches
        );

        FindVerticalMatches(
            board,
            matches
        );

        FindSquareMatches(
            board,
            matches
        );

        return matches;
    }

    private static void FindHorizontalMatches(
        BoardModel board,
        List<BoardCell> matches)
    {
        for (int y = 0; y < board.Height; y++)
        {
            int x = 0;

            while (x < board.Width)
            {
                BoardCell first =
                    board.GetCell(x, y);

                if (!CanParticipateInMatch(first))
                {
                    x++;
                    continue;
                }

                PieceType color =
                    first.Piece.Color;

                int checkX = x + 1;

                while (
                    checkX < board.Width &&
                    CanParticipateInMatch(
                        board.GetCell(checkX, y)
                    ) &&
                    board.GetCell(checkX, y)
                        .Piece.Color == color
                )
                {
                    checkX++;
                }

                int matchLength =
                    checkX - x;

                if (matchLength >= 3)
                {
                    for (
                        int matchX = x;
                        matchX < checkX;
                        matchX++
                    )
                    {
                        AddIfMissing(
                            matches,
                            board.GetCell(matchX, y)
                        );
                    }
                }

                x = checkX;
            }
        }
    }

    private static void FindVerticalMatches(
        BoardModel board,
        List<BoardCell> matches)
    {
        for (int x = 0; x < board.Width; x++)
        {
            int y = 0;

            while (y < board.Height)
            {
                BoardCell first =
                    board.GetCell(x, y);

                if (!CanParticipateInMatch(first))
                {
                    y++;
                    continue;
                }

                PieceType color =
                    first.Piece.Color;

                int checkY = y + 1;

                while (
                    checkY < board.Height &&
                    CanParticipateInMatch(
                        board.GetCell(x, checkY)
                    ) &&
                    board.GetCell(x, checkY)
                        .Piece.Color == color
                )
                {
                    checkY++;
                }

                int matchLength =
                    checkY - y;

                if (matchLength >= 3)
                {
                    for (
                        int matchY = y;
                        matchY < checkY;
                        matchY++
                    )
                    {
                        AddIfMissing(
                            matches,
                            board.GetCell(x, matchY)
                        );
                    }
                }

                y = checkY;
            }
        }
    }

    private static void FindSquareMatches(
        BoardModel board,
        List<BoardCell> matches)
    {
        for (
            int x = 0;
            x < board.Width - 1;
            x++
        )
        {
            for (
                int y = 0;
                y < board.Height - 1;
                y++
            )
            {
                BoardCell bottomLeft =
                    board.GetCell(x, y);

                BoardCell bottomRight =
                    board.GetCell(x + 1, y);

                BoardCell topLeft =
                    board.GetCell(x, y + 1);

                BoardCell topRight =
                    board.GetCell(x + 1, y + 1);

                if (
                    !CanParticipateInMatch(bottomLeft) ||
                    !CanParticipateInMatch(bottomRight) ||
                    !CanParticipateInMatch(topLeft) ||
                    !CanParticipateInMatch(topRight)
                )
                {
                    continue;
                }

                PieceType color =
                    bottomLeft.Piece.Color;

                bool isSquare =
                    bottomRight.Piece.Color == color &&
                    topLeft.Piece.Color == color &&
                    topRight.Piece.Color == color;

                if (!isSquare)
                {
                    continue;
                }

                AddIfMissing(matches, bottomLeft);
                AddIfMissing(matches, bottomRight);
                AddIfMissing(matches, topLeft);
                AddIfMissing(matches, topRight);
            }
        }
    }

    // =========================================================
    // STRUCTURED MATCH SEARCH
    //
    // BoardResolver uses this to determine which pattern won
    // and therefore which special should be created.
    // =========================================================

    public static List<MatchResult> FindMatchResults(
        BoardModel board)
    {
        List<MatchResult> candidates =
            new List<MatchResult>();

        // Find EVERYTHING first.
        FindHorizontalCandidates(
            board,
            candidates
        );

        FindVerticalCandidates(
            board,
            candidates
        );

        FindSquareCandidates(
            board,
            candidates
        );

        FindTLShapeCandidates(
            board,
            candidates
        );

        // Then strongest patterns get first choice
        // over overlapping cells.
        candidates.Sort(
            CompareMatchPriority
        );

        List<MatchResult> selected =
            new List<MatchResult>();

        foreach (MatchResult candidate in candidates)
        {
            if (
                SharesCellsWithExistingResult(
                    candidate,
                    selected
                )
            )
            {
                continue;
            }

            selected.Add(candidate);
        }

        return selected;
    }

    // =========================================================
    // STRAIGHT CANDIDATES
    // =========================================================

    private static void FindHorizontalCandidates(
        BoardModel board,
        List<MatchResult> candidates)
    {
        for (int y = 0; y < board.Height; y++)
        {
            int x = 0;

            while (x < board.Width)
            {
                BoardCell first =
                    board.GetCell(x, y);

                if (!CanParticipateInMatch(first))
                {
                    x++;
                    continue;
                }

                PieceType color =
                    first.Piece.Color;

                List<BoardCell> cells =
                    new List<BoardCell>();

                cells.Add(first);

                int checkX = x + 1;

                while (
                    checkX < board.Width &&
                    CanParticipateInMatch(
                        board.GetCell(checkX, y)
                    ) &&
                    board.GetCell(checkX, y)
                        .Piece.Color == color
                )
                {
                    cells.Add(
                        board.GetCell(checkX, y)
                    );

                    checkX++;
                }

                if (cells.Count >= 3)
                {
                    candidates.Add(
                        new MatchResult(
                            GetHorizontalShape(
                                cells.Count
                            ),
                            cells
                        )
                    );
                }

                x = checkX;
            }
        }
    }

    private static void FindVerticalCandidates(
        BoardModel board,
        List<MatchResult> candidates)
    {
        for (int x = 0; x < board.Width; x++)
        {
            int y = 0;

            while (y < board.Height)
            {
                BoardCell first =
                    board.GetCell(x, y);

                if (!CanParticipateInMatch(first))
                {
                    y++;
                    continue;
                }

                PieceType color =
                    first.Piece.Color;

                List<BoardCell> cells =
                    new List<BoardCell>();

                cells.Add(first);

                int checkY = y + 1;

                while (
                    checkY < board.Height &&
                    CanParticipateInMatch(
                        board.GetCell(x, checkY)
                    ) &&
                    board.GetCell(x, checkY)
                        .Piece.Color == color
                )
                {
                    cells.Add(
                        board.GetCell(x, checkY)
                    );

                    checkY++;
                }

                if (cells.Count >= 3)
                {
                    candidates.Add(
                        new MatchResult(
                            GetVerticalShape(
                                cells.Count
                            ),
                            cells
                        )
                    );
                }

                y = checkY;
            }
        }
    }

    // =========================================================
    // 2x2 SQUARE CANDIDATES
    // =========================================================

    private static void FindSquareCandidates(
        BoardModel board,
        List<MatchResult> candidates)
    {
        for (
            int x = 0;
            x < board.Width - 1;
            x++
        )
        {
            for (
                int y = 0;
                y < board.Height - 1;
                y++
            )
            {
                BoardCell bottomLeft =
                    board.GetCell(x, y);

                BoardCell bottomRight =
                    board.GetCell(x + 1, y);

                BoardCell topLeft =
                    board.GetCell(x, y + 1);

                BoardCell topRight =
                    board.GetCell(x + 1, y + 1);

                if (
                    !CanParticipateInMatch(bottomLeft) ||
                    !CanParticipateInMatch(bottomRight) ||
                    !CanParticipateInMatch(topLeft) ||
                    !CanParticipateInMatch(topRight)
                )
                {
                    continue;
                }

                PieceType color =
                    bottomLeft.Piece.Color;

                bool isSquare =
                    bottomRight.Piece.Color == color &&
                    topLeft.Piece.Color == color &&
                    topRight.Piece.Color == color;

                if (!isSquare)
                {
                    continue;
                }

                List<BoardCell> cells =
                    new List<BoardCell>
                    {
                        bottomLeft,
                        bottomRight,
                        topLeft,
                        topRight
                    };

                candidates.Add(
                    new MatchResult(
                        MatchShape.Square,
                        cells
                    )
                );
            }
        }
    }

    // =========================================================
    // T / L CANDIDATES
    //
    // At every cell we ask:
    //
    // "Is this the intersection of a horizontal
    //  run of 3+ and a vertical run of 3+?"
    //
    // If yes, their union forms a T/L/Cross-style match.
    // =========================================================

    private static void FindTLShapeCandidates(
        BoardModel board,
        List<MatchResult> candidates)
    {
        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                BoardCell center =
                    board.GetCell(x, y);

                if (!CanParticipateInMatch(center))
                {
                    continue;
                }

                PieceType color =
                    center.Piece.Color;

                List<BoardCell> horizontal =
                    GetHorizontalRunThroughCell(
                        board,
                        x,
                        y,
                        color
                    );

                List<BoardCell> vertical =
                    GetVerticalRunThroughCell(
                        board,
                        x,
                        y,
                        color
                    );

                if (
                    horizontal.Count < 3 ||
                    vertical.Count < 3
                )
                {
                    continue;
                }

                List<BoardCell> combined =
                    new List<BoardCell>();

                foreach (BoardCell cell in horizontal)
                {
                    AddIfMissing(
                        combined,
                        cell
                    );
                }

                foreach (BoardCell cell in vertical)
                {
                    AddIfMissing(
                        combined,
                        cell
                    );
                }

                if (combined.Count < 5)
                {
                    continue;
                }

                MatchShape shape =
                    DetermineTLShape(
                        horizontal,
                        vertical,
                        center
                    );

                candidates.Add(
                    new MatchResult(
                        shape,
                        combined
                    )
                );
            }
        }
    }

    private static List<BoardCell>
        GetHorizontalRunThroughCell(
            BoardModel board,
            int centerX,
            int y,
            PieceType color)
    {
        List<BoardCell> cells =
            new List<BoardCell>();

        int left = centerX;

        while (left - 1 >= 0)
        {
            BoardCell cell =
                board.GetCell(left - 1, y);

            if (
                !CanParticipateInMatch(cell) ||
                cell.Piece.Color != color
            )
            {
                break;
            }

            left--;
        }

        int right = centerX;

        while (right + 1 < board.Width)
        {
            BoardCell cell =
                board.GetCell(right + 1, y);

            if (
                !CanParticipateInMatch(cell) ||
                cell.Piece.Color != color
            )
            {
                break;
            }

            right++;
        }

        for (
            int x = left;
            x <= right;
            x++
        )
        {
            cells.Add(
                board.GetCell(x, y)
            );
        }

        return cells;
    }

    private static List<BoardCell>
        GetVerticalRunThroughCell(
            BoardModel board,
            int x,
            int centerY,
            PieceType color)
    {
        List<BoardCell> cells =
            new List<BoardCell>();

        int bottom = centerY;

        while (bottom - 1 >= 0)
        {
            BoardCell cell =
                board.GetCell(x, bottom - 1);

            if (
                !CanParticipateInMatch(cell) ||
                cell.Piece.Color != color
            )
            {
                break;
            }

            bottom--;
        }

        int top = centerY;

        while (top + 1 < board.Height)
        {
            BoardCell cell =
                board.GetCell(x, top + 1);

            if (
                !CanParticipateInMatch(cell) ||
                cell.Piece.Color != color
            )
            {
                break;
            }

            top++;
        }

        for (
            int y = bottom;
            y <= top;
            y++
        )
        {
            cells.Add(
                board.GetCell(x, y)
            );
        }

        return cells;
    }

    private static MatchShape DetermineTLShape(
        List<BoardCell> horizontal,
        List<BoardCell> vertical,
        BoardCell center)
    {
        bool horizontalHasLeft = false;
        bool horizontalHasRight = false;

        foreach (BoardCell cell in horizontal)
        {
            if (cell.X < center.X)
            {
                horizontalHasLeft = true;
            }

            if (cell.X > center.X)
            {
                horizontalHasRight = true;
            }
        }

        bool verticalHasBelow = false;
        bool verticalHasAbove = false;

        foreach (BoardCell cell in vertical)
        {
            if (cell.Y < center.Y)
            {
                verticalHasBelow = true;
            }

            if (cell.Y > center.Y)
            {
                verticalHasAbove = true;
            }
        }

        bool horizontalBothSides =
            horizontalHasLeft &&
            horizontalHasRight;

        bool verticalBothSides =
            verticalHasBelow &&
            verticalHasAbove;

        // If one arm passes completely through the
        // intersection, it behaves like a T.
        //
        // A + shaped intersection is also treated as T
        // for special-generation purposes.
        if (
            horizontalBothSides ||
            verticalBothSides
        )
        {
            return MatchShape.TShape;
        }

        return MatchShape.LShape;
    }

    // =========================================================
    // PRIORITY
    // =========================================================

    private static int GetPriority(
        MatchShape shape)
    {
        switch (shape)
        {
            case MatchShape.FivePlusHorizontal:
            case MatchShape.FivePlusVertical:
                return 5;

            case MatchShape.TShape:
            case MatchShape.LShape:
                return 4;

            case MatchShape.FourHorizontal:
            case MatchShape.FourVertical:
                return 3;

            case MatchShape.Square:
                return 2;

            case MatchShape.Three:
                return 1;

            default:
                return 0;
        }
    }

    private static int CompareMatchPriority(
        MatchResult first,
        MatchResult second)
    {
        int firstPriority =
            GetPriority(first.Shape);

        int secondPriority =
            GetPriority(second.Shape);

        // Higher priority comes first.
        int priorityComparison =
            secondPriority.CompareTo(
                firstPriority
            );

        if (priorityComparison != 0)
        {
            return priorityComparison;
        }

        // If priority is identical, prefer the
        // pattern containing more pieces.
        return second.Cells.Count.CompareTo(
            first.Cells.Count
        );
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static bool CanParticipateInMatch(
        BoardCell cell)
    {
        return
            cell.IsPlayable &&
            !cell.IsEmpty &&
            cell.Piece.IsColored && !cell.Piece.IsMatchProtected;
    }

    private static bool SharesCellsWithExistingResult(
        MatchResult candidate,
        List<MatchResult> selected)
    {
        foreach (MatchResult existing in selected)
        {
            foreach (BoardCell cell in candidate.Cells)
            {
                if (
                    existing.Cells.Contains(cell)
                )
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void AddIfMissing(
        List<BoardCell> cells,
        BoardCell cell)
    {
        if (!cells.Contains(cell))
        {
            cells.Add(cell);
        }
    }

    private static MatchShape GetHorizontalShape(
        int length)
    {
        if (length == 3)
        {
            return MatchShape.Three;
        }

        if (length == 4)
        {
            return MatchShape.FourHorizontal;
        }

        return MatchShape.FivePlusHorizontal;
    }

    private static MatchShape GetVerticalShape(
        int length)
    {
        if (length == 3)
        {
            return MatchShape.Three;
        }

        if (length == 4)
        {
            return MatchShape.FourVertical;
        }

        return MatchShape.FivePlusVertical;
    }
}