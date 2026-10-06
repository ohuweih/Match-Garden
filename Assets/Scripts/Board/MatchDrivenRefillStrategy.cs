using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MatchDrivenRefillStrategy :
    IRefillStrategy
{
    private ClassicRefillStrategy fallbackStrategy;

    public MatchDrivenRefillStrategy()
    {
        fallbackStrategy =
            new ClassicRefillStrategy();
    }

    public void BeginResolution()
    {
        // Normal mode doesn't need one global
        // direction for the entire resolution.
    }

    public void Apply(
    BoardModel board,
    ResolutionRound round,
    List<MatchResult> matches)
    {
        if (
            matches == null ||
            matches.Count == 0
        )
        {
            UseClassic(
                board,
                round
            );

            return;
        }

        // For this milestone every match must still
        // be a straight match.
        foreach (MatchResult match in matches)
        {
            if (!IsSupportedMatch(match))
            {
                UseClassic(
                    board,
                    round
                );

                return;
            }
        }

        // If specials activated and cleared cells that
        // don't belong to these matches, our simple
        // local planner does not own the whole clear.
        if (
            round != null &&
            HasExtraClears(
                round,
                matches
            )
        )
        {
            UseClassic(
                board,
                round
            );

            return;
        }

        LocalCollapsePlan plan =
    BuildMultiMatchPlan(
        board,
        matches
    );

        if (plan == null)
        {
            UseClassic(
                board,
                round
            );

            return;
        }

        ApplyPlan(
            board,
            round,
            plan
        );
    }

    private void UseClassic(
        BoardModel board,
        ResolutionRound round)
    {
        fallbackStrategy.Apply(
            board,
            round,
            null
        );
    }

    private bool IsHorizontal(
        MatchResult match)
    {
        int y =
            match.Cells[0].Y;

        foreach (BoardCell cell in match.Cells)
        {
            if (cell.Y != y)
            {
                return false;
            }
        }

        return true;
    }

    private bool IsVertical(
        MatchResult match)
    {
        int x =
            match.Cells[0].X;

        foreach (BoardCell cell in match.Cells)
        {
            if (cell.X != x)
            {
                return false;
            }
        }

        return true;
    }

    private void ApplyHorizontalMatch(
    BoardModel board,
    ResolutionRound round,
    MatchResult match)
    {
        int y =
            match.Cells[0].Y;

        int matchMinX =
            match.Cells.Min(
                cell => cell.X
            );

        int matchMaxX =
            match.Cells.Max(
                cell => cell.X
            );

        float center =
            (matchMinX + matchMaxX) / 2f;

        List<BoardCell> emptyCells =
            new List<BoardCell>();

        foreach (BoardCell cell in match.Cells)
        {
            // THIS is the important difference.
            //
            // A newly-created special is not empty,
            // so it is NOT treated as part of the gap.
            if (cell.IsEmpty)
            {
                emptyCells.Add(cell);
            }
        }

        List<BoardCell> leftTargets =
            emptyCells
                .Where(cell => cell.X <= center)
                .OrderBy(cell => cell.X)
                .ToList();

        List<BoardCell> rightTargets =
            emptyCells
                .Where(cell => cell.X > center)
                .OrderBy(cell => cell.X)
                .ToList();

        if (leftTargets.Count > 0)
        {
            FillFromLeft(
                board,
                round,
                y,
                matchMinX,
                leftTargets
            );
        }

        if (rightTargets.Count > 0)
        {
            FillFromRight(
                board,
                round,
                y,
                matchMaxX,
                rightTargets
            );
        }

        RefillHorizontalEdges(
            board,
            round,
            y,
            matchMinX,
            matchMaxX
        );
    }

    private void FillFromLeft(
    BoardModel board,
    ResolutionRound round,
    int y,
    int matchMinX,
    List<BoardCell> targets)
    {
        if (targets.Count == 0)
        {
            return;
        }

        List<BoardCell> sources =
            new List<BoardCell>();

        // IMPORTANT:
        // Start OUTSIDE the original match.
        //
        // Never pull a surviving special that
        // exists inside the match area.
        for (
            int x = matchMinX - 1;
            x >= 0 &&
            sources.Count < targets.Count;
            x--
        )
        {
            BoardCell cell =
                board.GetCell(x, y);

            if (!cell.IsPlayable) break;

            if (!cell.IsPlayable || !cell.IsEmpty)
            {
                sources.Add(cell);
            }
        }

        // We searched nearest-to-farthest.
        // Reverse to preserve visual ordering.
        sources.Reverse();

        int missingSources =
            targets.Count - sources.Count;

        for (int i = 0; i < sources.Count; i++)
        {
            BoardCell source =
                sources[i];

            BoardCell destination =
                targets[
                    i + missingSources
                ];

            destination.Piece =
                source.Piece;

            source.Piece =
                null;

            RecordMove(
                round,
                source.X,
                source.Y,
                destination.X,
                destination.Y
            );
        }
    }

    private void FillFromRight(
    BoardModel board,
    ResolutionRound round,
    int y,
    int matchMaxX,
    List<BoardCell> targets)
    {
        if (targets.Count == 0)
        {
            return;
        }

        List<BoardCell> sources =
            new List<BoardCell>();

        // Again, only use pieces OUTSIDE
        // the original match.
        for (
            int x = matchMaxX + 1;
            x < board.Width &&
            sources.Count < targets.Count;
            x++
        )
        {
            BoardCell cell =
                board.GetCell(x, y);

            if (!cell.IsPlayable) break;

            if (!cell.IsPlayable || !cell.IsEmpty)
            {
                sources.Add(cell);
            }
        }

        int sourceCount =
            sources.Count;

        for (int i = 0; i < sourceCount; i++)
        {
            BoardCell source =
                sources[i];

            BoardCell destination =
                targets[i];

            destination.Piece =
                source.Piece;

            source.Piece =
                null;

            RecordMove(
                round,
                source.X,
                source.Y,
                destination.X,
                destination.Y
            );
        }
    }

    private void RefillHorizontalEdges(
    BoardModel board,
    ResolutionRound round,
    int y,
    int matchMinX,
    int matchMaxX)
    {
        float center =
            (matchMinX + matchMaxX) / 2f;

        for (int x = 0; x < board.Width; x++)
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

            GravityDirection entryDirection;

            if (x <= center)
            {
                // Gravity points RIGHT,
                // therefore visual spawn enters
                // from the LEFT.
                entryDirection =
                    GravityDirection.Right;
            }
            else
            {
                // Gravity LEFT means enter
                // from the RIGHT.
                entryDirection =
                    GravityDirection.Left;
            }

            if (round != null)
            {
                round.Spawns.Add(
                    new SpawnRecord(
                        x,
                        y,
                        newPiece.Color,
                        newPiece.Special,
                        entryDirection
                    )
                );
            }
        }
    }

    private void ApplyVerticalMatch(
    BoardModel board,
    ResolutionRound round,
    MatchResult match)
    {
        int x =
            match.Cells[0].X;

        int matchMinY =
            match.Cells.Min(
                cell => cell.Y
            );

        int matchMaxY =
            match.Cells.Max(
                cell => cell.Y
            );

        float center =
            (matchMinY + matchMaxY) / 2f;

        List<BoardCell> emptyCells =
            new List<BoardCell>();

        foreach (BoardCell cell in match.Cells)
        {
            if (cell.IsEmpty)
            {
                emptyCells.Add(cell);
            }
        }

        List<BoardCell> bottomTargets =
            emptyCells
                .Where(cell => cell.Y <= center)
                .OrderBy(cell => cell.Y)
                .ToList();

        List<BoardCell> topTargets =
            emptyCells
                .Where(cell => cell.Y > center)
                .OrderBy(cell => cell.Y)
                .ToList();

        if (bottomTargets.Count > 0)
        {
            FillFromBottom(
                board,
                round,
                x,
                matchMinY,
                bottomTargets
            );
        }

        if (topTargets.Count > 0)
        {
            FillFromTop(
                board,
                round,
                x,
                matchMaxY,
                topTargets
            );
        }

        RefillVerticalEdges(
            board,
            round,
            x,
            matchMinY,
            matchMaxY
        );
    }

    private void FillFromBottom(
    BoardModel board,
    ResolutionRound round,
    int x,
    int matchMinY,
    List<BoardCell> targets)
    {
        if (targets.Count == 0)
        {
            return;
        }

        List<BoardCell> sources =
            new List<BoardCell>();

        for (
            int y = matchMinY - 1;
            y >= 0 &&
            sources.Count < targets.Count;
            y--
        )
        {
            BoardCell cell =
                board.GetCell(x, y);

            if (!cell.IsPlayable) break;

            if (!cell.IsPlayable || !cell.IsEmpty)
            {
                sources.Add(cell);
            }
        }

        sources.Reverse();

        int missingSources =
            targets.Count - sources.Count;

        for (int i = 0; i < sources.Count; i++)
        {
            BoardCell source =
                sources[i];

            BoardCell destination =
                targets[
                    i + missingSources
                ];

            destination.Piece =
                source.Piece;

            source.Piece =
                null;

            RecordMove(
                round,
                source.X,
                source.Y,
                destination.X,
                destination.Y
            );
        }
    }

    private void FillFromTop(
    BoardModel board,
    ResolutionRound round,
    int x,
    int matchMaxY,
    List<BoardCell> targets)
    {
        if (targets.Count == 0)
        {
            return;
        }

        List<BoardCell> sources =
            new List<BoardCell>();

        for (
            int y = matchMaxY + 1;
            y < board.Height &&
            sources.Count < targets.Count;
            y++
        )
        {
            BoardCell cell =
                board.GetCell(x, y);

            if (!cell.IsPlayable) break;

            if (!cell.IsPlayable || !cell.IsEmpty)
            {
                sources.Add(cell);
            }
        }

        for (int i = 0; i < sources.Count; i++)
        {
            BoardCell source =
                sources[i];

            BoardCell destination =
                targets[i];

            destination.Piece =
                source.Piece;

            source.Piece =
                null;

            RecordMove(
                round,
                source.X,
                source.Y,
                destination.X,
                destination.Y
            );
        }
    }

    private void RefillVerticalEdges(
    BoardModel board,
    ResolutionRound round,
    int x,
    int matchMinY,
    int matchMaxY)
    {
        float center =
            (matchMinY + matchMaxY) / 2f;

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

            GravityDirection entryDirection;

            if (y <= center)
            {
                // Collapse UP means new piece
                // visually enters from below.
                entryDirection =
                    GravityDirection.Up;
            }
            else
            {
                // Collapse DOWN means enter
                // from above.
                entryDirection =
                    GravityDirection.Down;
            }

            if (round != null)
            {
                round.Spawns.Add(
                    new SpawnRecord(
                        x,
                        y,
                        newPiece.Color,
                        newPiece.Special,
                        entryDirection
                    )
                );
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

    private class PlannedMove
    {
        public BoardCell Source;
        public BoardCell Destination;
        public PieceData Piece;

        public PlannedMove(
            BoardCell source,
            BoardCell destination)
        {
            Source = source;
            Destination = destination;

            Piece = source.Piece;
        }
    }

    private class PlannedSpawn
    {
        public BoardCell Cell;
        public GravityDirection Direction;

        public PlannedSpawn(
            BoardCell cell,
            GravityDirection direction)
        {
            Cell = cell;
            Direction = direction;
        }
    }

    private class LocalCollapsePlan
    {
        public List<PlannedMove> Moves =
            new List<PlannedMove>();

        public List<PlannedSpawn> Spawns =
            new List<PlannedSpawn>();
    }

    private bool IsSupportedMatch(
        MatchResult match)
    {
        return
            match.Shape == MatchShape.Three ||
            match.Shape == MatchShape.FourHorizontal ||
            match.Shape == MatchShape.FourVertical ||
            match.Shape == MatchShape.FivePlusHorizontal ||
            match.Shape == MatchShape.FivePlusVertical ||
            match.Shape == MatchShape.Square ||
            match.Shape == MatchShape.TShape ||
            match.Shape == MatchShape.LShape;
    }

    private bool HasExtraClears(
    ResolutionRound round,
    List<MatchResult> matches)
    {
        HashSet<BoardCell> matchCells =
            new HashSet<BoardCell>();

        foreach (MatchResult match in matches)
        {
            foreach (BoardCell cell in match.Cells)
            {
                matchCells.Add(cell);
            }
        }

        foreach (BoardCell clearedCell in round.ClearedCells)
        {
            if (!matchCells.Contains(clearedCell))
            {
                return true;
            }
        }

        return false;
    }

    private LocalCollapsePlan BuildMultiMatchPlan(
    BoardModel board,
    List<MatchResult> matches)
    {
        LocalCollapsePlan rawPlan =
            new LocalCollapsePlan();

        HashSet<BoardCell> protectedMatchCells =
            new HashSet<BoardCell>();

        foreach (MatchResult match in matches)
        {
            foreach (BoardCell cell in match.Cells)
            {
                protectedMatchCells.Add(cell);
            }
        }

        // -----------------------------------------
        // BUILD EVERY LOCAL PLAN FIRST.
        //
        // We intentionally allow conflicts here.
        // They will be resolved afterward.
        // -----------------------------------------

        foreach (MatchResult match in matches)
        {
            LocalCollapsePlan localPlan;

            if (
                match.Shape == MatchShape.TShape ||
                match.Shape == MatchShape.LShape
            )
            {
                localPlan =
                    BuildTLPlan(
                        board,
                        match,
                        protectedMatchCells
                    );
            }
            else if (match.Shape == MatchShape.Square)
            {
                localPlan =
                    BuildSquarePlan(
                        board,
                        match,
                        protectedMatchCells
                    );
            }
            else if (IsHorizontal(match))
            {
                localPlan =
                    BuildHorizontalPlan(
                        board,
                        match,
                        protectedMatchCells
                    );
            }
            else if (IsVertical(match))
            {
                localPlan =
                    BuildVerticalPlan(
                        board,
                        match,
                        protectedMatchCells
                    );
            }
            else
            {
                return null;
            }

            rawPlan.Moves.AddRange(
                localPlan.Moves
            );

            rawPlan.Spawns.AddRange(
                localPlan.Spawns
            );
        }

        // Resolve overlaps BEFORE touching BoardModel.
        return ExtendCollapseToEdges(
            board,
            ResolvePlanConflicts(rawPlan),
            protectedMatchCells
        );
    }

    private LocalCollapsePlan ExtendCollapseToEdges(
        BoardModel board,
        LocalCollapsePlan plan,
        HashSet<BoardCell> protectedMatchCells)
    {
        // Track each piece by its original cell while extending the chosen pulls.
        // The real board stays untouched, so each piece gets one animation move.
        var origins = new Dictionary<BoardCell, BoardCell>();
        for (int y = 0; y < board.Height; y++)
            for (int x = 0; x < board.Width; x++)
            {
                var cell = board.GetCell(x, y);
                if (!cell.IsEmpty) origins.Add(cell, cell);
            }

        var directions = new Dictionary<BoardCell, GravityDirection>();
        foreach (var move in plan.Moves) origins.Remove(move.Source);
        foreach (var move in plan.Moves)
        {
            origins.Add(move.Destination, move.Source);
            directions.Add(move.Source, GetMoveDirection(move));
        }

        var holes = new Dictionary<BoardCell, GravityDirection>();
        foreach (var spawn in plan.Spawns) holes[spawn.Cell] = spawn.Direction;

        // Start with the innermost hole on each side. Compact all remaining
        // pieces toward it, carrying the empty space out to the section edge.
        foreach (var spawn in plan.Spawns
            .OrderBy(s => s.Direction)
            .ThenByDescending(s => InwardPosition(s.Cell, s.Direction)))
        {
            if (origins.ContainsKey(spawn.Cell)) continue;
            int dx = 0, dy = 0;
            switch (spawn.Direction)
            {
                case GravityDirection.Right: dx = -1; break;
                case GravityDirection.Left: dx = 1; break;
                case GravityDirection.Up: dy = -1; break;
                case GravityDirection.Down: dy = 1; break;
            }

            var lane = new List<BoardCell>();
            int x = spawn.Cell.X, y = spawn.Cell.Y;
            while (board.IsPlayable(x, y))
            {
                var cell = board.GetCell(x, y);
                // Preserve each match's special and its own inward collapse.
                if (cell != spawn.Cell && protectedMatchCells.Contains(cell)) break;
                // Crossing pulls cannot turn a piece or move it twice in
                // different directions within a single animation round.
                if (origins.TryGetValue(cell, out var origin) &&
                    directions.TryGetValue(origin, out var direction) &&
                    direction != spawn.Direction) break;
                lane.Add(cell);
                x += dx;
                y += dy;
            }

            int write = 0;
            foreach (var source in lane)
            {
                if (!origins.TryGetValue(source, out var origin)) continue;
                var destination = lane[write++];
                if (source == destination) continue;
                origins.Remove(source);
                origins.Add(destination, origin);
                directions[origin] = spawn.Direction;
            }
            for (int i = write; i < lane.Count; i++) holes[lane[i]] = spawn.Direction;
        }

        var extended = new LocalCollapsePlan();
        foreach (var entry in origins)
            if (entry.Key != entry.Value)
                extended.Moves.Add(new PlannedMove(entry.Value, entry.Key));
        foreach (var hole in holes)
            if (!origins.ContainsKey(hole.Key))
                extended.Spawns.Add(new PlannedSpawn(hole.Key, hole.Value));
        return extended;
    }

    private int InwardPosition(BoardCell cell, GravityDirection direction)
    {
        switch (direction)
        {
            case GravityDirection.Right: return cell.X;
            case GravityDirection.Left: return -cell.X;
            case GravityDirection.Up: return cell.Y;
            default: return -cell.Y;
        }
    }


    private int GetMoveDistance(
    PlannedMove move)
    {
        return
            Mathf.Abs(
                move.Source.X -
                move.Destination.X
            )
            +
            Mathf.Abs(
                move.Source.Y -
                move.Destination.Y
            );
    }

    private GravityDirection GetMoveDirection(
    PlannedMove move)
    {
        if (
            move.Destination.X >
            move.Source.X
        )
        {
            return GravityDirection.Right;
        }

        if (
            move.Destination.X <
            move.Source.X
        )
        {
            return GravityDirection.Left;
        }

        if (
            move.Destination.Y >
            move.Source.Y
        )
        {
            return GravityDirection.Up;
        }

        return GravityDirection.Down;
    }

    private LocalCollapsePlan ResolvePlanConflicts(
    LocalCollapsePlan rawPlan)
    {
        LocalCollapsePlan resolved =
            new LocalCollapsePlan();

        // Sort closest moves first.
        List<PlannedMove> candidates =
            rawPlan.Moves
                .OrderBy(
                    move =>
                        GetMoveDistance(move)
                )
                .ThenBy(
                    move =>
                        move.Destination.Y
                )
                .ThenBy(
                    move =>
                        move.Destination.X
                )
                .ThenBy(
                    move =>
                        move.Source.Y
                )
                .ThenBy(
                    move =>
                        move.Source.X
                )
                .ToList();

        HashSet<BoardCell> reservedSources =
            new HashSet<BoardCell>();

        HashSet<BoardCell> reservedDestinations =
            new HashSet<BoardCell>();

        List<PlannedMove> rejectedMoves =
            new List<PlannedMove>();

        // -----------------------------------------
        // CHOOSE WINNING MOVEMENTS
        // -----------------------------------------

        foreach (PlannedMove move in candidates)
        {
            bool sourceConflict =
                reservedSources.Contains(
                    move.Source
                );

            bool destinationConflict =
                reservedDestinations.Contains(
                    move.Destination
                );

            // Also prevent two simultaneous movements
            // from crossing through each other's
            // source/destination bookkeeping.
            bool sourceIsDestination =
                reservedDestinations.Contains(
                    move.Source
                );

            bool destinationIsSource =
                reservedSources.Contains(
                    move.Destination
                );

            if (
                sourceConflict ||
                destinationConflict ||
                sourceIsDestination ||
                destinationIsSource
            )
            {
                rejectedMoves.Add(
                    move
                );

                continue;
            }

            reservedSources.Add(
                move.Source
            );

            reservedDestinations.Add(
                move.Destination
            );

            resolved.Moves.Add(
                move
            );
        }

        // -----------------------------------------
        // REBUILD SPAWN REQUESTS
        //
        // Do NOT blindly copy rawPlan.Spawns.
        // Some belonged to movements that lost.
        // -----------------------------------------

        HashSet<BoardCell> allMoveSources =
            new HashSet<BoardCell>();

        foreach (PlannedMove move in rawPlan.Moves)
        {
            allMoveSources.Add(
                move.Source
            );
        }

        HashSet<BoardCell> acceptedSources =
            new HashSet<BoardCell>();

        foreach (PlannedMove move in resolved.Moves)
        {
            acceptedSources.Add(
                move.Source
            );
        }

        Dictionary<BoardCell, PlannedSpawn> spawnByCell =
            new Dictionary<BoardCell, PlannedSpawn>();

        foreach (PlannedSpawn spawn in rawPlan.Spawns)
        {
            // -------------------------------------
            // Spawn attached to a movement source.
            //
            // Keep it ONLY if that source actually
            // moved.
            // -------------------------------------

            if (allMoveSources.Contains(spawn.Cell))
            {
                if (!acceptedSources.Contains(spawn.Cell))
                {
                    continue;
                }
            }

            // Don't spawn into a destination that
            // an accepted piece is moving into.
            if (
                reservedDestinations.Contains(
                    spawn.Cell
                )
            )
            {
                continue;
            }

            if (
                !spawnByCell.ContainsKey(
                    spawn.Cell
                )
            )
            {
                spawnByCell.Add(
                    spawn.Cell,
                    spawn
                );
            }
        }

        // -----------------------------------------
        // A REJECTED MOVE still had an empty
        // destination that needs to be filled.
        //
        // Instead of stealing another piece,
        // spawn directly into that destination.
        // -----------------------------------------

        foreach (PlannedMove rejected in rejectedMoves)
        {
            BoardCell destination =
                rejected.Destination;

            // Another winning movement may already
            // own this destination.
            if (
                reservedDestinations.Contains(
                    destination
                )
            )
            {
                continue;
            }

            if (
                spawnByCell.ContainsKey(
                    destination
                )
            )
            {
                continue;
            }

            GravityDirection direction =
                GetMoveDirection(
                    rejected
                );

            spawnByCell.Add(
                destination,
                new PlannedSpawn(
                    destination,
                    direction
                )
            );
        }

        foreach (
            PlannedSpawn spawn
            in spawnByCell.Values
        )
        {
            resolved.Spawns.Add(
                spawn
            );
        }

        Debug.Log(
            $"Normal conflict resolution: " +
            $"{rawPlan.Moves.Count} requested moves, " +
            $"{resolved.Moves.Count} accepted, " +
            $"{rejectedMoves.Count} redirected to refill."
        );

        return resolved;
    }

    private LocalCollapsePlan BuildTLPlan(
    BoardModel board,
    MatchResult match,
    HashSet<BoardCell> protectedMatchCells)
    {
        LocalCollapsePlan plan =
            new LocalCollapsePlan();

        // Find the cell that acts as the intersection/corner.
        BoardCell center =
            FindTLIntersection(match);

        if (center == null)
        {
            return null;
        }

        List<BoardCell> horizontalCells =
            match.Cells
                .Where(cell => cell.Y == center.Y)
                .OrderBy(cell => cell.X)
                .ToList();

        List<BoardCell> verticalCells =
            match.Cells
                .Where(cell => cell.X == center.X)
                .OrderBy(cell => cell.Y)
                .ToList();

        HashSet<BoardCell> reservedSources =
            new HashSet<BoardCell>();

        BuildTLHorizontalArm(
            board,
            plan,
            horizontalCells,
            protectedMatchCells,
            reservedSources
        );

        BuildTLVerticalArm(
            board,
            plan,
            verticalCells,
            protectedMatchCells,
            reservedSources
        );

        return plan;
    }

    private BoardCell FindTLIntersection(
    MatchResult match)
    {
        foreach (BoardCell candidate in match.Cells)
        {
            int sameRowCount =
                match.Cells.Count(
                    cell => cell.Y == candidate.Y
                );

            int sameColumnCount =
                match.Cells.Count(
                    cell => cell.X == candidate.X
                );

            if (
                sameRowCount >= 3 &&
                sameColumnCount >= 3
            )
            {
                return candidate;
            }
        }

        return null;
    }

    private void BuildTLHorizontalArm(
    BoardModel board,
    LocalCollapsePlan plan,
    List<BoardCell> armCells,
    HashSet<BoardCell> protectedMatchCells,
    HashSet<BoardCell> reservedSources)
    {
        if (armCells.Count < 3)
        {
            return;
        }

        int y =
            armCells[0].Y;

        int minX =
            armCells.Min(cell => cell.X);

        int maxX =
            armCells.Max(cell => cell.X);

        float center =
            (minX + maxX) / 2f;

        List<BoardCell> emptyCells =
            armCells
                .Where(cell => cell.IsEmpty)
                .ToList();

        List<BoardCell> leftTargets =
            emptyCells
                .Where(cell => cell.X <= center)
                .OrderBy(cell => cell.X)
                .ToList();

        List<BoardCell> rightTargets =
            emptyCells
                .Where(cell => cell.X > center)
                .OrderBy(cell => cell.X)
                .ToList();

        BuildTLLeftSide(
            board,
            plan,
            y,
            minX,
            leftTargets,
            protectedMatchCells,
            reservedSources
        );

        BuildTLRightSide(
            board,
            plan,
            y,
            maxX,
            rightTargets,
            protectedMatchCells,
            reservedSources
        );
    }

    private void BuildTLVerticalArm(
    BoardModel board,
    LocalCollapsePlan plan,
    List<BoardCell> armCells,
    HashSet<BoardCell> protectedMatchCells,
    HashSet<BoardCell> reservedSources)
    {
        if (armCells.Count < 3)
        {
            return;
        }

        int x =
            armCells[0].X;

        int minY =
            armCells.Min(cell => cell.Y);

        int maxY =
            armCells.Max(cell => cell.Y);

        float center =
            (minY + maxY) / 2f;

        List<BoardCell> emptyCells =
            armCells
                .Where(cell => cell.IsEmpty)
                .ToList();

        List<BoardCell> bottomTargets =
            emptyCells
                .Where(cell => cell.Y <= center)
                .OrderBy(cell => cell.Y)
                .ToList();

        List<BoardCell> topTargets =
            emptyCells
                .Where(cell => cell.Y > center)
                .OrderBy(cell => cell.Y)
                .ToList();

        BuildTLBottomSide(
            board,
            plan,
            x,
            minY,
            bottomTargets,
            protectedMatchCells,
            reservedSources
        );

        BuildTLTopSide(
            board,
            plan,
            x,
            maxY,
            topTargets,
            protectedMatchCells,
            reservedSources
        );
    }

    private void BuildTLLeftSide(
    BoardModel board,
    LocalCollapsePlan plan,
    int y,
    int minX,
    List<BoardCell> targets,
    HashSet<BoardCell> protectedMatchCells,
    HashSet<BoardCell> reservedSources)
    {
        List<BoardCell> sources =
            new List<BoardCell>();

        for (
            int x = minX - 1;
            x >= 0 &&
            sources.Count < targets.Count;
            x--
        )
        {
            BoardCell source =
                board.GetCell(x, y);

            if (!source.IsPlayable) break;

            if (
                source.IsEmpty ||
                protectedMatchCells.Contains(source) ||
                reservedSources.Contains(source)
            )
            {
                continue;
            }

            sources.Add(source);
        }

        sources.Reverse();

        int missing =
            targets.Count - sources.Count;

        for (int i = 0; i < sources.Count; i++)
        {
            BoardCell source =
                sources[i];

            BoardCell destination =
                targets[i + missing];

            reservedSources.Add(source);

            plan.Moves.Add(
                new PlannedMove(
                    source,
                    destination
                )
            );

            plan.Spawns.Add(
                new PlannedSpawn(
                    source,
                    GravityDirection.Right
                )
            );
        }

        for (int i = 0; i < missing; i++)
        {
            plan.Spawns.Add(
                new PlannedSpawn(
                    targets[i],
                    GravityDirection.Right
                )
            );
        }
    }

    private void BuildTLRightSide(
    BoardModel board,
    LocalCollapsePlan plan,
    int y,
    int maxX,
    List<BoardCell> targets,
    HashSet<BoardCell> protectedMatchCells,
    HashSet<BoardCell> reservedSources)
    {
        List<BoardCell> sources =
            new List<BoardCell>();

        for (
            int x = maxX + 1;
            x < board.Width &&
            sources.Count < targets.Count;
            x++
        )
        {
            BoardCell source =
                board.GetCell(x, y);

            if (!source.IsPlayable) break;

            if (
                source.IsEmpty ||
                protectedMatchCells.Contains(source) ||
                reservedSources.Contains(source)
            )
            {
                continue;
            }

            sources.Add(source);
        }

        for (int i = 0; i < sources.Count; i++)
        {
            BoardCell source =
                sources[i];

            reservedSources.Add(source);

            plan.Moves.Add(
                new PlannedMove(
                    source,
                    targets[i]
                )
            );

            plan.Spawns.Add(
                new PlannedSpawn(
                    source,
                    GravityDirection.Left
                )
            );
        }

        for (
            int i = sources.Count;
            i < targets.Count;
            i++
        )
        {
            plan.Spawns.Add(
                new PlannedSpawn(
                    targets[i],
                    GravityDirection.Left
                )
            );
        }
    }

    private void BuildTLBottomSide(
    BoardModel board,
    LocalCollapsePlan plan,
    int x,
    int minY,
    List<BoardCell> targets,
    HashSet<BoardCell> protectedMatchCells,
    HashSet<BoardCell> reservedSources)
    {
        List<BoardCell> sources =
            new List<BoardCell>();

        for (
            int y = minY - 1;
            y >= 0 &&
            sources.Count < targets.Count;
            y--
        )
        {
            BoardCell source =
                board.GetCell(x, y);

            if (!source.IsPlayable) break;

            if (
                source.IsEmpty ||
                protectedMatchCells.Contains(source) ||
                reservedSources.Contains(source)
            )
            {
                continue;
            }

            sources.Add(source);
        }

        sources.Reverse();

        int missing =
            targets.Count - sources.Count;

        for (int i = 0; i < sources.Count; i++)
        {
            BoardCell source =
                sources[i];

            BoardCell destination =
                targets[i + missing];

            reservedSources.Add(source);

            plan.Moves.Add(
                new PlannedMove(
                    source,
                    destination
                )
            );

            plan.Spawns.Add(
                new PlannedSpawn(
                    source,
                    GravityDirection.Up
                )
            );
        }

        for (int i = 0; i < missing; i++)
        {
            plan.Spawns.Add(
                new PlannedSpawn(
                    targets[i],
                    GravityDirection.Up
                )
            );
        }
    }

    private void BuildTLTopSide(
    BoardModel board,
    LocalCollapsePlan plan,
    int x,
    int maxY,
    List<BoardCell> targets,
    HashSet<BoardCell> protectedMatchCells,
    HashSet<BoardCell> reservedSources)
    {
        List<BoardCell> sources =
            new List<BoardCell>();

        for (
            int y = maxY + 1;
            y < board.Height &&
            sources.Count < targets.Count;
            y++
        )
        {
            BoardCell source =
                board.GetCell(x, y);

            if (!source.IsPlayable) break;

            if (
                source.IsEmpty ||
                protectedMatchCells.Contains(source) ||
                reservedSources.Contains(source)
            )
            {
                continue;
            }

            sources.Add(source);
        }

        for (int i = 0; i < sources.Count; i++)
        {
            BoardCell source =
                sources[i];

            reservedSources.Add(source);

            plan.Moves.Add(
                new PlannedMove(
                    source,
                    targets[i]
                )
            );

            plan.Spawns.Add(
                new PlannedSpawn(
                    source,
                    GravityDirection.Down
                )
            );
        }

        for (
            int i = sources.Count;
            i < targets.Count;
            i++
        )
        {
            plan.Spawns.Add(
                new PlannedSpawn(
                    targets[i],
                    GravityDirection.Down
                )
            );
        }
    }

    private LocalCollapsePlan BuildHorizontalPlan(
    BoardModel board,
    MatchResult match,
    HashSet<BoardCell> protectedMatchCells)
    {
        LocalCollapsePlan plan =
            new LocalCollapsePlan();

        int y =
            match.Cells[0].Y;

        int minX =
            match.Cells.Min(
                cell => cell.X
            );

        int maxX =
            match.Cells.Max(
                cell => cell.X
            );

        float center =
            (minX + maxX) / 2f;

        List<BoardCell> emptyCells =
            match.Cells
                .Where(cell => cell.IsEmpty)
                .ToList();

        List<BoardCell> leftTargets =
            emptyCells
                .Where(cell => cell.X <= center)
                .OrderBy(cell => cell.X)
                .ToList();

        List<BoardCell> rightTargets =
            emptyCells
                .Where(cell => cell.X > center)
                .OrderBy(cell => cell.X)
                .ToList();

        BuildLeftSide(
            board,
            plan,
            y,
            minX,
            leftTargets,
            protectedMatchCells
        );

        BuildRightSide(
            board,
            plan,
            y,
            maxX,
            rightTargets,
            protectedMatchCells
        );

        return plan;
    }

    private void BuildLeftSide(
    BoardModel board,
    LocalCollapsePlan plan,
    int y,
    int matchMinX,
    List<BoardCell> targets,
    HashSet<BoardCell> protectedMatchCells)
    {
        List<BoardCell> sources =
            new List<BoardCell>();

        for (
            int x = matchMinX - 1;
            x >= 0 &&
            sources.Count < targets.Count;
            x--
        )
        {
            BoardCell source =
                board.GetCell(x, y);

            if (!source.IsPlayable) break;

            if (
                source.IsEmpty ||
                protectedMatchCells.Contains(source)
            )
            {
                continue;
            }

            sources.Add(source);
        }

        sources.Reverse();

        int missingSources =
            targets.Count -
            sources.Count;

        for (int i = 0; i < sources.Count; i++)
        {
            BoardCell destination =
                targets[
                    i + missingSources
                ];

            plan.Moves.Add(
                new PlannedMove(
                    sources[i],
                    destination
                )
            );

            // Moving this piece creates a new hole
            // toward the left edge.
            plan.Spawns.Add(
                new PlannedSpawn(
                    sources[i],
                    GravityDirection.Right
                )
            );
        }

        // Not enough existing pieces?
        // Spawn directly into the remaining gap.
        for (
            int i = 0;
            i < missingSources;
            i++
        )
        {
            plan.Spawns.Add(
                new PlannedSpawn(
                    targets[i],
                    GravityDirection.Right
                )
            );
        }
    }

    private void BuildRightSide(
    BoardModel board,
    LocalCollapsePlan plan,
    int y,
    int matchMaxX,
    List<BoardCell> targets,
    HashSet<BoardCell> protectedMatchCells)
    {
        List<BoardCell> sources =
            new List<BoardCell>();

        for (
            int x = matchMaxX + 1;
            x < board.Width &&
            sources.Count < targets.Count;
            x++
        )
        {
            BoardCell source =
                board.GetCell(x, y);

            if (!source.IsPlayable) break;

            if (
                source.IsEmpty ||
                protectedMatchCells.Contains(source)
            )
            {
                continue;
            }

            sources.Add(source);
        }

        for (int i = 0; i < sources.Count; i++)
        {
            plan.Moves.Add(
                new PlannedMove(
                    sources[i],
                    targets[i]
                )
            );

            plan.Spawns.Add(
                new PlannedSpawn(
                    sources[i],
                    GravityDirection.Left
                )
            );
        }

        for (
            int i = sources.Count;
            i < targets.Count;
            i++
        )
        {
            plan.Spawns.Add(
                new PlannedSpawn(
                    targets[i],
                    GravityDirection.Left
                )
            );
        }
    }

    private LocalCollapsePlan BuildVerticalPlan(
    BoardModel board,
    MatchResult match,
    HashSet<BoardCell> protectedMatchCells)
    {
        LocalCollapsePlan plan =
            new LocalCollapsePlan();

        int x =
            match.Cells[0].X;

        int minY =
            match.Cells.Min(
                cell => cell.Y
            );

        int maxY =
            match.Cells.Max(
                cell => cell.Y
            );

        float center =
            (minY + maxY) / 2f;

        List<BoardCell> emptyCells =
            match.Cells
                .Where(cell => cell.IsEmpty)
                .ToList();

        List<BoardCell> bottomTargets =
            emptyCells
                .Where(cell => cell.Y <= center)
                .OrderBy(cell => cell.Y)
                .ToList();

        List<BoardCell> topTargets =
            emptyCells
                .Where(cell => cell.Y > center)
                .OrderBy(cell => cell.Y)
                .ToList();

        BuildBottomSide(
            board,
            plan,
            x,
            minY,
            bottomTargets,
            protectedMatchCells
        );

        BuildTopSide(
            board,
            plan,
            x,
            maxY,
            topTargets,
            protectedMatchCells
        );

        return plan;
    }

    private void BuildBottomSide(
    BoardModel board,
    LocalCollapsePlan plan,
    int x,
    int matchMinY,
    List<BoardCell> targets,
    HashSet<BoardCell> protectedMatchCells)
    {
        List<BoardCell> sources =
            new List<BoardCell>();

        for (
            int y = matchMinY - 1;
            y >= 0 &&
            sources.Count < targets.Count;
            y--
        )
        {
            BoardCell source =
                board.GetCell(x, y);

            if (!source.IsPlayable) break;

            if (
                source.IsEmpty ||
                protectedMatchCells.Contains(source)
            )
            {
                continue;
            }

            sources.Add(source);
        }

        sources.Reverse();

        int missingSources =
            targets.Count -
            sources.Count;

        for (int i = 0; i < sources.Count; i++)
        {
            BoardCell destination =
                targets[
                    i + missingSources
                ];

            plan.Moves.Add(
                new PlannedMove(
                    sources[i],
                    destination
                )
            );

            plan.Spawns.Add(
                new PlannedSpawn(
                    sources[i],
                    GravityDirection.Up
                )
            );
        }

        for (
            int i = 0;
            i < missingSources;
            i++
        )
        {
            plan.Spawns.Add(
                new PlannedSpawn(
                    targets[i],
                    GravityDirection.Up
                )
            );
        }
    }

    private void BuildTopSide(
    BoardModel board,
    LocalCollapsePlan plan,
    int x,
    int matchMaxY,
    List<BoardCell> targets,
    HashSet<BoardCell> protectedMatchCells)
    {
        List<BoardCell> sources =
            new List<BoardCell>();

        for (
            int y = matchMaxY + 1;
            y < board.Height &&
            sources.Count < targets.Count;
            y++
        )
        {
            BoardCell source =
                board.GetCell(x, y);

            if (!source.IsPlayable) break;

            if (
                source.IsEmpty ||
                protectedMatchCells.Contains(source)
            )
            {
                continue;
            }

            sources.Add(source);
        }

        for (int i = 0; i < sources.Count; i++)
        {
            plan.Moves.Add(
                new PlannedMove(
                    sources[i],
                    targets[i]
                )
            );

            plan.Spawns.Add(
                new PlannedSpawn(
                    sources[i],
                    GravityDirection.Down
                )
            );
        }

        for (
            int i = sources.Count;
            i < targets.Count;
            i++
        )
        {
            plan.Spawns.Add(
                new PlannedSpawn(
                    targets[i],
                    GravityDirection.Down
                )
            );
        }
    }

    private void ApplyPlan(
    BoardModel board,
    ResolutionRound round,
    LocalCollapsePlan plan)
    {
        // -----------------------------------------
        // 1. Capture every moving PieceData
        // BEFORE modifying the board.
        // -----------------------------------------

        foreach (PlannedMove move in plan.Moves)
        {
            move.Piece =
                move.Source.Piece;
        }

        // -----------------------------------------
        // 2. Empty ALL sources first.
        // -----------------------------------------

        foreach (PlannedMove move in plan.Moves)
        {
            move.Source.Piece =
                null;
        }

        // -----------------------------------------
        // 3. Fill ALL destinations.
        // -----------------------------------------

        foreach (PlannedMove move in plan.Moves)
        {
            move.Destination.Piece =
                move.Piece;

            if (round != null)
            {
                round.GravityMoves.Add(
                    new GravityMove(
                        move.Source.X,
                        move.Source.Y,
                        move.Destination.X,
                        move.Destination.Y
                    )
                );
            }
        }

        // -----------------------------------------
        // 4. Spawn all replacement pieces.
        // -----------------------------------------

        foreach (PlannedSpawn plannedSpawn in plan.Spawns)
        {
            BoardCell cell =
                plannedSpawn.Cell;

            // Another planned movement might have
            // filled this position.
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
                        cell.X,
                        cell.Y,
                        newPiece.Color,
                        newPiece.Special,
                        plannedSpawn.Direction
                    )
                );
            }
        }
    }

    private LocalCollapsePlan BuildSquarePlan(
    BoardModel board,
    MatchResult match,
    HashSet<BoardCell> protectedMatchCells)
    {
        LocalCollapsePlan plan =
            new LocalCollapsePlan();

        int minX =
            match.Cells.Min(
                cell => cell.X
            );

        int maxX =
            match.Cells.Max(
                cell => cell.X
            );

        int minY =
            match.Cells.Min(
                cell => cell.Y
            );

        int maxY =
            match.Cells.Max(
                cell => cell.Y
            );

        // One of the four cells normally survives
        // as the Target special.
        //
        // We ONLY collapse into cells that actually
        // became empty.
        List<BoardCell> targets =
            match.Cells
                .Where(cell => cell.IsEmpty)
                .ToList();

        HashSet<BoardCell> reservedSources =
            new HashSet<BoardCell>();

        foreach (BoardCell target in targets)
        {
            List<GravityDirection> preferences =
                GetSquareDirections(
                    target,
                    minX,
                    maxX,
                    minY,
                    maxY
                );

            BoardCell source = null;
            GravityDirection chosenDirection =
                preferences[0];

            foreach (
                GravityDirection direction
                in preferences
            )
            {
                source =
                    FindSquareSource(
                        board,
                        target,
                        direction,
                        minX,
                        maxX,
                        minY,
                        maxY,
                        protectedMatchCells,
                        reservedSources
                    );

                if (source != null)
                {
                    chosenDirection =
                        direction;

                    break;
                }
            }

            if (source != null)
            {
                reservedSources.Add(
                    source
                );

                plan.Moves.Add(
                    new PlannedMove(
                        source,
                        target
                    )
                );

                // Moving the source leaves a new
                // empty cell where it came from.
                plan.Spawns.Add(
                    new PlannedSpawn(
                        source,
                        chosenDirection
                    )
                );
            }
            else
            {
                // No existing piece available from
                // either outward side.
                //
                // Spawn directly into this target.
                plan.Spawns.Add(
                    new PlannedSpawn(
                        target,
                        chosenDirection
                    )
                );
            }
        }

        return plan;
    }

    private List<GravityDirection> GetSquareDirections(
    BoardCell target,
    int minX,
    int maxX,
    int minY,
    int maxY)
    {
        bool isLeft =
            target.X == minX;

        bool isBottom =
            target.Y == minY;

        // Bottom-left
        if (isLeft && isBottom)
        {
            return new List<GravityDirection>
        {
            GravityDirection.Right,
            GravityDirection.Up
        };
        }

        // Bottom-right
        if (!isLeft && isBottom)
        {
            return new List<GravityDirection>
        {
            GravityDirection.Up,
            GravityDirection.Left
        };
        }

        // Top-left
        if (isLeft && !isBottom)
        {
            return new List<GravityDirection>
        {
            GravityDirection.Down,
            GravityDirection.Right
        };
        }

        // Top-right
        return new List<GravityDirection>
    {
        GravityDirection.Left,
        GravityDirection.Down
    };
    }

    private BoardCell FindSquareSource(
    BoardModel board,
    BoardCell target,
    GravityDirection direction,
    int minX,
    int maxX,
    int minY,
    int maxY,
    HashSet<BoardCell> protectedMatchCells,
    HashSet<BoardCell> reservedSources)
    {
        switch (direction)
        {
            // Piece comes FROM the left
            // and moves RIGHT.
            case GravityDirection.Right:
                {
                    for (
                        int x = minX - 1;
                        x >= 0;
                        x--
                    )
                    {
                        BoardCell source =
                            board.GetCell(
                                x,
                                target.Y
                            );

            if (!source.IsPlayable) break;

                        if (
                            CanUseSquareSource(
                                source,
                                protectedMatchCells,
                                reservedSources
                            )
                        )
                        {
                            return source;
                        }
                    }

                    break;
                }

            // Piece comes FROM the right
            // and moves LEFT.
            case GravityDirection.Left:
                {
                    for (
                        int x = maxX + 1;
                        x < board.Width;
                        x++
                    )
                    {
                        BoardCell source =
                            board.GetCell(
                                x,
                                target.Y
                            );

            if (!source.IsPlayable) break;

                        if (
                            CanUseSquareSource(
                                source,
                                protectedMatchCells,
                                reservedSources
                            )
                        )
                        {
                            return source;
                        }
                    }

                    break;
                }

            // Piece comes FROM below
            // and moves UP.
            case GravityDirection.Up:
                {
                    for (
                        int y = minY - 1;
                        y >= 0;
                        y--
                    )
                    {
                        BoardCell source =
                            board.GetCell(
                                target.X,
                                y
                            );

            if (!source.IsPlayable) break;

                        if (
                            CanUseSquareSource(
                                source,
                                protectedMatchCells,
                                reservedSources
                            )
                        )
                        {
                            return source;
                        }
                    }

                    break;
                }

            // Piece comes FROM above
            // and moves DOWN.
            case GravityDirection.Down:
                {
                    for (
                        int y = maxY + 1;
                        y < board.Height;
                        y++
                    )
                    {
                        BoardCell source =
                            board.GetCell(
                                target.X,
                                y
                            );

            if (!source.IsPlayable) break;

                        if (
                            CanUseSquareSource(
                                source,
                                protectedMatchCells,
                                reservedSources
                            )
                        )
                        {
                            return source;
                        }
                    }

                    break;
                }
        }

        return null;
    }

    private bool CanUseSquareSource(
    BoardCell source,
    HashSet<BoardCell> protectedMatchCells,
    HashSet<BoardCell> reservedSources)
    {
        if (source.IsEmpty)
        {
            return false;
        }

        // Don't steal the surviving Target or
        // anything belonging to another simultaneous
        // match.
        if (
            protectedMatchCells.Contains(
                source
            )
        )
        {
            return false;
        }

        // Don't let two corners pull the same piece.
        if (
            reservedSources.Contains(
                source
            )
        )
        {
            return false;
        }

        return true;
    }
}