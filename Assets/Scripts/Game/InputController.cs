using System.Collections;
using UnityEngine;

public partial class InputController : MonoBehaviour
{
    public static InputController Instance { get; private set; }
    // Fires after opening or a whole player action, including all earned bonus moves.
    public event System.Action<MoveSource> ResolutionChainCompleted;
    private bool levelEnded;

    public void EndLevel()
    {
        ResetBankInteraction();
        levelEnded = true;
        inputLocked = true;
        ClearHint();
        DeselectPiece();
        lastClickedPiece = null;
    }

    public void PrepareForRestart()
    {
        ResetBankInteraction();
        StopAllCoroutines();
        ClearHint();
        DeselectPiece();
        lastClickedPiece = null;
        cascadeBonus.Stop();
        inputLocked = true;
    }

    private BoardResolver boardResolver;
    private BoardModel board;
    private BoardView boardView;

    private PieceView selectedPiece;

    [SerializeField]
    private float doubleClickThreshold = 0.35f;

    private PieceView lastClickedPiece;
    private float lastClickTime;

    [SerializeField]
    private float swapDuration = 0.2f;

    [SerializeField]
    private float popDuration = 0.22f;

    [SerializeField]
    private float fallDuration = 0.25f;

    [SerializeField]
    private float refillDuration = 0.3f;

    [SerializeField]
    private bool logBoardMoveChecks = true;

    [SerializeField, Min(0f)]
    private float openingMatchDelay = 0.6f;

    [SerializeField, Min(0f)]
    private float shuffleDuration = 0.5f;

    [SerializeField] private RefillMode refillMode = RefillMode.Normal;
    public RefillMode SceneRefillMode => refillMode;
    private CascadeBonusSettings activeBonusSettings = new CascadeBonusSettings();

    public CascadeBonusSettings CaptureSceneCascadeBonus() => new CascadeBonusSettings
    {
        Enabled = cascadeBonusEnabled,
        TriggerCascades = Mathf.Max(1, cascadeBonusTrigger),
        BonusMoves = Mathf.Max(1, cascadeBonusMoves)
    };

    private readonly System.Random shuffleRandom = new System.Random();
    [SerializeField] private bool cascadeBonusEnabled = true;
    [SerializeField, Min(1)] private int cascadeBonusTrigger = 10;
    [SerializeField, Min(1)] private int cascadeBonusMoves = 5;
    [SerializeField, Min(0f)] private float bonusMoveDelay = 0.35f;
    private CascadeBonusState cascadeBonus = new CascadeBonusState();
    public bool IsCascadeBonusActive => cascadeBonus.IsActive;
    public int CascadeBonusCompletedMoves => cascadeBonus.CompletedMoves;
    public int CascadeBonusTotalMoves => cascadeBonus.TotalMoves;
    public bool CascadeBonusEnabled => activeBonusSettings != null && activeBonusSettings.Enabled;
    public int CascadeBonusTrigger => activeBonusSettings != null ? Mathf.Max(1, activeBonusSettings.TriggerCascades) : 10;
    private readonly System.Random bonusRandom = new System.Random();
    private bool lastSwapSucceeded;
    private bool inputLocked = true;
    private bool gameplayPaused;
    private Coroutine hintAnimation;
    private PieceView hintFirst;
    private PieceView hintSecond;
    private string hintMessage = "";

    public bool PlayerActionsReady => !gameplayPaused && !inputLocked && !levelEnded && board != null;

    public void SetGameplayPaused(bool paused)
    {
        gameplayPaused = paused;
        if (paused)
        {
            ClearHint();
            DeselectPiece();
            lastClickedPiece = null;
        }
    }
    public string HintMessage => hintMessage;

    private readonly System.Random hintRandom = new System.Random();

    public void ShowHint()
    {
        if (gameplayPaused || inputLocked || board == null) return;
        pendingBankId = null;
        bankMessage = "";
        ClearHint();
        DeselectPiece();
        lastClickedPiece = null;
        var moves = BoardMoveFinder.FindAllMoves(board);
        if (moves.Count > 0)
        {
            var move = moves[hintRandom.Next(moves.Count)];
            BoardCell first = move.First;
            BoardCell second = move.Second;
            hintFirst = boardView.GetPieceView(first.X, first.Y);
            hintSecond = second == null ? null : boardView.GetPieceView(second.X, second.Y);
            hintMessage = second == null ? "Double-click the pulsing special" : "Swap the two pulsing pieces";
            Debug.Log(second == null
                ? $"[Hint] Activate special at ({first.X}, {first.Y})."
                : $"[Hint] Swap ({first.X}, {first.Y}) with ({second.X}, {second.Y}).", this);
            hintAnimation = StartCoroutine(PulseHint());
        }
        else
        {
            hintMessage = "No playable move available";
            Debug.LogWarning("[Hint] No playable move found.", this);
        }
    }

    private IEnumerator PulseHint()
    {
        float elapsed = 0f;
        while (elapsed < 3f)
        {
            elapsed += Time.unscaledDeltaTime;
            float scale = 1f + 0.15f * Mathf.Sin(elapsed * Mathf.PI * 4f);
            if (hintFirst != null) hintFirst.transform.localScale = Vector3.one * scale;
            if (hintSecond != null) hintSecond.transform.localScale = Vector3.one * scale;
            yield return null;
        }
        if (hintFirst != null) hintFirst.transform.localScale = Vector3.one;
        if (hintSecond != null) hintSecond.transform.localScale = Vector3.one;
        hintFirst = null;
        hintSecond = null;
        hintMessage = "";
        hintAnimation = null;
    }

    private void ClearHint()
    {
        if (hintAnimation != null) StopCoroutine(hintAnimation);
        if (hintFirst != null) hintFirst.transform.localScale = Vector3.one;
        if (hintSecond != null) hintSecond.transform.localScale = Vector3.one;
        hintFirst = null;
        hintSecond = null;
        hintAnimation = null;
        hintMessage = "";
    }

    private void OnDisable()
    {
        ClearHint();
    }

    private void Awake()
    {
        Instance = this;
    }

    public void Setup(
        BoardModel boardModel,
        BoardView view,
        RefillMode? levelRefillMode = null,
        CascadeBonusSettings levelBonus = null,
        CampaignProgress campaign = null,
        LevelDefinition level = null,
        PackageSettings packageSettings = null)
    {
        ResetBankInteraction();
        bankCampaign = campaign;
        bankLevel = level;
        levelEnded = false;
        gameplayPaused = false;
        activeBonusSettings = levelBonus != null ? levelBonus.Copy() : CaptureSceneCascadeBonus();
        cascadeBonus = new CascadeBonusState();
        board = boardModel;
        boardView = view;

        boardResolver =
            new BoardResolver(
                levelRefillMode ?? refillMode, packageSettings
            );

        // Setup prepares the board but deliberately leaves input locked.
        // The player-facing level intro calls BeginLevel() when Play is pressed.
        inputLocked = true;
    }

    public void BeginLevel()
    {
        if (board == null || boardView == null || levelEnded || !inputLocked) return;
        StartCoroutine(ResolveOpeningBoardAnimated());
    }

    private IEnumerator ResolveOpeningBoardAnimated()
    {
        if (bankCampaign == null && (MatchFinder.FindMatches(board).Count > 0 || BirdRescueRules.HasReadyBird(board)))
        {
            // Let the player see the lucky starting matches before clearing them.
            yield return new WaitForSeconds(openingMatchDelay);

            // No player swap: reuse normal special creation, refill and cascades.
            ResolutionSequence sequence =
                boardResolver.ResolveWithSequence(board, null, MoveSource.OpeningCascade);

            Debug.Log(
                $"[Opening Cascade] Resolving {sequence.Rounds.Count} round(s). No player move spent.",
                this
            );

            yield return StartCoroutine(
                boardView.PlayResolutionSequence(
                    sequence,
                    popDuration,
                    fallDuration,
                    refillDuration
                )
            );

            boardView.RefreshBoard(board);
        }

        lastClickedPiece = null;
        ResolutionChainCompleted?.Invoke(MoveSource.OpeningCascade);
        if (!levelEnded) yield return StartCoroutine(EnsurePlayableBoard("After opening resolution"));
        inputLocked = levelEnded;
    }

    public void HandlePieceClicked(
        PieceView clickedPiece)
    {
        if (gameplayPaused || inputLocked || clickedPiece == null || PointerOverBank()) return;
        if (pendingBankId != null)
        {
            PlaceBankedSpecial(clickedPiece);
            return;
        }

        ClearHint();

        bool isDoubleClick =
            lastClickedPiece == clickedPiece &&
            Time.time - lastClickTime <=
            doubleClickThreshold;

        lastClickedPiece = clickedPiece;
        lastClickTime = Time.time;

        // -----------------------------------------------------
        // DOUBLE CLICK / DOUBLE TAP SPECIAL
        // -----------------------------------------------------

        if (
            isDoubleClick &&
            IsSpecialPiece(clickedPiece)
        )
        {
            StartCoroutine(
                ActivateSpecialPieceAnimated(
                    clickedPiece
                )
            );

            return;
        }

        // -----------------------------------------------------
        // FIRST SELECTION
        // -----------------------------------------------------

        if (selectedPiece == null)
        {
            SelectPiece(
                clickedPiece
            );

            return;
        }

        // -----------------------------------------------------
        // CLICK SAME PIECE AGAIN
        // -----------------------------------------------------

        if (selectedPiece == clickedPiece)
        {
            DeselectPiece();
            return;
        }

        // -----------------------------------------------------
        // ATTEMPT SWAP
        // -----------------------------------------------------

        if (
            AreAdjacent(
                selectedPiece,
                clickedPiece
            )
        )
        {
            PieceView first =
                selectedPiece;

            PieceView second =
                clickedPiece;

            DeselectPiece();

            StartCoroutine(
                SwapPiecesAnimated(
                    first,
                    second
                )
            );

            return;
        }

        // -----------------------------------------------------
        // SELECT A DIFFERENT NON-ADJACENT PIECE
        // -----------------------------------------------------

        SelectDifferentPiece(
            clickedPiece
        );
    }

    private IEnumerator SwapPiecesAnimated(
        PieceView first,
        PieceView second,
        MoveSource source = MoveSource.Player)
    {
        if (levelEnded) yield break;
        inputLocked = true;
        lastSwapSucceeded = false;

        int firstX = first.X;
        int firstY = first.Y;

        int secondX = second.X;
        int secondY = second.Y;

        // Frozen tiles stay still, including during invalid-swap feedback.
        if (!board.GetCell(firstX, firstY).CanSwap || !board.GetCell(secondX, secondY).CanSwap)
        {
            inputLocked = false;
            yield break;
        }

        SwapResult result =
            board.TrySwap(
                firstX,
                firstY,
                secondX,
                secondY
            );

        // =====================================================
        // INVALID SWAP
        // =====================================================

        if (!result.Success)
        {
            // Animate toward each other.
            yield return StartCoroutine(
                boardView.AnimateSwap(
                    first,
                    second,
                    swapDuration
                )
            );

            // Temporarily update coordinates so
            // AnimateSwap knows their current locations.
            first.SetCoordinates(
                secondX,
                secondY
            );

            second.SetCoordinates(
                firstX,
                firstY
            );

            // Animate back.
            yield return StartCoroutine(
                boardView.AnimateSwap(
                    first,
                    second,
                    swapDuration
                )
            );

            // Restore the original logical positions.
            first.SetCoordinates(
                firstX,
                firstY
            );

            second.SetCoordinates(
                secondX,
                secondY
            );

            boardView.RefreshBoard(
                board
            );

            Debug.Log(
                $"Invalid swap: " +
                $"({firstX}, {firstY}) with " +
                $"({secondX}, {secondY})"
            );

            if (!cascadeBonus.IsActive) inputLocked = false;

            yield break;
        }

        // =====================================================
        // VALID SWAP
        // =====================================================

        Debug.Log(
            $"Valid swap: " +
            $"({firstX}, {firstY}) with " +
            $"({secondX}, {secondY})"
        );

        // Physically animate the two pieces swapping.
        yield return StartCoroutine(
            boardView.AnimateSwap(
                first,
                second,
                swapDuration
            )
        );

        // Keep BoardView's lookup grid and PieceView
        // coordinates synchronized with the model.
        boardView.CommitVisualSwap(
            first,
            second,
            firstX,
            firstY,
            secondX,
            secondY
        );

        ResolutionSequence sequence;

        // =====================================================
        // SPECIAL + SPECIAL COMBO
        // =====================================================

        if (result.HasSpecialCombo)
        {
            Debug.Log(
                $"Special combo: " +
                $"{result.SpecialCombo.Type}"
            );

            sequence =
                boardResolver
                    .ResolveSpecialComboWithSequence(
                        board,
                        result.SpecialCombo,
                        source
                    );
        }

        // =====================================================
        // SINGLE SPECIAL SWAPPED WITH NORMAL PIECE
        // =====================================================

        else if (result.HasDirectSpecialActivation)
        {
            Debug.Log(
                $"Special swap activation at " +
                $"({result.SpecialActivationCell.X}, " +
                $"{result.SpecialActivationCell.Y})"
            );

            sequence =
                boardResolver
                    .ActivateSpecialWithSequence(
                        board,
                        result.SpecialActivationCell,
                        result.ActivationColor,
                        source
                    );
        }

        // =====================================================
        // NORMAL MATCH
        // =====================================================

        else
        {
            sequence =
                boardResolver.ResolveWithSequence(
                    board,
                    result,
                    source
                );
        }

        // =====================================================
        // PLAY ALL RECORDED RESOLUTION ROUNDS
        //
        // Clear
        // ↓
        // Gravity
        // ↓
        // Refill
        // ↓
        // Cascade
        // ↓
        // Repeat
        // =====================================================

        yield return StartCoroutine(
            boardView.PlayResolutionSequence(
                sequence,
                popDuration,
                fallDuration,
                refillDuration
            )
        );

        // Final safety refresh.
        // The model is always the source of truth.
        boardView.RefreshBoard(
            board
        );

        lastSwapSucceeded = true;
        lastClickedPiece = null;
        yield return StartCoroutine(FinishResolutionChain(sequence, "After swap resolution"));
    }

    private IEnumerator ActivateSpecialPieceAnimated(
        PieceView pieceView)
    {
        if (levelEnded) yield break;
        inputLocked = true;

        BoardCell cell =
            board.GetCell(
                pieceView.X,
                pieceView.Y
            );

        if (
            cell.IsEmpty ||
            !cell.Piece.IsSpecial
        )
        {
            inputLocked = false;
            yield break;
        }

        DeselectPiece();

        Debug.Log(
            $"Double-click activating " +
            $"{cell.Piece.Special} at " +
            $"({cell.X}, {cell.Y})"
        );

        ResolutionSequence sequence =
            boardResolver
                .ActivateSpecialWithSequence(
                    board,
                    cell
                );

        yield return StartCoroutine(
            boardView.PlayResolutionSequence(
                sequence,
                popDuration,
                fallDuration,
                refillDuration
            )
        );

        boardView.RefreshBoard(
            board
        );

        lastClickedPiece = null;

        yield return StartCoroutine(FinishResolutionChain(sequence, "After special activation"));
    }

    private IEnumerator FinishResolutionChain(ResolutionSequence sequence, string phase)
    {
        // The outer player action owns input and final win/loss evaluation.
        if (sequence.Source == MoveSource.CascadeBonus) yield break;
        yield return StartCoroutine(TryRunCascadeBonus(sequence));
        if (sequence.Source == MoveSource.Player)
        {
            var generated = boardResolver.GenerateAfterPlayerAction(board, sequence);
            if (generated.Rounds.Count > 0)
            {
                yield return StartCoroutine(boardView.PlayResolutionSequence(generated, popDuration, fallDuration, refillDuration));
                boardView.RefreshBoard(board);
            }
        }
        ResolutionChainCompleted?.Invoke(sequence.Source);
        if (!levelEnded) yield return StartCoroutine(EnsurePlayableBoard(phase));
        inputLocked = levelEnded;
    }

    private IEnumerator TryRunCascadeBonus(ResolutionSequence trigger)
    {
        if (!cascadeBonus.TryBegin(trigger, activeBonusSettings.Enabled,
            Mathf.Max(1, activeBonusSettings.TriggerCascades), Mathf.Max(1, activeBonusSettings.BonusMoves))) yield break;

        inputLocked = true;
        ClearHint();
        DeselectPiece();
        lastClickedPiece = null;
        Debug.Log($"[Hot Streak] Triggered by {trigger.CascadeCount} player cascades! {cascadeBonus.TotalMoves} bonus moves.", this);
        try
        {
            while (cascadeBonus.IsActive)
            {
                yield return StartCoroutine(EnsurePlayableBoard("Before bonus move", true));
                // Never fabricate a match or play on an unfinished resolution.
                if (MatchFinder.FindMatches(board).Count > 0)
                {
                    Debug.LogError("[Hot Streak] Stopped: board still contains unresolved matches.", this);
                    yield break;
                }
                var moves = BoardMoveFinder.FindMatchProducingMoves(board);
                if (moves.Count == 0)
                {
                    Debug.LogWarning("[Hot Streak] Stopped: no match-producing move found after reshuffling.", this);
                    yield break;
                }
                var move = moves[bonusRandom.Next(moves.Count)];
                PieceView first = boardView.GetPieceView(move.First.X, move.First.Y);
                PieceView second = boardView.GetPieceView(move.Second.X, move.Second.Y);
                first.SetSelected(true);
                second.SetSelected(true);
                yield return new WaitForSeconds(Mathf.Max(0f, bonusMoveDelay));
                first.SetSelected(false);
                second.SetSelected(false);
                yield return StartCoroutine(SwapPiecesAnimated(first, second, MoveSource.CascadeBonus));
                if (!lastSwapSucceeded)
                {
                    Debug.LogError("[Hot Streak] Stopped: selected swap was rejected.", this);
                    yield break;
                }
                cascadeBonus.CompleteMove();
                Debug.Log($"[Hot Streak] Completed {cascadeBonus.CompletedMoves}/{cascadeBonus.TotalMoves} bonus moves.", this);
            }
        }
        finally
        {
            cascadeBonus.Stop();
            lastClickedPiece = null;
            // Keep input locked until the parent action decides the level result.
            inputLocked = true;
        }
    }

    private IEnumerator EnsurePlayableBoard(string phase, bool requireMatchProducingMove = false)
    {
        LogBoardMoveStatus(phase);
        // Leave unresolved boards to the resolution system, not the shuffler.
        if (MatchFinder.FindMatches(board).Count > 0)
            yield break;
        for (int y = 0; y < board.Height; y++)
            for (int x = 0; x < board.Width; x++)
                if (board.IsPlayable(x, y) && (board.GetCell(x, y).IsEmpty ||
                    board.GetCell(x, y).Piece.IsMatchProtected))
                    yield break;

        if (requireMatchProducingMove
            ? BoardMoveFinder.FindMatchProducingMoves(board).Count > 0
            : BoardMoveFinder.HasAnyValidMove(board))
            yield break;

        DeselectPiece();
        lastClickedPiece = null;
        Debug.Log("[Board Shuffle] Searching for an arrangement with a usable move.", this);

        bool shuffled = false;
        // Yield between batches so an unlucky search cannot freeze the game.
        for (int batch = 0; batch < 50; batch++)
        {
            if (BoardShuffler.TryShuffle(board, shuffleRandom, 20, requireMatchProducingMove))
            {
                shuffled = true;
                break;
            }
            yield return null;
        }

        if (!shuffled)
        {
            Debug.LogError(
                "[Board Shuffle] No playable arrangement found after 1000 attempts. Original board preserved; restart the level to recover.",
                this
            );
            yield break;
        }

        // Hide the old layout, refresh, then reveal the validated arrangement.
        var scales = new Vector3[board.Width, board.Height];
        for (int y = 0; y < board.Height; y++)
            for (int x = 0; x < board.Width; x++)
                if (board.IsPlayable(x, y))
                    scales[x, y] = boardView.GetPieceView(x, y).transform.localScale;

        float halfDuration = shuffleDuration * 0.5f;
        for (int stage = 0; stage < 2; stage++)
        {
            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / halfDuration);
                float scale = stage == 0 ? 1f - progress : progress;
                for (int y = 0; y < board.Height; y++)
                    for (int x = 0; x < board.Width; x++)
                        if (board.IsPlayable(x, y))
                            boardView.GetPieceView(x, y).transform.localScale = scales[x, y] * scale;
                yield return null;
            }
            if (stage == 0)
            {
                boardView.RefreshBoard(board);
                for (int y = 0; y < board.Height; y++)
                    for (int x = 0; x < board.Width; x++)
                        if (board.IsPlayable(x, y))
                            boardView.GetPieceView(x, y).transform.localScale = Vector3.zero;
            }
        }
        for (int y = 0; y < board.Height; y++)
            for (int x = 0; x < board.Width; x++)
                if (board.IsPlayable(x, y))
                    boardView.GetPieceView(x, y).transform.localScale = scales[x, y];

        Debug.Log("[Board Shuffle] Complete: no immediate matches; playable action available.", this);
        LogBoardMoveStatus("After shuffle");
    }

    private void LogBoardMoveStatus(string phase)
    {
        if (!logBoardMoveChecks)
        {
            return;
        }

        // Only report move availability once the board has settled.
        for (int y = 0; y < board.Height; y++)
        {
            for (int x = 0; x < board.Width; x++)
            {
                if (board.IsPlayable(x, y) && board.GetCell(x, y).IsEmpty)
                {
                    Debug.LogWarning(
                        $"[Board Moves] {phase}: check skipped; board has empty cells.",
                        this
                    );
                    return;
                }
            }
        }

        if (MatchFinder.FindMatches(board).Count > 0)
        {
            Debug.LogWarning(
                $"[Board Moves] {phase}: check skipped; board still contains matches.",
                this
            );
            return;
        }

        if (BoardMoveFinder.HasAnyValidMove(board))
        {
            Debug.Log(
                $"[Board Moves] {phase}: playable action available (swap or special activation).",
                this
            );
        }
        else
        {
            Debug.LogWarning(
                $"[Board Moves] {phase}: DEAD BOARD - no valid moves. Checking for automatic reshuffle.",
                this
            );
        }
    }

    private bool IsSpecialPiece(
        PieceView pieceView)
    {
        BoardCell cell =
            board.GetCell(
                pieceView.X,
                pieceView.Y
            );

        return
            !cell.IsEmpty &&
            cell.Piece.IsSpecial;
    }

    private void SelectPiece(
        PieceView piece)
    {
        selectedPiece = piece;

        selectedPiece.SetSelected(
            true
        );
    }

    private void DeselectPiece()
    {
        if (selectedPiece != null)
        {
            selectedPiece.SetSelected(
                false
            );
        }

        selectedPiece = null;
    }

    private void SelectDifferentPiece(
        PieceView newPiece)
    {
        DeselectPiece();

        SelectPiece(
            newPiece
        );
    }

    private bool AreAdjacent(
        PieceView first,
        PieceView second)
    {
        int horizontalDistance =
            Mathf.Abs(
                first.X - second.X
            );

        int verticalDistance =
            Mathf.Abs(
                first.Y - second.Y
            );

        return
            horizontalDistance +
            verticalDistance == 1;
    }
}