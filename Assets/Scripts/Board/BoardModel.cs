using System;

public class BoardModel
{
    private Random random;
    private readonly ColorRarityTable colorRarity;

    private BoardCell[,] cells;

    public int Width { get; }
    public int Height { get; }

    public BoardModel(int width, int height, bool[,] playableMask = null, int[,] unlockRequirements = null, ColorRarityTable colorRarity = null)
    {
        if (width < 1 || height < 1)
            throw new ArgumentOutOfRangeException("Board dimensions must be positive.");
        if (playableMask != null &&
            (playableMask.GetLength(0) != width || playableMask.GetLength(1) != height))
            throw new ArgumentException("Mask dimensions must match the board.");
        if (unlockRequirements != null &&
            (unlockRequirements.GetLength(0) != width || unlockRequirements.GetLength(1) != height))
            throw new ArgumentException("Unlock requirements must match board dimensions.");
        Width = width;
        Height = height;

        random = new Random();

        this.colorRarity = colorRarity ?? new ColorRarityTable();

        cells = new BoardCell[width, height];

        FillBoard(playableMask, unlockRequirements);
    }

    private void FillBoard(bool[,] playableMask, int[,] unlockRequirements)
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                int required = unlockRequirements == null ? 0 : unlockRequirements[x, y];
                bool playable = (playableMask == null || playableMask[x, y]) && required == 0;
                // Opening matches are resolved after the board is displayed.
                cells[x, y] =
                    new BoardCell(
                        x,
                        y,
                        playable ? GetRandomPiece() : null,
                        playable,
                        required
                    );
            }
        }
    }

    private PieceType GetRandomColor()
    {
        return colorRarity.ChooseColor(random);
    }

    public PieceData GetRandomPiece()
    {
        return new PieceData(
            GetRandomColor()
        );
    }

    public bool IsPlayable(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height && cells[x, y].IsPlayable;
    }

    public BoardCell GetCell(int x, int y)
    {
        return cells[x, y];
    }

    public SwapResult TrySwap(
    int x1,
    int y1,
    int x2,
    int y2)
    {
        BoardCell firstCell =
            cells[x1, y1];

        BoardCell secondCell =
            cells[x2, y2];

        if (!firstCell.CanSwap || !secondCell.CanSwap ||
            Math.Abs(x1 - x2) + Math.Abs(y1 - y2) != 1)
            return new SwapResult(false, firstCell, secondCell);

        bool firstIsSpecial =
            firstCell.Piece.IsSpecial;

        bool secondIsSpecial =
            secondCell.Piece.IsSpecial;

        if (firstIsSpecial && secondIsSpecial)
        {
            SpecialType firstSpecial =
                firstCell.Piece.Special;

            SpecialType secondSpecial =
                secondCell.Piece.Special;

            PieceType firstColor =
                firstCell.Piece.Color;

            PieceType secondColor =
                secondCell.Piece.Color;

            SpecialComboType comboType =
                SpecialComboRules.GetComboType(
                    firstSpecial,
                    secondSpecial
                );

            if (comboType == SpecialComboType.None)
            {
                return new SwapResult(
                    false,
                    firstCell,
                    secondCell
                );
            }

            SpecialComboRequest combo =
                new SpecialComboRequest(
                    comboType,
                    firstCell,
                    secondCell,
                    firstSpecial,
                    secondSpecial,
                    firstColor,
                    secondColor
                );

            SwapInternal(
                x1,
                y1,
                x2,
                y2
            );

            return new SwapResult(
                true,
                firstCell,
                secondCell,
                specialCombo: combo
            );
        }

        // Target special swapped with a normal piece.
        // Any single special can be deliberately
        // activated by swapping it with a normal piece.
        if (firstIsSpecial)
        {
            PieceType activationColor =
                secondCell.Piece.IsColored ? secondCell.Piece.Color : firstCell.Piece.Color;

            SwapInternal(
                x1,
                y1,
                x2,
                y2
            );

            secondCell.Piece.RecordTravel(x1, y1, x2, y2);
            firstCell.Piece.RecordTravel(x2, y2, x1, y1);

            // The special moved into secondCell.
            return new SwapResult(
                true,
                firstCell,
                secondCell,
                secondCell,
                activationColor
            );
        }

        if (secondIsSpecial)
        {
            PieceType activationColor =
                firstCell.Piece.IsColored ? firstCell.Piece.Color : secondCell.Piece.Color;

            SwapInternal(
                x1,
                y1,
                x2,
                y2
            );

            secondCell.Piece.RecordTravel(x1, y1, x2, y2);
            firstCell.Piece.RecordTravel(x2, y2, x1, y1);

            // The special moved into firstCell.
            return new SwapResult(
                true,
                firstCell,
                secondCell,
                firstCell,
                activationColor
            );
        }

        // Normal same-color pieces can't be swapped
        // unless one of them is another special type.
        if (
            firstCell.Piece.IsColored && secondCell.Piece.IsColored &&
            firstCell.Piece.Color ==
            secondCell.Piece.Color &&
            !firstCell.Piece.IsSpecial &&
            !secondCell.Piece.IsSpecial
        )
        {
            return new SwapResult(
                false,
                firstCell,
                secondCell
            );
        }

        SwapInternal(
            x1,
            y1,
            x2,
            y2
        );

        var matches =
            MatchFinder.FindMatches(this);

        if (matches.Contains(firstCell) || matches.Contains(secondCell))
        {
            secondCell.Piece.RecordTravel(x1, y1, x2, y2);
            firstCell.Piece.RecordTravel(x2, y2, x1, y1);
            return new SwapResult(
                true,
                firstCell,
                secondCell
            );
        }

        SwapInternal(
            x1,
            y1,
            x2,
            y2
        );

        return new SwapResult(
            false,
            firstCell,
            secondCell
        );
    }

    private void SwapInternal(
        int x1,
        int y1,
        int x2,
        int y2)
    {
        PieceData firstPiece =
            cells[x1, y1].Piece;

        PieceData secondPiece =
            cells[x2, y2].Piece;

        cells[x1, y1].Piece =
            secondPiece;

        cells[x2, y2].Piece =
            firstPiece;
    }
}