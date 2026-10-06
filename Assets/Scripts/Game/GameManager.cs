using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private BoardView boardView;

    [SerializeField]
    private InputController inputController;

    [Tooltip("When assigned, this asset supplies the level rules. Otherwise the scene settings below are used.")]
    [SerializeField] private LevelDefinition levelDefinition;

    [Tooltip("Assign to enable ordered levels and saved progress. Leave empty for single-level testing.")]
    [SerializeField] private LevelCatalog levelCatalog;

    [Tooltip("Level asset used for Free Play. Its refill mode is overridden by the player's selected play style.")]
    [SerializeField] private LevelDefinition freePlayLevel;

    public LevelDefinition AssignedLevel => levelDefinition;
    public LevelCatalog Catalog => levelCatalog;
    public static string ProgressDirectory => Application.persistentDataPath;
    private CampaignProgress campaign;
    private bool hasCampaignRefillOverride;
    private RefillMode campaignRefillOverride = RefillMode.Normal;
    private bool freePlayActive;
    private RefillMode freePlayRefillMode = RefillMode.Normal;
    private LevelDefinition currentLevel;
    private bool campaignCompleteScreen;
    private string campaignLoadError;

    [SerializeField, Min(1)] private int width = 8;
    [SerializeField, Min(1)] private int height = 8;
    [SerializeField] private BoardShape shape = BoardShape.Rectangle;
    [Tooltip("Extra dead tiles. Coordinates start at (0, 0) in the bottom-left.")]
    [SerializeField] private Vector2Int[] blockedCells = new Vector2Int[0];
    [Tooltip("Tiles that unlock after adjacent matches. Overrides the shape and Blocked Cells at these coordinates.")]
    [SerializeField] private Vector2Int[] lockedCells = new Vector2Int[0];
    [SerializeField, Min(1)] private int matchesToUnlock = 2;
    [SerializeField] private bool fitCameraToBoard = true;
    [Tooltip("Stationary frost on playable cells; one or two layers per cell.")]
    [SerializeField] private FrostPlacement[] frostCells = new FrostPlacement[0];
    [SerializeField] private GeneratorPlacement[] generators = new GeneratorPlacement[0];
    [SerializeField] private PackageSettings packages = new PackageSettings();

    [Tooltip("Tier 1: most common, 1x points. Tier 6: rarest, 6x points. Include every color once.")]
    [SerializeField] private ColorRaritySetting[] colorRarity = ColorRarityTable.CreateDefaults();
    [Tooltip("Base points multiplied by the cleared piece's color tier.")]
    [SerializeField, Min(0)] private int pointsPerPiece = 10;
    [SerializeField] private bool awardOpeningPoints = true;

    [Tooltip("Complete all objectives within the move budget. Disable for unlimited play.")]
    [SerializeField] private bool useMoveLimit = false;
    [SerializeField, Min(1)] private int moveLimit = 30;
    [SerializeField] private bool countOpeningCascadesForObjectives = true;
    [SerializeField] private ObjectiveDefinition[] objectives = new ObjectiveDefinition[]
    {
        new ObjectiveDefinition { Type = ObjectiveType.Score, Target = 500 },
        new ObjectiveDefinition { Type = ObjectiveType.ClearColor, Color = PieceType.Red, Target = 20 }
    };

    [Tooltip("Bird placeholders carried by gravity. Coordinates start at the bottom-left (0, 0).")]
    [SerializeField] private Vector2Int[] movableObjectiveCells = new Vector2Int[0];

    [Tooltip("Applies to birds in Movable Objective Cells unless overridden below.")]
    [SerializeField] private BirdRescueSettings defaultBirdRescue = new BirdRescueSettings();
    [SerializeField] private BirdRescueOverride[] birdRescueOverrides = new BirdRescueOverride[0];

    private BoardModel board;

    private void OnValidate()
    {
        if (objectives != null)
            foreach (var objective in objectives)
                if (objective != null) objective.Target = Mathf.Max(1, objective.Target);
        moveLimit = Mathf.Max(1, moveLimit);
        matchesToUnlock = Mathf.Max(1, matchesToUnlock);
        pointsPerPiece = Mathf.Max(0, pointsPerPiece);
        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);
    }

    // Capture the live Inspector values, including unsaved scene edits.
    // Return independent data so creating/editing an asset cannot change this scene.
    public LevelSettings CaptureSceneSettings()
    {
        return new LevelSettings
        {
            Width = width, Height = height, Shape = shape,
            BlockedCells = blockedCells, LockedCells = lockedCells, MatchesToUnlock = matchesToUnlock,
            FrostCells = frostCells, Generators = generators, Packages = packages,
            ColorRarity = colorRarity, PointsPerPiece = pointsPerPiece, AwardOpeningPoints = awardOpeningPoints,
            UseMoveLimit = useMoveLimit, MoveLimit = moveLimit,
            CountOpeningCascadesForObjectives = countOpeningCascadesForObjectives, Objectives = objectives,
            MovableObjectiveCells = movableObjectiveCells, DefaultBirdRescue = defaultBirdRescue,
            BirdRescueOverrides = birdRescueOverrides,
            RefillMode = inputController != null ? inputController.SceneRefillMode : RefillMode.Normal,
            CascadeBonus = inputController != null ? inputController.CaptureSceneCascadeBonus() : new CascadeBonusSettings()
        }.Copy();
    }

    public LevelSettings CaptureActiveSettings()
    {
        if (freePlayActive)
        {
            LevelSettings freePlaySettings =
                freePlayLevel != null ? freePlayLevel.CreateSettings() : CaptureSceneSettings();

            freePlaySettings.RefillMode = freePlayRefillMode;
            return freePlaySettings;
        }

        var level = campaign != null ? currentLevel : levelDefinition;

        LevelSettings settings =
            level != null ? level.CreateSettings() : CaptureSceneSettings();

        if (campaign != null && hasCampaignRefillOverride)
            settings.RefillMode = campaignRefillOverride;

        return settings;
    }

    public void StartFreePlay(RefillMode refillMode)
    {
        if (freePlayLevel == null)
        {
            Debug.LogError("[Free Play] Assign a Free Play Level on GameManager before starting Free Play.", this);
            return;
        }

        campaign = null;
        currentLevel = null;
        campaignLoadError = null;
        campaignCompleteScreen = false;
        hasCampaignRefillOverride = false;
        freePlayActive = true;
        freePlayRefillMode = refillMode;

        Debug.Log($"[Free Play] Starting {freePlayLevel.name} in {refillMode} mode.", this);
        LoadLevel();
    }

    public bool PrepareCampaign(RefillMode refillMode)
    {
        freePlayActive = false;
        if (levelCatalog == null)
        {
            Debug.LogError("[Campaign] GameManager needs a Level Catalog.", this);
            return false;
        }

        campaignLoadError = null;
        campaignCompleteScreen = false;
        currentLevel = null;

        try
        {
            string saveFileName = GetCampaignSaveFileName(refillMode);
            campaign = new CampaignProgress(
                levelCatalog,
                new ProgressFileStore(ProgressDirectory, saveFileName));

            if (!string.IsNullOrEmpty(campaign.LoadWarning))
                Debug.LogWarning("[Save] " + campaign.LoadWarning, this);

            if (!campaign.CanSave)
                throw new System.InvalidOperationException(campaign.LoadWarning);
        }
        catch (System.Exception error) when
            (error is System.ArgumentException || error is System.InvalidOperationException)
        {
            campaign = null;
            campaignLoadError = error.Message;
            if (inputController != null) inputController.EndLevel();
            Debug.LogError($"[Save] Cannot open {refillMode} campaign: {campaignLoadError}", this);
            return false;
        }

        hasCampaignRefillOverride = true;
        campaignRefillOverride = refillMode;
        return true;
    }

    public void StartCampaign(RefillMode refillMode)
    {
        if (!PrepareCampaign(refillMode)) return;
        ContinueCampaign();
    }

    public void ContinueCampaign()
    {
        if (campaign == null) return;
        currentLevel = campaign.NextUnfinished;
        if (currentLevel == null)
        {
            Debug.Log("[Campaign] All campaign levels are complete. Choose a completed level to replay.", this);
            return;
        }

        Debug.Log($"[Campaign] Continuing with {currentLevel.name} in {campaignRefillOverride} mode.", this);
        LoadLevel();
    }

    public int CampaignLevelCount => levelCatalog != null ? levelCatalog.CreateLevelList().Length : 0;
    public int CampaignCompletedCount => campaign != null ? campaign.CompletedCount : 0;
    public bool CampaignAllComplete => campaign != null && campaign.AllComplete;

    public bool IsCampaignLevelCompleted(int index)
    {
        if (campaign == null || levelCatalog == null) return false;
        var levels = levelCatalog.CreateLevelList();
        return index >= 0 && index < levels.Length && campaign.IsCompleted(levels[index]);
    }

    public bool IsCampaignLevelUnlocked(int index)
    {
        if (campaign == null || levelCatalog == null) return false;
        var levels = levelCatalog.CreateLevelList();
        if (index < 0 || index >= levels.Length) return false;
        if (campaign.IsCompleted(levels[index])) return true;
        return campaign.NextUnfinished == levels[index];
    }

    public void StartCampaignLevel(int index)
    {
        if (!IsCampaignLevelUnlocked(index)) return;
        var levels = levelCatalog.CreateLevelList();
        currentLevel = levels[index];
        campaignCompleteScreen = false;
        Debug.Log($"[Campaign] Loading Level {index + 1} ({currentLevel.name}) in {campaignRefillOverride} mode.", this);
        LoadLevel();
    }

    public static string GetCampaignSaveFileName(RefillMode refillMode)
    {
        switch (refillMode)
        {
            case RefillMode.Classic: return "progress_classic.json";
            case RefillMode.Chaos: return "progress_chaos.json";
            default: return "progress_normal.json";
        }
    }

    public void RetryCurrentLevel()
    {
        if (board == null) return;
        LoadLevel();
    }

    public void QuitToTitle()
    {
        // Results belong to the level and must close before showing the title menu.
        GetComponent<ResultScreenPresenter>()?.Hide();

        if (inputController != null)
            inputController.EndLevel();

        GameplayHudPresenter gameplayHud = GetComponent<GameplayHudPresenter>();
        if (gameplayHud != null)
            gameplayHud.HideImmediate();

        if (boardView != null && board != null)
        {
            inputController?.PrepareForRestart();
            boardView.ClearBoard();
            board = null;
        }

        campaign = null;
        currentLevel = null;
        freePlayActive = false;
        hasCampaignRefillOverride = false;
        campaignCompleteScreen = false;
        campaignLoadError = null;

        TitleMenuController menu = FindFirstObjectByType<TitleMenuController>(FindObjectsInactive.Include);
        if (menu != null)
            menu.ReturnToTitle();
        else
            Debug.LogError("[Menu] TitleMenuController was not found.", this);
    }

    public void RestartLevel()
    {
        var progress = GetComponent<LevelProgressController>();
        if (progress == null || progress.Model == null || !progress.Model.IsFinished) return;
        LoadLevel();
    }

    private void Start()
    {
        // Milestone 6: the title menu is now the true entry point.
        // Campaign progress is loaded only after the player chooses
        // Campaign + Classic/Normal/Chaos.
        campaign = null;
        currentLevel = null;
        campaignCompleteScreen = false;
        campaignLoadError = null;
        hasCampaignRefillOverride = false;
        freePlayActive = false;
    }

    private void SaveCampaignWin()
    {
        var progress = GetComponent<LevelProgressController>();
        if (campaign == null || currentLevel == null || progress.Model.Status != LevelAttemptStatus.Won) return;
        if (!campaign.TryComplete(currentLevel, out var error))
        {
            Debug.LogError("[Save] Could not save progress: " + error, this);
            progress.ConfigureWinResult("Level complete!", "Progress could not be saved. Check storage access and try again.", "Retry Save", SaveCampaignWin);
            return;
        }
        Debug.Log($"[Save] Completed {currentLevel.name}. Progress saved locally.", this);
        if (campaign.AllComplete)
            progress.ConfigureWinResult("All levels complete!", "Your progress is saved.", "Replay Level", RestartLevel);
        else
            progress.ConfigureWinResult("Level complete!", "Progress saved. The next level is ready.", "Next Level", AdvanceCampaign);
    }

    public void AdvanceCampaign()
    {
        var progress = GetComponent<LevelProgressController>();
        if (campaign == null || progress == null || progress.Model.Status != LevelAttemptStatus.Won) return;
        // Also guards programmatic calls after a failed save.
        if (!campaign.TryComplete(currentLevel, out var error)) { Debug.LogError("[Save] " + error, this); return; }
        var next = campaign.NextUnfinished;
        if (next == null) return;
        currentLevel = next;
        LoadLevel();
    }

    private void OnGUI()
    {
        if (campaignLoadError != null)
        {
            if (GameHud.Result("Campaign unavailable", "Check the Console for level or save-file details. Existing saves were preserved.", "Retry Load")) Start();
        }
        else if (campaignCompleteScreen)
        {
            if (GameHud.Result("All levels complete!", "Your progress is saved.", "Replay Last Level"))
            {
                campaignCompleteScreen = false;
                currentLevel = campaign.LastLevel;
                LoadLevel();
            }
        }
    }

    private void LoadLevel()
    {
        if (boardView == null || inputController == null)
        {
            Debug.LogError("GameManager needs Board View and Input Controller references.", this);
            return;
        }
        LevelSettings settings = CaptureActiveSettings();
        LevelBuildResult result;
        try { result = LevelBuilder.Build(settings, message => Debug.LogWarning(message, this), prepareCampaignOpening: campaign != null && !freePlayActive); }
        catch (System.ArgumentException error)
        {
            Debug.LogError($"Unable to load level: {error.Message}", this);
            return;
        }
        // Build successfully before removing the previous level's visuals.
        if (board != null)
        {
            inputController.PrepareForRestart();
            boardView.ClearBoard();
        }
        board = result.Board;
        boardView.CreateBoard(board);

        // Milestone 5A: floating HUD with a left Hot Streak rail.
        // Top 18% is reserved for status/objective chips and bottom 11% for actions.
        // The board owns the middle 71% and is fitted only inside that rectangle.

        if (fitCameraToBoard) boardView.FitCamera(board);

        var attempt = new LevelAttemptModel(settings.UseMoveLimit, settings.MoveLimit, campaign != null || freePlayActive);
        ScoreController scoring = GetComponent<ScoreController>();
        if (scoring == null) scoring = gameObject.AddComponent<ScoreController>();
        scoring.Setup(boardView, settings.PointsPerPiece, settings.AwardOpeningPoints, result.ColorRarity, attempt);
        ObjectiveController tracker = GetComponent<ObjectiveController>();
        if (tracker == null) tracker = gameObject.AddComponent<ObjectiveController>();
        tracker.Setup(scoring, LevelBuilder.CreateObjectives(settings), settings.CountOpeningCascadesForObjectives,
            LevelBuilder.CountGenerators(settings, GeneratorType.FrostMachine));
        LevelProgressController progress = GetComponent<LevelProgressController>();
        if (progress == null) progress = gameObject.AddComponent<LevelProgressController>();
        progress.Setup(boardView, inputController, tracker, attempt, RestartLevel, campaign == null ? (System.Action)null : SaveCampaignWin);
        inputController.Setup(board, boardView, settings.RefillMode, settings.CascadeBonus, campaign, currentLevel, settings.Packages);

        var loadedLevel = freePlayActive ? freePlayLevel : (campaign != null ? currentLevel : levelDefinition);
        string levelName = loadedLevel != null ? loadedLevel.name : "Scene settings";

        string displayTitle = freePlayActive
            ? $"FREE PLAY - {freePlayRefillMode.ToString().ToUpperInvariant()}"
            : GetLevelDisplayTitle(loadedLevel);

        GameplayHudPresenter gameplayHud = GetComponent<GameplayHudPresenter>();
        if (gameplayHud == null) gameplayHud = gameObject.AddComponent<GameplayHudPresenter>();
        gameplayHud.Setup(
            displayTitle, scoring, tracker, progress, inputController,
            campaign != null, RetryCurrentLevel, QuitToTitle);

        ResultScreenPresenter resultScreen = GetComponent<ResultScreenPresenter>();
        if (resultScreen == null) resultScreen = gameObject.AddComponent<ResultScreenPresenter>();
        resultScreen.Setup(progress, scoring, gameplayHud, campaign != null, QuitToTitle);

        LevelIntroPresenter intro = GetComponent<LevelIntroPresenter>();
        if (intro == null) intro = gameObject.AddComponent<LevelIntroPresenter>();
        intro.Show(
            displayTitle,
            LevelBuilder.CreateObjectives(settings),
            settings.UseMoveLimit,
            settings.MoveLimit,
            () =>
            {
                gameplayHud.Show();
                inputController.BeginLevel();
            }
        );

        Debug.Log($"[Level] Loaded {levelName}: {settings.Width} x {settings.Height}, {settings.Shape}, {settings.RefillMode} refill.", this);
    }

    private string GetLevelDisplayTitle(LevelDefinition loadedLevel)
    {
        if (campaign != null && levelCatalog != null && loadedLevel != null)
        {
            LevelDefinition[] levels = levelCatalog.CreateLevelList();
            int index = System.Array.IndexOf(levels, loadedLevel);
            if (index >= 0) return $"LEVEL {index + 1}";
        }

        return loadedLevel != null ? loadedLevel.name.ToUpperInvariant() : "LEVEL";
    }
}
