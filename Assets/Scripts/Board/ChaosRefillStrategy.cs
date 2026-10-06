using System;

public class ChaosRefillStrategy :
    IRefillStrategy
{
    private Random random;

    private GravityDirection direction;

    public ChaosRefillStrategy()
    {
        random =
            new Random();

        direction =
            GravityDirection.Down;
    }

    public void BeginResolution()
    {

    }

    private void ChooseRandomDirection()
    {
        Array values =
            Enum.GetValues(
                typeof(GravityDirection)
            );

        direction =
            (GravityDirection)
            values.GetValue(
                random.Next(values.Length)
            );

        UnityEngine.Debug.Log(
            $"Chaos gravity: {direction}"
        );
    }

    public void Apply(
    BoardModel board,
    ResolutionRound round,
    System.Collections.Generic.List<MatchResult> matches)
    {
        ChooseRandomDirection();

        switch (direction)
        {
            case GravityDirection.Down:
                ApplyDown(
                    board,
                    round
                );
                break;

            case GravityDirection.Up:
                ApplyUp(
                    board,
                    round
                );
                break;

            case GravityDirection.Left:
                ApplyLeft(
                    board,
                    round
                );
                break;

            case GravityDirection.Right:
                ApplyRight(
                    board,
                    round
                );
                break;
        }

        RefillBoard(
            board,
            round
        );
    }

    private void ApplyDown(
    BoardModel board,
    ResolutionRound round)
    {
        for (int x = 0; x < board.Width; x++)
        {
            int emptyY = 0;

            for (int y = 0; y < board.Height; y++)
            {
                BoardCell current =
                    board.GetCell(x, y);

                if (!current.IsPlayable)
                {
                    emptyY = y + 1;
                    continue;
                }

                if (current.IsEmpty)
                {
                    continue;
                }

                if (y != emptyY)
                {
                    BoardCell destination =
                        board.GetCell(
                            x,
                            emptyY
                        );

                    RecordMove(
                        round,
                        x,
                        y,
                        x,
                        emptyY
                    );

                    destination.Piece =
                        current.Piece;

                    current.Piece =
                        null;
                }

                emptyY++;
            }
        }
    }

    private void ApplyUp(
    BoardModel board,
    ResolutionRound round)
    {
        for (int x = 0; x < board.Width; x++)
        {
            int emptyY =
                board.Height - 1;

            for (
                int y = board.Height - 1;
                y >= 0;
                y--
            )
            {
                BoardCell current =
                    board.GetCell(x, y);

                if (!current.IsPlayable)
                {
                    emptyY = y - 1;
                    continue;
                }

                if (current.IsEmpty)
                {
                    continue;
                }

                if (y != emptyY)
                {
                    BoardCell destination =
                        board.GetCell(
                            x,
                            emptyY
                        );

                    RecordMove(
                        round,
                        x,
                        y,
                        x,
                        emptyY
                    );

                    destination.Piece =
                        current.Piece;

                    current.Piece =
                        null;
                }

                emptyY--;
            }
        }
    }

    private void ApplyLeft(
    BoardModel board,
    ResolutionRound round)
    {
        for (int y = 0; y < board.Height; y++)
        {
            int emptyX = 0;

            for (int x = 0; x < board.Width; x++)
            {
                BoardCell current =
                    board.GetCell(x, y);

                if (!current.IsPlayable)
                {
                    emptyX = x + 1;
                    continue;
                }

                if (current.IsEmpty)
                {
                    continue;
                }

                if (x != emptyX)
                {
                    BoardCell destination =
                        board.GetCell(
                            emptyX,
                            y
                        );

                    RecordMove(
                        round,
                        x,
                        y,
                        emptyX,
                        y
                    );

                    destination.Piece =
                        current.Piece;

                    current.Piece =
                        null;
                }

                emptyX++;
            }
        }
    }

    private void ApplyRight(
    BoardModel board,
    ResolutionRound round)
    {
        for (int y = 0; y < board.Height; y++)
        {
            int emptyX =
                board.Width - 1;

            for (
                int x = board.Width - 1;
                x >= 0;
                x--
            )
            {
                BoardCell current =
                    board.GetCell(x, y);

                if (!current.IsPlayable)
                {
                    emptyX = x - 1;
                    continue;
                }

                if (current.IsEmpty)
                {
                    continue;
                }

                if (x != emptyX)
                {
                    BoardCell destination =
                        board.GetCell(
                            emptyX,
                            y
                        );

                    RecordMove(
                        round,
                        x,
                        y,
                        emptyX,
                        y
                    );

                    destination.Piece =
                        current.Piece;

                    current.Piece =
                        null;
                }

                emptyX--;
            }
        }
    }

    private void RecordMove(
    ResolutionRound round,
    int fromX,
    int fromY,
    int toX,
    int toY)
    {
        if (round == null)
        {
            return;
        }

        round.GravityMoves.Add(
            new GravityMove(
                fromX,
                fromY,
                toX,
                toY
            )
        );
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
                            newPiece.Special,
                            direction
                        )
                    );
                }
            }
        }
    }
}