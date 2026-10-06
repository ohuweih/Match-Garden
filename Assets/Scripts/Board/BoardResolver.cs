using System.Collections.Generic;
using UnityEngine;

public class BoardResolver
{
    private System.Random random =
    new System.Random();

    private IRefillStrategy refillStrategy;

    private readonly PackageSystem packages;

    private readonly BoardGeneratorSystem generators = new BoardGeneratorSystem();

    private ResolutionSequence activeSequence;

    private ResolutionRound activeRound;
    private SpecialEffectRecord currentEffect;
    private SpecialEffectRecord effectParent;
    private Vector2Int effectOrigin;

    private void BeginEffect(SpecialType type, int x, int y, int radius = 2)
    {
        currentEffect = new SpecialEffectRecord(type, x, y, effectParent, radius);
        activeRound?.SpecialEffects.Add(currentEffect);
    }

    private void StartRecording(MoveSource source)
    {
        activeSequence =
            new ResolutionSequence(source);

        refillStrategy.BeginResolution();
    }

    private void ApplyRefill(
    BoardModel board,
    List<MatchResult> matches = null)
    {
        BoardGeneratorSystem.DamageAdjacentMatches(board, matches, activeRound);
        PackageSystem.HitAdjacentMatches(board, matches, activeRound);
        packages.OpenHitPackages(board, activeRound, random);
        var objectives = new Dictionary<PieceData, (int X, int Y)>();
        for (int y = 0; y < board.Height; y++)
            for (int x = 0; x < board.Width; x++)
            {
                var piece = board.GetCell(x, y).Piece;
                if (piece != null && piece.IsMovableObjective) objectives.Add(piece, (x, y));
            }
        refillStrategy.Apply(
            board,
            activeRound,
            matches
        );

        for (int y = 0; y < board.Height; y++)
            for (int x = 0; x < board.Width; x++)
            {
                var piece = board.GetCell(x, y).Piece;
                if (piece != null && objectives.TryGetValue(piece, out var start))
                    piece.RecordTravel(start.X, start.Y, x, y);
            }

        if (activeRound == null) return;
        foreach (var change in activeRound.GeneratorChanges)
        {
            if (!change.Destroyed) continue;
            var cell = board.GetCell(change.X, change.Y);
            cell.RemoveDestroyedGenerator();
            cell.Piece = board.GetRandomPiece();
            activeRound.Spawns.Add(new SpawnRecord(cell.X, cell.Y, cell.Piece.Color, cell.Piece.Special));
        }
        // Locks remain walls for this round's refill. Newly unlocked cells
        // receive a piece now and participate in the next cascade scan.
        foreach (LockedTileChange change in BoardUnlocker.ApplyRound(board, matches, activeRound.ClearedCells))
        {
            if (activeRound != null) activeRound.LockedTileChanges.Add(change);
            if (change.RemainingMatches == 0)
            {
                PieceData piece = board.GetRandomPiece();
                board.GetCell(change.X, change.Y).Piece = piece;
                if (activeRound != null)
                    activeRound.Spawns.Add(new SpawnRecord(change.X, change.Y, piece.Color, piece.Special));
            }
        }

    }

    public BoardResolver(
    RefillMode refillMode = RefillMode.Classic, PackageSettings packageSettings = null)
    {
        packages = new PackageSystem(packageSettings, refillMode);
        refillStrategy =
            RefillStrategyFactory.Create(
                refillMode
            );
    }

    private ResolutionSequence FinishRecording()
    {
        ResolutionSequence result =
            activeSequence;

        activeSequence = null;
        activeRound = null;
        currentEffect = null;
        effectParent = null;

        return result;
    }

    private void BeginRound(bool isMatchRound = false)
    {
        activeRound =
            new ResolutionRound(isMatchRound);

        if (activeSequence != null)
            activeSequence.Rounds.Add(activeRound);
    }

    private void EndRound()
    {
        activeRound = null;
        currentEffect = null;
        effectParent = null;
    }

    public ResolutionSequence GenerateAfterPlayerAction(BoardModel board, ResolutionSequence action)
    {
        StartRecording(MoveSource.Generator);
        BeginRound();
        bool produced = generators.GenerateAfterPlayerAction(board, action, activeRound, random);
        produced |= packages.TrySpawnAfterPlayerAction(board, action, activeRound, random);
        if (!produced) activeSequence.Rounds.Remove(activeRound);
        EndRound();
        // Outputs can later spawn pieces/birds: resolve their resulting matches/rescues normally.
        if (produced) Resolve(board, null);
        return FinishRecording();
    }

    public ResolutionSequence ResolveWithSequence(
    BoardModel board,
    SwapResult swapResult,
    MoveSource source = MoveSource.Player)
    {
        StartRecording(source);

        Resolve(
            board,
            swapResult
        );

        return FinishRecording();
    }

    public ResolutionSequence ActivateSpecialWithSequence(
    BoardModel board,
    BoardCell cell,
    PieceType? targetColorOverride = null,
    MoveSource source = MoveSource.Player)
    {
        StartRecording(source);

        ActivateSpecial(
            board,
            cell,
            targetColorOverride
        );

        return FinishRecording();
    }

    public ResolutionSequence ResolveSpecialComboWithSequence(
    BoardModel board,
    SpecialComboRequest combo,
    MoveSource source = MoveSource.Player)
    {
        StartRecording(source);

        ResolveSpecialCombo(
            board,
            combo
        );

        return FinishRecording();
    }

    public void Resolve(
        BoardModel board,
        SwapResult swapResult)
    {
        int cascadeNumber = 0;

        while (true)
        {
            // Rescue after a complete gravity movement, before scanning new matches.
            // This separate round keeps rescue/refill animations in order and does
            // not count a rescue as a match or a cascade for the Hot Streak bonus.
            if (BirdRescueRules.HasReadyBird(board))
            {
                BeginRound();
                for (int y = 0; y < board.Height; y++)
                    for (int x = 0; x < board.Width; x++)
                    {
                        var cell = board.GetCell(x, y);
                        if (!BirdRescueRules.CanRescue(cell, board)) continue;
                        activeRound.RescuedBirds.Add(new BirdRescueRecord(cell));
                        cell.Piece = null;
                    }
                ApplyRefill(board);
                EndRound();
                // Refilling can move another bird onto its goal.
                continue;
            }

            List<MatchResult> matchResults =
                MatchFinder.FindMatchResults(board);

            foreach (MatchResult result in matchResults)
            {
                Debug.Log(
                    $"Winning match: {result.Shape} " +
                    $"with {result.Cells.Count} pieces."
                );
            }

            if (matchResults.Count == 0)
            {
                break;
            }

            cascadeNumber++;

            BeginRound(isMatchRound: true);

            Debug.Log(
                $"Cascade {cascadeNumber}"
            );

            Queue<SpecialActivation> activationQueue =
                new Queue<SpecialActivation>();

            // First clear the matches and create
            // any new special pieces.
            foreach (MatchResult match in matchResults)
            {
                ClearMatchResult(
                    match,
                    swapResult,
                    activationQueue
                );
            }

            // Then activate any specials that were
            // cleared by those matches.
            ProcessSpecialActivations(
                board,
                activationQueue
            );

            ApplyRefill(
                board,
                matchResults
            );

            EndRound();
        }

        ClearMatchProtection(board);

        Debug.Log(
            $"Resolution finished after " +
            $"{cascadeNumber} cascade(s)."
        );
    }

    private void ClearMatchProtection(
    BoardModel board)
    {
        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                BoardCell cell =
                    board.GetCell(x, y);

                if (cell.IsEmpty || !cell.Piece.IsColored)
                {
                    continue;
                }

                cell.Piece.IsMatchProtected =
                    false;
            }
        }
    }

    private void ClearMatchResult(
        MatchResult match,
        SwapResult swapResult,
        Queue<SpecialActivation> activationQueue)
    {
        SpecialType newSpecialType =
            GetSpecialType(match.Shape);

        BoardCell specialSpawnCell = null;

        if (newSpecialType != SpecialType.None)
        {
            specialSpawnCell =
                ChooseSpecialSpawnCell(
                    match,
                    swapResult
                );
        }

        foreach (BoardCell cell in match.Cells)
        {
            // Matching also chips frost under the cell that becomes a new special.
            activeRound?.HitFrost(cell);
            // The cell chosen to become the NEW special
            // survives this match.
            if (cell == specialSpawnCell)
            {
                continue;
            }

            ClearCell(
                cell,
                activationQueue
            );
        }

        if (specialSpawnCell != null)
        {
            specialSpawnCell.Piece.Special =
                newSpecialType;

            specialSpawnCell.Piece.IsMatchProtected =
                true;

            Debug.Log(
                $"Created {newSpecialType} at " +
                $"({specialSpawnCell.X}, " +
                $"{specialSpawnCell.Y})"
            );
        }
    }

    private void ClearCell(
    BoardCell cell,
    Queue<SpecialActivation> activationQueue,
    PieceType? targetColorOverride = null)
    {
        currentEffect?.AddTarget(cell);
        activeRound?.HitFrost(cell);
        activeRound?.HitGenerator(cell);
        if (cell.Piece != null && cell.Piece.IsPackage)
        {
            activeRound?.HitPackage(cell);
            return;
        }
        if (cell.IsEmpty || !cell.Piece.IsColored)
        {
            return;
        }

        if (cell.Piece.IsSpecial)
        {
            PieceType activationColor =
                targetColorOverride ??
                cell.Piece.Color;

            activationQueue.Enqueue(
                new SpecialActivation(
                    cell.X,
                    cell.Y,
                    cell.Piece.Special,
                    activationColor
                ) { ParentEffect = currentEffect }
            );
        }

        if (activeRound != null)
        {
            activeRound.AddClearedCell(
                cell
            );
        }

        cell.Piece = null;
    }

    private void ProcessSpecialActivations(
        BoardModel board,
        Queue<SpecialActivation> activationQueue)
    {
        while (activationQueue.Count > 0)
        {
            SpecialActivation activation =
                activationQueue.Dequeue();

            Debug.Log(
                $"Activating {activation.Type} at " +
                $"({activation.X}, {activation.Y})"
            );

            effectOrigin = new Vector2Int(activation.X, activation.Y);
            effectParent = activation.ParentEffect;
            currentEffect = null;
            switch (activation.Type)
            {
                case SpecialType.LineHorizontal:
                    ActivateHorizontalLine(
                        board,
                        activation.Y,
                        activationQueue
                    );
                    break;

                case SpecialType.LineVertical:
                    ActivateVerticalLine(
                        board,
                        activation.X,
                        activationQueue
                    );
                    break;

                case SpecialType.Target:
                    ActivateTarget(
                        board,
                        activation.TargetColor,
                        activationQueue
                    );
                    break;

                case SpecialType.ColorClear:
                    ActivateColorClear(
                        board,
                        activation.TargetColor,
                        activationQueue
                    );
                    break;

                case SpecialType.Bomb:
                    ActivateBomb(
                        board,
                        activation.X,
                        activation.Y,
                        activationQueue
                    );
                    break;
            }
        }
    }

    private void ActivateColorClear(
    BoardModel board,
    PieceType? targetColor,
    Queue<SpecialActivation> activationQueue)
    {
        BeginEffect(SpecialType.ColorClear, effectOrigin.x, effectOrigin.y);
        if (!targetColor.HasValue)
        {
            return;
        }

        PieceType color =
            targetColor.Value;

        List<BoardCell> targets =
            new List<BoardCell>();

        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                BoardCell cell =
                    board.GetCell(x, y);

                if (cell.IsEmpty || !cell.Piece.IsColored)
                {
                    continue;
                }

                if (cell.Piece.Color == color)
                {
                    targets.Add(cell);
                }
            }
        }

        Debug.Log(
            $"ColorClear targeting {color}. " +
            $"Clearing {targets.Count} pieces."
        );

        foreach (BoardCell target in targets)
        {
            ClearCell(
                target,
                activationQueue
            );
        }
    }

    private void ActivateHorizontalLine(
        BoardModel board,
        int y,
        Queue<SpecialActivation> activationQueue)
    {
        BeginEffect(SpecialType.LineHorizontal, effectOrigin.x, y);
        for (int x = 0; x < board.Width; x++)
        {
            BoardCell cell =
                board.GetCell(x, y);

            ClearCell(
                cell,
                activationQueue
            );
        }
    }

    private void ActivateVerticalLine(
        BoardModel board,
        int x,
        Queue<SpecialActivation> activationQueue)
    {
        BeginEffect(SpecialType.LineVertical, x, effectOrigin.y);
        for (int y = 0; y < board.Height; y++)
        {
            BoardCell cell =
                board.GetCell(x, y);

            ClearCell(
                cell,
                activationQueue
            );
        }
    }

    private void ActivateBomb(
    BoardModel board,
    int centerX,
    int centerY,
    Queue<SpecialActivation> activationQueue)
    {
        BeginEffect(SpecialType.Bomb, centerX, centerY);
        int radius = 2;

        Debug.Log(
            $"Bomb exploding at ({centerX}, {centerY})"
        );

        for (
            int x = centerX - radius;
            x <= centerX + radius;
            x++
        )
        {
            for (
                int y = centerY - radius;
                y <= centerY + radius;
                y++
            )
            {
                if (
                    x < 0 ||
                    x >= board.Width ||
                    y < 0 ||
                    y >= board.Height
                )
                {
                    continue;
                }

                BoardCell cell =
                    board.GetCell(x, y);

                ClearCell(
                    cell,
                    activationQueue
                );
            }
        }
    }

    private BoardCell ChooseSpecialSpawnCell(
    MatchResult match,
    SwapResult swapResult)
    {
        // If this resolution came from a player swap,
        // prefer the destination of that swap.
        if (swapResult != null)
        {
            if (
                match.Cells.Contains(
                    swapResult.SecondCell
                ) &&
                CanBecomeSpecial(
                    swapResult.SecondCell
                )
            )
            {
                return swapResult.SecondCell;
            }

            if (
                match.Cells.Contains(
                    swapResult.FirstCell
                ) &&
                CanBecomeSpecial(
                    swapResult.FirstCell
                )
            )
            {
                return swapResult.FirstCell;
            }
        }

        // Cascades or direct activations don't have
        // a meaningful swap destination, so prefer
        // the center of the resulting match.
        int middle =
            match.Cells.Count / 2;

        if (CanBecomeSpecial(
            match.Cells[middle]))
        {
            return match.Cells[middle];
        }

        foreach (BoardCell cell in match.Cells)
        {
            if (CanBecomeSpecial(cell))
            {
                return cell;
            }
        }

        return null;
    }

    private bool CanBecomeSpecial(
        BoardCell cell)
    {
        return
            !cell.IsEmpty && cell.Piece.IsColored &&
            !cell.Piece.IsSpecial;
    }

    private SpecialType GetSpecialType(
    MatchShape shape)
    {
        switch (shape)
        {
            case MatchShape.FourHorizontal:
                return SpecialType.LineHorizontal;

            case MatchShape.FourVertical:
                return SpecialType.LineVertical;

            case MatchShape.Square:
                return SpecialType.Target;

            case MatchShape.FivePlusHorizontal:
            case MatchShape.FivePlusVertical:
                return SpecialType.ColorClear;

            case MatchShape.TShape:
            case MatchShape.LShape:
                return SpecialType.Bomb;

            default:
                return SpecialType.None;
        }
    }

    public void ActivateSpecial(
    BoardModel board,
    BoardCell cell,
    PieceType? targetColorOverride = null)
    {
        if (cell.IsEmpty || !cell.Piece.IsColored)
        {
            return;
        }

        if (!cell.Piece.IsSpecial)
        {
            return;
        }

        Debug.Log(
            $"Directly activating {cell.Piece.Special} at " +
            $"({cell.X}, {cell.Y})"
        );

        Queue<SpecialActivation> activationQueue =
            new Queue<SpecialActivation>();

        BeginRound();

        ClearCell(
            cell,
            activationQueue,
            targetColorOverride
        );

        ProcessSpecialActivations(
            board,
            activationQueue
        );

        ApplyRefill(
            board
        );

        EndRound();

        Resolve(
            board,
            null
        );
    }

    private void ActivateTarget(
    BoardModel board,
    PieceType? targetColor,
    Queue<SpecialActivation> activationQueue)
    {
        BeginEffect(SpecialType.Target, effectOrigin.x, effectOrigin.y);
        if (!targetColor.HasValue)
        {
            return;
        }

        List<BoardCell> candidates =
            new List<BoardCell>();

        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                BoardCell cell =
                    board.GetCell(x, y);

                if (cell.IsEmpty || !cell.Piece.IsColored)
                {
                    continue;
                }

                if (
                    cell.Piece.Color ==
                    targetColor.Value
                )
                {
                    candidates.Add(cell);
                }
            }
        }

        int targetsToClear =
            candidates.Count >= 2
                ? 2
                : candidates.Count;

        Debug.Log(
            $"Target special searching for " +
            $"{targetColor.Value}. " +
            $"Found {candidates.Count} candidates."
        );

        for (
            int i = 0;
            i < targetsToClear;
            i++)
        {
            int randomIndex =
                random.Next(
                    candidates.Count
                );

            BoardCell target =
                candidates[randomIndex];

            candidates.RemoveAt(
                randomIndex
            );

            Debug.Log(
                $"Targeting {targetColor.Value} at " +
                $"({target.X}, {target.Y})"
            );

            ClearCell(
                target,
                activationQueue
            );
        }
    }

    private void ConsumeForGeneratedActivation(
    BoardCell cell,
    SpecialType generatedSpecial,
    PieceType activationColor,
    Queue<SpecialActivation> activationQueue)
    {
        if (cell.IsEmpty || !cell.Piece.IsColored)
        {
            return;
        }

        currentEffect?.AddTarget(cell);

        // IMPORTANT:
        // Record this cell as visually cleared so
        // BoardView can recycle its PieceView.
        if (activeRound != null)
        {
            activeRound.AddClearedCell(
                cell
            );
        }

        activationQueue.Enqueue(
            new SpecialActivation(
                cell.X,
                cell.Y,
                generatedSpecial,
                activationColor
            ) { ParentEffect = currentEffect }
        );

        // Consume it WITHOUT calling ClearCell().
        //
        // ClearCell would activate the piece's
        // previous special too, which isn't what
        // this combo means.
        cell.Piece = null;
    }

    public void ResolveSpecialCombo(
    BoardModel board,
    SpecialComboRequest combo)
    {
        Debug.Log(
            $"Resolving combo: {combo.Type}"
        );

        Queue<SpecialActivation> activationQueue =
            new Queue<SpecialActivation>();

        BeginRound();

        if (activeRound != null)
        {
            activeRound.AddClearedCell(
                combo.FirstCell
            );

            activeRound.AddClearedCell(
                combo.SecondCell
            );
        }

        // Consume the two pieces used to create
        // the combo without activating their
        // normal standalone effects.
        combo.FirstCell.Piece = null;
        combo.SecondCell.Piece = null;

        effectOrigin = new Vector2Int(combo.SecondCell.X, combo.SecondCell.Y);
        effectParent = null;
        currentEffect = null;
        switch (combo.Type)
        {
            case SpecialComboType.TargetHorizontal:
                ActivateTargetGeneratedSpecials(
                    board,
                    SpecialType.LineHorizontal,
                    2,
                    activationQueue
                );
                break;

            case SpecialComboType.TargetVertical:
                ActivateTargetGeneratedSpecials(
                    board,
                    SpecialType.LineVertical,
                    2,
                    activationQueue
                );
                break;

            case SpecialComboType.TargetTarget:
                ActivateDoubleTargetCombo(
                    board,
                    activationQueue
                );
                break;

            case SpecialComboType.TargetBomb:
                ActivateTargetGeneratedSpecials(
                    board,
                    SpecialType.Bomb,
                    2,
                    activationQueue
                );
                break;

            case SpecialComboType.LineLine:
                ActivateLineLineCombo(
                    board,
                    combo,
                    activationQueue
                );
                break;

            case SpecialComboType.BombLine:
                ActivateBombLineCombo(
                    board,
                    combo,
                    activationQueue
                );
                break;

            case SpecialComboType.BombBomb:
                ActivateLargeBombCombo(
                    board,
                    combo,
                    activationQueue
                );
                break;

            case SpecialComboType.ColorClearSpecial:
                ActivateColorClearSpecialCombo(
                    board,
                    combo,
                    activationQueue
                );
                break;

            case SpecialComboType.ColorClearColorClear:
                ActivateDoubleColorClearCombo(
                    board,
                    combo,
                    activationQueue
                );
                break;
        }

        ProcessSpecialActivations(
            board,
            activationQueue
        );

        ApplyRefill(
            board
        );

        EndRound();

        Resolve(
            board,
            null
        );
    }

    private void ActivateTargetGeneratedSpecials(
    BoardModel board,
    SpecialType generatedType,
    int count,
    Queue<SpecialActivation> activationQueue)
    {
        BeginEffect(SpecialType.Target, effectOrigin.x, effectOrigin.y);
        List<BoardCell> candidates =
            GetAllOccupiedCells(board);

        int amount =
            candidates.Count < count
                ? candidates.Count
                : count;

        for (int i = 0; i < amount; i++)
        {
            int index =
                random.Next(candidates.Count);

            BoardCell target =
                candidates[index];

            candidates.RemoveAt(index);

            PieceType color =
                target.Piece.Color;

            Debug.Log(
                $"Combo generated {generatedType} at " +
                $"({target.X}, {target.Y})"
            );

            ConsumeForGeneratedActivation(
                target,
                generatedType,
                color,
                activationQueue
            );
        }
    }

    private void ActivateLineLineCombo(
    BoardModel board,
    SpecialComboRequest combo,
    Queue<SpecialActivation> activationQueue)
    {
        int centerX =
            combo.SecondCell.X;

        int centerY =
            combo.SecondCell.Y;

        Debug.Log(
            $"Line + Line cross at " +
            $"({centerX}, {centerY})"
        );

        ActivateHorizontalLine(
            board,
            centerY,
            activationQueue
        );

        ActivateVerticalLine(
            board,
            centerX,
            activationQueue
        );
    }

    private void ActivateBombLineCombo(
    BoardModel board,
    SpecialComboRequest combo,
    Queue<SpecialActivation> activationQueue)
    {
        int centerX =
            combo.SecondCell.X;

        int centerY =
            combo.SecondCell.Y;

        Debug.Log(
            $"Bomb + Line combo at " +
            $"({centerX}, {centerY})"
        );

        for (int offset = -1; offset <= 1; offset++)
        {
            int row =
                centerY + offset;

            if (
                row >= 0 &&
                row < board.Height
            )
            {
                ActivateHorizontalLine(
                    board,
                    row,
                    activationQueue
                );
            }

            int column =
                centerX + offset;

            if (
                column >= 0 &&
                column < board.Width
            )
            {
                ActivateVerticalLine(
                    board,
                    column,
                    activationQueue
                );
            }
        }
    }

    private void ActivateLargeBombCombo(
    BoardModel board,
    SpecialComboRequest combo,
    Queue<SpecialActivation> activationQueue)
    {
        BeginEffect(SpecialType.Bomb, combo.SecondCell.X, combo.SecondCell.Y, 3);
        int centerX =
            combo.SecondCell.X;

        int centerY =
            combo.SecondCell.Y;

        int radius = 3;

        Debug.Log(
            $"BIG BOMB at ({centerX}, {centerY})"
        );

        for (
            int x = centerX - radius;
            x <= centerX + radius;
            x++
        )
        {
            for (
                int y = centerY - radius;
                y <= centerY + radius;
                y++
            )
            {
                if (
                    x < 0 ||
                    x >= board.Width ||
                    y < 0 ||
                    y >= board.Height
                )
                {
                    continue;
                }

                ClearCell(
                    board.GetCell(x, y),
                    activationQueue
                );
            }
        }
    }

    private void ActivateDoubleTargetCombo(
    BoardModel board,
    Queue<SpecialActivation> activationQueue)
    {
        BeginEffect(SpecialType.Target, effectOrigin.x, effectOrigin.y);
        const int targetCount = 5;

        List<BoardCell> candidates =
            GetAllOccupiedCells(board);

        int amountToClear =
            candidates.Count < targetCount
                ? candidates.Count
                : targetCount;

        Debug.Log(
            $"Target + Target launching " +
            $"{amountToClear} strikes."
        );

        for (int i = 0; i < amountToClear; i++)
        {
            int randomIndex =
                random.Next(candidates.Count);

            BoardCell target =
                candidates[randomIndex];

            candidates.RemoveAt(randomIndex);

            Debug.Log(
                $"Target strike at " +
                $"({target.X}, {target.Y})"
            );

            ClearCell(
                target,
                activationQueue
            );
        }
    }

    private void ActivateColorClearSpecialCombo(
    BoardModel board,
    SpecialComboRequest combo,
    Queue<SpecialActivation> activationQueue)
    {
        BeginEffect(SpecialType.ColorClear, effectOrigin.x, effectOrigin.y);
        SpecialType generatedSpecial;
        PieceType targetColor;

        if (
            combo.FirstSpecial ==
            SpecialType.ColorClear
        )
        {
            generatedSpecial =
                combo.SecondSpecial;

            targetColor =
                combo.SecondColor;
        }
        else
        {
            generatedSpecial =
                combo.FirstSpecial;

            targetColor =
                combo.FirstColor;
        }

        List<BoardCell> targets =
            GetCellsOfColor(
                board,
                targetColor
            );

        Debug.Log(
            $"ColorClear combo converting " +
            $"{targets.Count} {targetColor} pieces " +
            $"into {generatedSpecial}"
        );

        foreach (BoardCell target in targets)
        {
            ConsumeForGeneratedActivation(
                target,
                generatedSpecial,
                targetColor,
                activationQueue
            );
        }
    }

    private void ActivateDoubleColorClearCombo(
    BoardModel board,
    SpecialComboRequest combo,
    Queue<SpecialActivation> activationQueue)
    {
        BeginEffect(SpecialType.ColorClear, effectOrigin.x, effectOrigin.y);
        List<BoardCell> targets =
            new List<BoardCell>();

        AddCellsOfColor(
            board,
            combo.FirstColor,
            targets
        );

        AddCellsOfColor(
            board,
            combo.SecondColor,
            targets
        );

        Debug.Log(
            $"Double ColorClear generated " +
            $"{targets.Count} ColorClears."
        );

        foreach (BoardCell target in targets)
        {
            PieceType randomTargetColor =
                GetRandomExistingColor(
                    board
                );

            ConsumeForGeneratedActivation(
                target,
                SpecialType.ColorClear,
                randomTargetColor,
                activationQueue
            );
        }
    }

    private List<BoardCell> GetAllOccupiedCells(
    BoardModel board)
    {
        List<BoardCell> cells =
            new List<BoardCell>();

        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                BoardCell cell =
                    board.GetCell(x, y);

                if (!cell.IsEmpty && cell.Piece.IsColored)
                {
                    cells.Add(cell);
                }
            }
        }

        return cells;
    }

    private List<BoardCell> GetCellsOfColor(
    BoardModel board,
    PieceType color)
    {
        List<BoardCell> cells =
            new List<BoardCell>();

        AddCellsOfColor(
            board,
            color,
            cells
        );

        return cells;
    }

    private void AddCellsOfColor(
    BoardModel board,
    PieceType color,
    List<BoardCell> results)
    {
        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                BoardCell cell =
                    board.GetCell(x, y);

                if (
                    !cell.IsEmpty && cell.Piece.IsColored &&
                    cell.Piece.Color == color &&
                    !results.Contains(cell)
                )
                {
                    results.Add(cell);
                }
            }
        }
    }

    private PieceType GetRandomExistingColor(
    BoardModel board)
    {
        List<PieceType> colors =
            new List<PieceType>();

        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                BoardCell cell =
                    board.GetCell(x, y);

                if (
                    !cell.IsEmpty && cell.Piece.IsColored &&
                    !colors.Contains(
                        cell.Piece.Color
                    )
                )
                {
                    colors.Add(
                        cell.Piece.Color
                    );
                }
            }
        }

        if (colors.Count == 0)
        {
            return PieceType.Red;
        }

        return colors[
            random.Next(colors.Count)
        ];
    }
}