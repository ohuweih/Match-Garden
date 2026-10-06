using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardView : MonoBehaviour
{
    public event System.Action<ResolutionSequence> ResolutionStarted;
    public event System.Action<ResolutionRoundEvent> RoundCleared;
    public event System.Action<ResolutionSequence> ResolutionCompleted;

    private SpecialEffectsView specialEffects;
    private ObjectEffectsView objectEffects;
    private PieceView[,] pieceViews;
    private BoardModel activeBoard;
    private bool[,] visualPlayable;
    private GameObject[,] lockedViews;
    private TextMesh[,] lockedLabels;
    private FrostTileView[,] frostViews;
    private GeneratorView[,] generatorViews;
    private Sprite lockSprite;
    private Texture2D lockTexture;
    private readonly Dictionary<Vector2Int, BirdNestView> nests = new Dictionary<Vector2Int, BirdNestView>();

    [SerializeField]
    private PieceView piecePrefab;

    [SerializeField]
    private float spacing = 1.1f;

    [SerializeField]
    private float cameraPadding = 2.5f;

    [Header("Board Object Art")]
    [SerializeField] private Sprite blockerSprite;
    [SerializeField] private Sprite frostSprite;
    [SerializeField] private Sprite machineSprite;

    [Header("Modular Board Art")]
    [SerializeField] private Sprite boardCellSprite;
    [SerializeField] private Sprite topEdgeSprite;
    [SerializeField] private Sprite bottomEdgeSprite;
    [SerializeField] private Sprite leftEdgeSprite;
    [SerializeField] private Sprite rightEdgeSprite;
    [SerializeField] private Sprite topLeftCornerSprite;
    [SerializeField] private Sprite topRightCornerSprite;
    [SerializeField] private Sprite bottomLeftCornerSprite;
    [SerializeField] private Sprite bottomRightCornerSprite;
    [SerializeField, Range(0.5f, 1.5f)] private float boardCellArtScale = 1.15f;
    [SerializeField, Range(0.5f, 2.0f)] private float boardFrameArtScale = 1f;
    [SerializeField, Range(0f, 0.75f)] private float boardFrameOutset = 0.48f;
    [SerializeField] private Vector2 boardFrameFineOffset = Vector2.zero;
    private Transform boardArtRoot;

    public void ClearBoard()
    {
        StopAllCoroutines();
        if (specialEffects != null) specialEffects.Clear();
        if (objectEffects != null) objectEffects.Clear();
        if (boardArtRoot != null)
        {
            Destroy(boardArtRoot.gameObject);
            boardArtRoot = null;
        }
        if (pieceViews != null)
            foreach (var view in pieceViews)
                if (view != null) { view.gameObject.SetActive(false); Destroy(view.gameObject); }
        if (lockedViews != null)
            foreach (var view in lockedViews)
                if (view != null) { view.SetActive(false); Destroy(view); }
        foreach (var nest in nests.Values)
            if (nest != null) { nest.gameObject.SetActive(false); Destroy(nest.gameObject); }
        nests.Clear();
        if (frostViews != null)
            foreach (var frost in frostViews)
                if (frost != null) { frost.gameObject.SetActive(false); Destroy(frost.gameObject); }
        if (generatorViews != null)
            foreach (var machine in generatorViews)
                if (machine != null) { machine.gameObject.SetActive(false); Destroy(machine.gameObject); }
        generatorViews = null;
        frostViews = null;
        pieceViews = null;
        lockedViews = null;
        lockedLabels = null;
        visualPlayable = null;
        activeBoard = null;
    }

    public void CreateBoard(BoardModel board)
    {
        activeBoard = board;
        pieceViews = new PieceView[board.Width, board.Height];
        visualPlayable = new bool[board.Width, board.Height];
        generatorViews = new GeneratorView[board.Width, board.Height];
        lockedViews = new GameObject[board.Width, board.Height];
        lockedLabels = new TextMesh[board.Width, board.Height];

        float boardWidth = (board.Width - 1) * spacing;
        float boardHeight = (board.Height - 1) * spacing;

        Vector3 offset = new Vector3(
            -boardWidth / 2f,
            -boardHeight / 2f,
            0f
        );

        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                BoardCell cell = board.GetCell(x, y);
                visualPlayable[x, y] = cell.IsPlayable;
                if (cell.IsLocked) CreateLockedTile(cell);
                if (cell.Generator != null) CreateGenerator(cell);
                if (!cell.IsPlayable) continue;

                Vector3 position = new Vector3(
                    x * spacing,
                    y * spacing,
                    0f
                ) + offset;

                PieceView pieceView =
                    Instantiate(piecePrefab, position, Quaternion.identity, transform);

                pieceView.Setup(cell);
                pieceViews[x, y] = pieceView;
            }
        }
        CreateModularBoardArt(board);
        CreateFrostMarkers(board);
        CreateNestMarkers(board);
    }

    private void CreateModularBoardArt(BoardModel board)
    {
        if (boardCellSprite == null) return;

        if (boardArtRoot != null) Destroy(boardArtRoot.gameObject);
        boardArtRoot = new GameObject("Board Art").transform;
        boardArtRoot.SetParent(transform, false);

        var pieceRenderer = piecePrefab.GetComponentInChildren<SpriteRenderer>();
        int layer = pieceRenderer != null ? pieceRenderer.sortingLayerID : 0;
        int pieceOrder = pieceRenderer != null ? pieceRenderer.sortingOrder : 0;
        int cellOrder = pieceOrder - 20;
        int frameOrder = pieceOrder - 19;

        float outset = spacing * boardFrameOutset;
        Vector3 fineOffset = new Vector3(
            boardFrameFineOffset.x * spacing,
            boardFrameFineOffset.y * spacing,
            0f
        );

        for (int y = 0; y < board.Height; y++)
        for (int x = 0; x < board.Width; x++)
        {
            if (!IsBoardArtCell(x, y)) continue;

            Vector3 cellPosition = GetWorldPosition(x, y);

            // The recessed socket stays centered beneath the piece.
            AddBoardSprite(
                $"Cell ({x},{y})",
                boardCellSprite,
                cellPosition,
                layer,
                cellOrder,
                boardCellArtScale,
                false
            );

            bool top = !IsBoardArtCell(x, y + 1);
            bool bottom = !IsBoardArtCell(x, y - 1);
            bool left = !IsBoardArtCell(x - 1, y);
            bool right = !IsBoardArtCell(x + 1, y);

            // V2: frame artwork lives OUTSIDE the cell rather than being
            // crushed underneath it. Each edge is shifted toward the exposed
            // side of the board silhouette.
            if (top)
                AddBoardSprite(
                    $"Top ({x},{y})",
                    topEdgeSprite,
                    cellPosition + Vector3.up * outset + fineOffset,
                    layer, frameOrder, boardFrameArtScale, true
                );

            if (bottom)
                AddBoardSprite(
                    $"Bottom ({x},{y})",
                    bottomEdgeSprite,
                    cellPosition + Vector3.down * outset + fineOffset,
                    layer, frameOrder, boardFrameArtScale, true, 180f
                );

            if (left)
                AddBoardSprite(
                    $"Left ({x},{y})",
                    leftEdgeSprite,
                    cellPosition + Vector3.left * outset + fineOffset,
                    layer, frameOrder, boardFrameArtScale, true
                );

            if (right)
                AddBoardSprite(
                    $"Right ({x},{y})",
                    rightEdgeSprite,
                    cellPosition + Vector3.right * outset + fineOffset,
                    layer, frameOrder, boardFrameArtScale, true
                );

            // Convex corners are shifted diagonally to the actual board
            // intersection instead of being centered on the corner cell.
            if (top && left)
                AddBoardSprite(
                    $"TL ({x},{y})",
                    topLeftCornerSprite,
                    cellPosition + new Vector3(-outset, outset, 0f) + fineOffset,
                    layer, frameOrder + 1, boardFrameArtScale, true
                );

            if (top && right)
                AddBoardSprite(
                    $"TR ({x},{y})",
                    topRightCornerSprite,
                    cellPosition + new Vector3(outset, outset, 0f) + fineOffset,
                    layer, frameOrder + 1, boardFrameArtScale, true
                );

            if (bottom && left)
                AddBoardSprite(
                    $"BL ({x},{y})",
                    topRightCornerSprite,
                    cellPosition + new Vector3(-outset, -outset, 0f) + fineOffset,
                    layer, frameOrder + 1, boardFrameArtScale, true, 180f
                );

            if (bottom && right)
                AddBoardSprite(
                    $"BR ({x},{y})",
                    topLeftCornerSprite,
                    cellPosition + new Vector3(outset, -outset, 0f) + fineOffset,
                    layer, frameOrder + 1, boardFrameArtScale, true, 180f
                );
        }
    }

    private bool IsBoardArtCell(int x, int y)
    {
        if (activeBoard == null ||
            x < 0 || y < 0 ||
            x >= activeBoard.Width || y >= activeBoard.Height)
            return false;

        BoardCell cell = activeBoard.GetCell(x, y);
        return cell.IsPlayable || cell.IsLocked || cell.Generator != null;
    }

    private void AddBoardSprite(
        string name,
        Sprite sprite,
        Vector3 position,
        int layer,
        int order,
        float scaleMultiplier,
        bool preserveSpriteProportions,
        float rotation = 0f)
    {
        if (sprite == null) return;

        var go = new GameObject(name);
        go.transform.SetParent(boardArtRoot, false);
        go.transform.position = position;
        go.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = Color.white;
        sr.sortingLayerID = layer;
        sr.sortingOrder = order;

        Vector2 size = sprite.bounds.size;
        if (size.x <= 0.0001f || size.y <= 0.0001f) return;

        if (!preserveSpriteProportions)
        {
            // Socket art should occupy approximately one logical board cell.
            float maxDimension = Mathf.Max(size.x, size.y);
            float uniformScale = spacing / maxDimension;
            go.transform.localScale =
                Vector3.one * uniformScale * scaleMultiplier;
            return;
        }

        // Frame art keeps the proportions of the sliced source sprite.
        // Horizontal pieces are fitted by width; vertical pieces by height.
        bool horizontal = size.x >= size.y;
        float sourceLength = horizontal ? size.x : size.y;
        float targetLength = spacing * scaleMultiplier;
        float uniformFrameScale = targetLength / sourceLength;
        go.transform.localScale = Vector3.one * uniformFrameScale;
    }

    private void CreateGenerator(BoardCell cell) =>
        CreateGenerator(cell.X, cell.Y, cell.Generator.Type, cell.Generator.RemainingHits);

    private void CreateGenerator(int x, int y, GeneratorType type, int hits)
    {
        var renderer = piecePrefab.GetComponentInChildren<SpriteRenderer>();
        var root = new GameObject($"{type} ({x}, {y})");
        root.transform.SetParent(transform, false);
        root.transform.position = GetWorldPosition(x, y);
        var view = root.AddComponent<GeneratorView>();
        view.Setup(spacing, renderer.sortingLayerID, renderer.sortingOrder + 3, type, hits, machineSprite);
        generatorViews[x, y] = view;
    }

    private IEnumerator PlayPackageChanges(List<PackageChange> changes, float duration)
    {
        if (changes.Count == 0) yield break;
        var cues = new List<ObjectEffectsView.Cue>();
        foreach (var change in changes)
        {
            if (!change.Opened) continue;
            var piece = pieceViews[change.X, change.Y];
            if (piece != null) cues.Add(new ObjectEffectsView.Cue(ObjectSprite(piece.gameObject), ObjectEffectsView.Kind.Package));
        }
        yield return PlayObjectCues(cues);
        foreach (var change in changes)
        {
            var view = pieceViews[change.X, change.Y];
            if (change.Piece == null)
            {
                if (view != null) { view.Hide(); Destroy(view.gameObject); }
                pieceViews[change.X, change.Y] = null;
            }
            else
            {
                if (view == null)
                {
                    view = Instantiate(piecePrefab, GetWorldPosition(change.X, change.Y), Quaternion.identity, transform);
                    pieceViews[change.X, change.Y] = view;
                }
                view.SetCoordinates(change.X, change.Y);
                view.Show(change.Piece);
            }
            Debug.Log(change.Opened ? $"[Package] Opened at ({change.X}, {change.Y}): {change.Content}."
                : $"[Package] Spawned at ({change.X}, {change.Y}).");
        }
    }

    private static SpriteRenderer ObjectSprite(GameObject root)
    {
        if (root == null) return null;
        foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>())
            if (renderer.enabled && renderer.sprite != null) return renderer;
        return null;
    }

    private IEnumerator PlayObjectCues(List<ObjectEffectsView.Cue> cues)
    {
        if (cues.Count == 0) yield break;
        if (objectEffects == null) objectEffects = gameObject.AddComponent<ObjectEffectsView>();
        yield return objectEffects.Play(cues);
    }

    private IEnumerator PlayTerrainChanges(ResolutionRound round)
    {
        var cues = new List<ObjectEffectsView.Cue>();
        foreach (var change in round.LockedTileChanges)
            cues.Add(new ObjectEffectsView.Cue(ObjectSprite(lockedViews[change.X, change.Y]),
                change.RemainingMatches == 0 ? ObjectEffectsView.Kind.Stone : ObjectEffectsView.Kind.Hit));
        foreach (var change in round.FrostChanges)
        {
            var frost = frostViews[change.X, change.Y];
            if (change.LayersRemoved > 0 && frost != null)
                cues.Add(new ObjectEffectsView.Cue(ObjectSprite(frost.gameObject), ObjectEffectsView.Kind.Ice, change.RemainingLayers == 0));
        }
        foreach (var change in round.GeneratorChanges)
        {
            var generator = generatorViews[change.X, change.Y];
            if (!change.Created && generator != null)
                cues.Add(new ObjectEffectsView.Cue(ObjectSprite(generator.gameObject),
                    change.Destroyed ? ObjectEffectsView.Kind.Machine : ObjectEffectsView.Kind.Hit));
        }
        yield return PlayObjectCues(cues);
    }

    private void ApplyGeneratorChanges(List<GeneratorChange> changes, List<PieceView> recycledViews)
    {
        foreach (var change in changes)
        {
            if (change.Created)
            {
                visualPlayable[change.X, change.Y] = false;
                CreateGenerator(change.X, change.Y, change.Type, change.RemainingHits);
                continue;
            }
            var view = generatorViews[change.X, change.Y];
            if (view != null) view.SetHits(change.RemainingHits);
            Debug.Log($"[Generator] {change.Type} at ({change.X}, {change.Y}): {change.RemainingHits} hit(s) remaining.");
            if (!change.Destroyed) continue;
            if (view != null) Destroy(view.gameObject);
            generatorViews[change.X, change.Y] = null;
            visualPlayable[change.X, change.Y] = true;
            CreateModularBoardArt(activeBoard);
            var piece = Instantiate(piecePrefab, transform);
            piece.Hide();
            recycledViews.Add(piece);
        }
    }

    private void CreateFrostMarkers(BoardModel board)
    {
        frostViews = new FrostTileView[board.Width, board.Height];
        for (int y = 0; y < board.Height; y++)
            for (int x = 0; x < board.Width; x++) SetFrostVisual(x, y, board.GetCell(x, y).FrostLayers);
    }

    private void SetFrostVisual(int x, int y, int layers)
    {
        var frost = frostViews[x, y];
        if (frost == null && layers > 0)
        {
            var renderer = piecePrefab.GetComponentInChildren<SpriteRenderer>();
            var root = new GameObject($"Frost ({x}, {y})");
            root.transform.SetParent(transform, false);
            root.transform.position = GetWorldPosition(x, y);
            frost = root.AddComponent<FrostTileView>();
            frost.Setup(spacing, renderer.sortingLayerID, renderer.sortingOrder + 3, layers, frostSprite);
            frostViews[x, y] = frost;
        }
        if (frost != null) frost.SetLayers(layers);
    }

    private void ApplyFrostChanges(List<FrostTileChange> changes)
    {
        foreach (var change in changes)
        {
            SetFrostVisual(change.X, change.Y, change.RemainingLayers);
            Debug.Log($"[Frost] ({change.X}, {change.Y}): {change.PreviousLayers} -> {change.RemainingLayers} layer(s).");
        }
    }

    private void CreateNestMarkers(BoardModel board)
    {
        foreach (var nest in nests.Values)
            if (nest != null) Destroy(nest.gameObject);
        nests.Clear();
        var pieceRenderer = piecePrefab.GetComponentInChildren<SpriteRenderer>();
        for (int y = 0; y < board.Height; y++)
            for (int x = 0; x < board.Width; x++)
            {
                var piece = board.GetCell(x, y).Piece;
                var goal = piece?.RescueGoal;
                if (goal == null || goal.Condition != BirdRescueCondition.ReachDestination) continue;
                var destination = new Vector2Int(goal.DestinationX, goal.DestinationY);
                if (!nests.TryGetValue(destination, out var nest))
                {
                    var root = new GameObject($"Nest ({destination.x}, {destination.y})");
                    root.transform.SetParent(transform, false);
                    root.transform.position = GetWorldPosition(destination.x, destination.y);
                    nest = root.AddComponent<BirdNestView>();
                    nest.Setup(spacing, pieceRenderer.sortingLayerID, pieceRenderer.sortingOrder + 10);
                    nests.Add(destination, nest);
                }
                nest.AddBird(piece.BirdNumber);
            }
    }

    private void RemoveRescuedNest(BirdRescueRecord rescue)
    {
        if (rescue.Goal == null || rescue.Goal.Condition != BirdRescueCondition.ReachDestination) return;
        var destination = new Vector2Int(rescue.Goal.DestinationX, rescue.Goal.DestinationY);
        if (nests.TryGetValue(destination, out var nest) && nest.RemoveBird(rescue.BirdNumber))
        {
            nests.Remove(destination);
            nest.gameObject.SetActive(false);
            Destroy(nest.gameObject);
        }
    }

    public PieceView GetPieceView(
    int x,
    int y)
    {
        return pieceViews[x, y];
    }

    public void CommitVisualSwap(
    PieceView first,
    PieceView second,
    int firstOldX,
    int firstOldY,
    int secondOldX,
    int secondOldY)
    {
        pieceViews[firstOldX, firstOldY] = second;
        pieceViews[secondOldX, secondOldY] = first;

        first.SetCoordinates(
            secondOldX,
            secondOldY
        );

        second.SetCoordinates(
            firstOldX,
            firstOldY
        );
    }

    public Vector3 GetWorldPosition(
    int x,
    int y)
    {
        float boardWidth =
            (pieceViews.GetLength(0) - 1)
            * spacing;

        float boardHeight =
            (pieceViews.GetLength(1) - 1)
            * spacing;

        Vector3 offset =
            new Vector3(
                -boardWidth / 2f,
                -boardHeight / 2f,
                0f
            );

        return new Vector3(
            x * spacing,
            y * spacing,
            0f
        ) + offset;
    }

    public IEnumerator AnimateSwap(
    PieceView first,
    PieceView second,
    float duration)
    {
        Vector3 firstTarget =
            GetWorldPosition(
                second.X,
                second.Y
            );

        Vector3 secondTarget =
            GetWorldPosition(
                first.X,
                first.Y
            );

        StartCoroutine(
            first.MoveTo(
                firstTarget,
                duration
            )
        );

        yield return StartCoroutine(
            second.MoveTo(
                secondTarget,
                duration
            )
        );
    }

    public IEnumerator PlayResolutionSequence(
    ResolutionSequence sequence,
    float popDuration,
    float fallDuration,
    float refillDuration)
    {
        ResolutionStarted?.Invoke(sequence);
        int roundIndex = 0;
        int cascadeNumber = 0;
        foreach (ResolutionRound round in sequence.Rounds)
        {
            List<PieceView> recycledViews =
                new List<PieceView>();

            // -----------------------------------------
            // CLEAR / POP
            // -----------------------------------------

            if (round.ClearedCells.Count > 0)
            {
                if (round.SpecialEffects.Count > 0)
                {
                    if (specialEffects == null) specialEffects = gameObject.AddComponent<SpecialEffectsView>();
                    yield return specialEffects.Play(this, round, spacing, popDuration);
                }
                else
                {
                    yield return StartCoroutine(AnimateMatchPop(round.ClearedCells, popDuration));
                }

                foreach (
                    BoardCell cell
                    in round.ClearedCells
                )
                {
                    PieceView view =
                        pieceViews[
                            cell.X,
                            cell.Y
                        ];

                    if (view == null)
                    {
                        continue;
                    }

                    pieceViews[
                        cell.X,
                        cell.Y
                    ] = null;

                    view.Hide();

                    recycledViews.Add(
                        view
                    );
                }
            }

            if (round.RescuedBirds.Count > 0)
            {
                var rescueCells = new List<BoardCell>();
                foreach (var rescue in round.RescuedBirds)
                    rescueCells.Add(activeBoard.GetCell(rescue.X, rescue.Y));
                yield return StartCoroutine(AnimateMatchPop(rescueCells, popDuration));
                foreach (var rescue in round.RescuedBirds)
                {
                    var view = pieceViews[rescue.X, rescue.Y];
                    pieceViews[rescue.X, rescue.Y] = null;
                    if (view != null)
                    {
                        view.Hide();
                        recycledViews.Add(view);
                    }
                    RemoveRescuedNest(rescue);
                    Debug.Log($"[Bird Rescue] Bird {rescue.BirdNumber} rescued at ({rescue.X}, {rescue.Y}) after {rescue.DistanceTraveled} spaces.");
                }
            }

            yield return StartCoroutine(PlayPackageChanges(round.PackageChanges, popDuration));
            yield return PlayTerrainChanges(round);
            ApplyGeneratorChanges(round.GeneratorChanges, recycledViews);
            foreach (var activation in round.GeneratorActivations)
            {
                var generator = generatorViews[activation.X, activation.Y];
                if (generator != null) yield return StartCoroutine(generator.Pulse());
                Debug.Log($"[Generator] {activation.Type} at ({activation.X}, {activation.Y}) produced after the player action.");
            }
            ApplyFrostChanges(round.FrostChanges);
            if (round.IsMatchRound) cascadeNumber++;
            RoundCleared?.Invoke(new ResolutionRoundEvent(sequence, roundIndex, cascadeNumber));
            ApplyLockedTileChanges(round.LockedTileChanges, recycledViews);

            // -----------------------------------------
            // GRAVITY
            // -----------------------------------------

            if (round.GravityMoves.Count > 0)
            {
                yield return StartCoroutine(
                    AnimateGravityMoves(
                        round.GravityMoves,
                        fallDuration
                    )
                );
            }

            // -----------------------------------------
            // SPAWN / REFILL
            // -----------------------------------------

            if (round.Spawns.Count > 0)
            {
                yield return StartCoroutine(
                    AnimateSpawns(
                        round.Spawns,
                        recycledViews,
                        refillDuration
                    )
                );
            }
            roundIndex++;
        }
        ResolutionCompleted?.Invoke(sequence);
    }

    public IEnumerator AnimateMatchPop(
    System.Collections.Generic.List<BoardCell> cells,
    float duration)
    {
        System.Collections.Generic.List<PieceView> views =
            new System.Collections.Generic.List<PieceView>();

        foreach (BoardCell cell in cells)
        {
            PieceView pieceView =
                GetPieceView(
                    cell.X,
                    cell.Y
                );

            if (pieceView != null)
            {
                views.Add(pieceView);
            }
        }

        float growDuration =
            duration * 0.3f;

        float shrinkDuration =
            duration * 0.7f;

        float elapsed = 0f;

        // --------------------------------------------
        // POP OUT SLIGHTLY
        // --------------------------------------------

        while (elapsed < growDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / growDuration
                );

            Vector3 scale =
                Vector3.Lerp(
                    Vector3.one,
                    Vector3.one * 1.15f,
                    t
                );

            foreach (PieceView view in views)
            {
                view.transform.localScale =
                    scale;
            }

            yield return null;
        }

        // --------------------------------------------
        // SHRINK TO ZERO
        // --------------------------------------------

        elapsed = 0f;

        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / shrinkDuration
                );

            Vector3 scale =
                Vector3.Lerp(
                    Vector3.one * 1.15f,
                    Vector3.zero,
                    t
                );

            foreach (PieceView view in views)
            {
                view.transform.localScale =
                    scale;
            }

            yield return null;
        }

        // Guarantee the animation has completely
        // finished before BoardResolver is allowed
        // to modify/refill the board.
        foreach (PieceView view in views)
        {
            view.transform.localScale =
                Vector3.zero;
        }
    }

    private IEnumerator AnimateGravityMoves(
    List<GravityMove> moves,
    float duration)
    {
        List<PieceView> movingViews =
            new List<PieceView>();

        List<GravityMove> validMoves =
            new List<GravityMove>();

        List<Vector3> startPositions =
            new List<Vector3>();

        List<Vector3> targetPositions =
            new List<Vector3>();

        // =====================================================
        // STEP 1
        //
        // Capture ALL PieceView references before touching
        // pieceViews[,].
        //
        // This is extremely important.
        // =====================================================

        foreach (GravityMove move in moves)
        {
            PieceView view =
                pieceViews[
                    move.FromX,
                    move.FromY
                ];

            if (view == null)
            {
                Debug.LogWarning(
                    $"Gravity could not find PieceView at " +
                    $"({move.FromX}, {move.FromY})"
                );

                continue;
            }

            movingViews.Add(view);

            validMoves.Add(move);

            startPositions.Add(
                view.transform.position
            );

            targetPositions.Add(
                GetWorldPosition(
                    move.ToX,
                    move.ToY
                )
            );
        }

        // =====================================================
        // STEP 2
        //
        // Clear ALL source locations first.
        //
        // Do not move things one-by-one in the lookup array.
        // =====================================================

        foreach (GravityMove move in validMoves)
        {
            pieceViews[
                move.FromX,
                move.FromY
            ] = null;
        }

        // =====================================================
        // STEP 3
        //
        // Now assign every captured PieceView to its
        // destination cell.
        // =====================================================

        for (int i = 0; i < validMoves.Count; i++)
        {
            GravityMove move =
                validMoves[i];

            PieceView view =
                movingViews[i];

            pieceViews[
                move.ToX,
                move.ToY
            ] = view;

            view.SetCoordinates(
                move.ToX,
                move.ToY
            );
        }

        // =====================================================
        // STEP 4
        //
        // Animate all falling pieces simultaneously.
        // =====================================================

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            // Slight ease-in makes falling look
            // more like gravity instead of sliding.
            float easedT =
                t * t;

            for (int i = 0; i < movingViews.Count; i++)
            {
                movingViews[i].transform.position =
                    Vector3.Lerp(
                        startPositions[i],
                        targetPositions[i],
                        easedT
                    );
            }

            yield return null;
        }

        // =====================================================
        // STEP 5
        //
        // Snap exactly to final positions.
        // =====================================================

        for (int i = 0; i < movingViews.Count; i++)
        {
            movingViews[i].transform.position =
                targetPositions[i];
        }
    }

    private IEnumerator AnimateSpawns(
    List<SpawnRecord> spawns,
    List<PieceView> recycledViews,
    float duration)
    {
        List<PieceView> views =
            new List<PieceView>();

        List<Vector3> startPositions =
            new List<Vector3>();

        List<Vector3> targetPositions =
            new List<Vector3>();

        Debug.Log(
            $"Animating {spawns.Count} spawns " +
            $"with {recycledViews.Count} recycled views."
        );

        for (int i = 0; i < spawns.Count; i++)
        {
            SpawnRecord spawn =
                spawns[i];

            PieceView view;

            // Prefer recycling a piece that was just cleared.
            if (recycledViews.Count > 0)
            {
                int lastIndex =
                    recycledViews.Count - 1;

                view =
                    recycledViews[lastIndex];

                recycledViews.RemoveAt(
                    lastIndex
                );
            }
            else
            {
                // Safety fallback:
                // if recycling bookkeeping ever comes up short,
                // create another visual instead of leaving a hole.
                view =
                    Instantiate(
                        piecePrefab,
                        transform
                    );

                Debug.LogWarning(
                    $"Created fallback PieceView for spawn " +
                    $"at ({spawn.X}, {spawn.Y})."
                );
            }

            view.SetCoordinates(
                spawn.X,
                spawn.Y
            );

            view.Show(
                spawn.Color,
                spawn.Special
            );

            Vector3 target =
                GetWorldPosition(
                    spawn.X,
                    spawn.Y
                );

            // Start above the board.
            Vector3 start =
                target +
                GetSpawnOffset(
                    spawn
                );

            view.transform.position =
                start;
            view.transform.localScale = Vector3.zero;

            pieceViews[
                spawn.X,
                spawn.Y
            ] = view;

            views.Add(view);

            startPositions.Add(
                start
            );

            targetPositions.Add(
                target
            );
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            for (int i = 0; i < views.Count; i++)
            {
                views[i].transform.localScale = Vector3.one * t;
                views[i].transform.position =
                    Vector3.Lerp(
                        startPositions[i],
                        targetPositions[i],
                        t
                    );
            }

            yield return null;
        }

        for (int i = 0; i < views.Count; i++)
        {
            views[i].transform.localScale = Vector3.one;
            views[i].transform.position =
                targetPositions[i];
        }
    }

    private Vector3 GetSpawnOffset(SpawnRecord spawn)
    {
        int dx = 0, dy = 0;
        switch (spawn.EntryDirection)
        {
            case GravityDirection.Down: dy = 1; break;
            case GravityDirection.Up: dy = -1; break;
            case GravityDirection.Left: dx = 1; break;
            case GravityDirection.Right: dx = -1; break;
        }
        // Spawn within this contiguous section, never through a dead tile.
        int steps = 0;
        while ((steps + 1) * spacing <= 3f &&
            IsVisuallyPlayable(spawn.X + dx * (steps + 1), spawn.Y + dy * (steps + 1)))
            steps++;
        return new Vector3(dx, dy, 0f) * (steps * spacing);
    }

    private bool IsVisuallyPlayable(int x, int y)
    {
        return x >= 0 && y >= 0 && x < visualPlayable.GetLength(0) &&
            y < visualPlayable.GetLength(1) && visualPlayable[x, y];
    }

    private void CreateLockedTile(BoardCell cell)
    {
        if (lockSprite == null)
        {
            lockTexture = new Texture2D(1, 1);
            lockTexture.SetPixel(0, 0, Color.white);
            lockTexture.Apply();
            lockSprite = Sprite.Create(lockTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
        var root = new GameObject($"Locked Tile ({cell.X}, {cell.Y})");
        root.transform.SetParent(transform, false);
        root.transform.position = GetWorldPosition(cell.X, cell.Y);
        lockedViews[cell.X, cell.Y] = root;

        var plate = new GameObject("Plate");
        plate.transform.SetParent(root.transform, false);
        plate.transform.localScale = Vector3.one * spacing * 0.85f;
        var renderer = plate.AddComponent<SpriteRenderer>();
        renderer.sprite = blockerSprite != null ? blockerSprite : lockSprite;
        renderer.color = blockerSprite != null ? Color.white : new Color(0.18f, 0.22f, 0.28f);
        renderer.sortingOrder = 5;

        var labelObject = new GameObject("Matches remaining");
        labelObject.transform.SetParent(root.transform, false);
        var label = labelObject.AddComponent<TextMesh>();
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 48;
        label.characterSize = spacing * 0.055f;
        label.fontStyle = FontStyle.Bold;
        labelObject.transform.localPosition = new Vector3(spacing * .25f, -spacing * .25f, -.1f);
        label.color = new Color(.12f, .23f, .16f);
        label.text = cell.MatchesUntilUnlock.ToString();
        var labelRenderer = label.GetComponent<MeshRenderer>();
        labelRenderer.sharedMaterial = label.font.material;
        labelRenderer.sortingOrder = 6;
        lockedLabels[cell.X, cell.Y] = label;
    }

    private void ApplyLockedTileChanges(List<LockedTileChange> changes, List<PieceView> recycledViews)
    {
        foreach (LockedTileChange change in changes)
        {
            if (change.RemainingMatches > 0)
            {
                if (lockedLabels[change.X, change.Y] != null)
                    lockedLabels[change.X, change.Y].text = change.RemainingMatches.ToString();
                Debug.Log($"[Locked Tile] ({change.X}, {change.Y}): {change.RemainingMatches} match(es) remaining.");
                continue;
            }

            visualPlayable[change.X, change.Y] = true;
            if (lockedViews[change.X, change.Y] != null)
            {
                lockedViews[change.X, change.Y].SetActive(false);
                Destroy(lockedViews[change.X, change.Y]);
                lockedViews[change.X, change.Y] = null;
                lockedLabels[change.X, change.Y] = null;
            }
            CreateModularBoardArt(activeBoard);

            // The board grew by one piece: supply one extra view for spawn playback.
            PieceView newView = Instantiate(piecePrefab, transform);
            newView.Hide();
            recycledViews.Add(newView);
            Debug.Log($"[Locked Tile] ({change.X}, {change.Y}) unlocked!");
        }
    }

    private void OnDestroy()
    {
        if (lockSprite != null) Destroy(lockSprite);
        if (lockTexture != null) Destroy(lockTexture);
    }

    public void FitCamera(BoardModel board)
    {
        Camera camera = Camera.main;
        if (camera != null && camera.orthographic)
        {
            camera.orthographicSize = Mathf.Max(
                board.Height * spacing * 0.5f + cameraPadding,
                (board.Width * spacing * 0.5f + cameraPadding) / camera.aspect);
        }
    }

    public void RefreshBoard(BoardModel board)
    {
        if (frostViews != null)
            for (int y = 0; y < board.Height; y++)
                for (int x = 0; x < board.Width; x++)
                    SetFrostVisual(x, y, board.GetCell(x, y).FrostLayers);
        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                BoardCell cell =
                    board.GetCell(x, y);

                PieceView pieceView =
                    pieceViews[x, y];

                // Empty model cell: nothing should be shown.
                if (cell.IsEmpty)
                {
                    if (pieceView != null)
                    {
                        pieceView.Hide();
                    }

                    continue;
                }

                // The model contains a piece, but our visual
                // lookup lost its PieceView during animation.
                // Re-create one so the visual layer heals itself.
                if (pieceView == null)
                {
                    Vector3 position =
                        GetWorldPosition(x, y);

                    pieceView =
                        Instantiate(
                            piecePrefab,
                            position,
                            Quaternion.identity,
                            transform
                        );

                    pieceView.Setup(cell);

                    pieceViews[x, y] =
                        pieceView;

                    Debug.LogWarning(
                        $"Re-created missing PieceView at ({x}, {y})."
                    );

                    continue;
                }

                pieceView.SetCoordinates(
                    x,
                    y
                );

                pieceView.transform.position =
                    GetWorldPosition(x, y);

                pieceView.Refresh(cell);
            }
        }
    }
}