public class ClassicRefillStrategy :
    IRefillStrategy
{
    public void BeginResolution()
    {
        // Classic always falls downward.
    }
    public void Apply(
        BoardModel board,
        ResolutionRound round,
        System.Collections.Generic.List<MatchResult> matches)
    {
        ApplyGravity(
            board,
            round
        );

        RefillBoard(
            board,
            round
        );
    }

    private void ApplyGravity(
        BoardModel board,
        ResolutionRound round)
    {
        for (int x = 0; x < board.Width; x++)
        {
            int emptyY = 0;

            for (int y = 0; y < board.Height; y++)
            {
                BoardCell currentCell =
                    board.GetCell(x, y);

                if (!currentCell.IsPlayable)
                {
                    emptyY = y + 1;
                    continue;
                }

                if (currentCell.IsEmpty)
                {
                    continue;
                }

                if (y != emptyY)
                {
                    BoardCell destinationCell =
                        board.GetCell(
                            x,
                            emptyY
                        );

                    if (round != null)
                    {
                        round.GravityMoves.Add(
                            new GravityMove(
                                x,
                                y,
                                x,
                                emptyY
                            )
                        );
                    }

                    destinationCell.Piece =
                        currentCell.Piece;

                    currentCell.Piece =
                        null;
                }

                emptyY++;
            }
        }
    }

    private void RefillBoard(
        BoardModel board,
        ResolutionRound round)
    {
        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                BoardCell cell =
                    board.GetCell(x, y);

                if (!cell.IsPlayable || !cell.IsEmpty)
                {
                    continue;
                }

                PieceData newPiece =
                    board.GetRandomPiece();

                cell.Piece =
                    newPiece;

                if (round != null)
                {
                    round.Spawns.Add(
                        new SpawnRecord(
                            x,
                            y,
                            newPiece.Color,
                            newPiece.Special
                        )
                    );
                }
            }
        }
    }
}