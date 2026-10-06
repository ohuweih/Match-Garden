using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bridge between the existing game systems and the scene-authored Canvas HUD.
/// It never repositions the authored HUD. Layout stays under your control in Unity.
/// Temporary Bank popup/Hot Streak announcement remain runtime UI until their later redesign.
/// </summary>
public sealed class GameplayHudPresenter : MonoBehaviour
{
    public static bool IsPlayerHudActive { get; private set; }
    private static GameplayHudPresenter activePresenter;

    private EditableGameplayHud hud;
    private readonly List<TMP_Text> objectiveCardTexts = new List<TMP_Text>();

    private float displayedFill;
    private float targetFill;
    private int lastCascadeCount = -1;
    private bool lastBonusActive;
    private int lastBonusCompletedMoves = -1;
    private float pulseTimer;
    private Vector3 hotStreakBaseScale = Vector3.one;

    private TMP_Text streakAnnouncement;
    private float announcementTimer;

    // Scene-authored sliding Bank drawer.
    private Vector2 bankDrawerOpenPosition;
    private Vector2 bankDrawerClosedPosition;
    private bool bankDrawerPositionsReady;
    private CanvasGroup bankDrawerCanvasGroup;
    private const float BankDrawerSlideSpeed = 1800f;

    private ScoreController scoring;
    private ObjectiveController objectives;
    private LevelProgressController progress;
    private InputController input;
    private string levelTitle;
    private bool bankEnabled;
    private Action retryAction;
    private Action quitAction;
    private bool pauseMenuOpen;

    private void Awake()
    {
        // Only one presenter may drive the player HUD.
        if (activePresenter != null && activePresenter != this)
        {
            Debug.LogWarning("[HUD] Duplicate GameplayHudPresenter disabled.", this);
            enabled = false;
            return;
        }

        activePresenter = this;
    }

    public void Setup(
        string title,
        ScoreController scoreController,
        ObjectiveController objectiveController,
        LevelProgressController progressController,
        InputController inputController,
        bool enableBank,
        Action onRetry,
        Action onQuit)
    {
        levelTitle = string.IsNullOrWhiteSpace(title) ? "LEVEL" : title;
        scoring = scoreController;
        objectives = objectiveController;
        progress = progressController;
        input = inputController;
        bankEnabled = enableBank;
        retryAction = onRetry;
        quitAction = onQuit;

        BindSceneHud();
        HideImmediate();
    }

    public void Show()
    {
        if (!BindSceneHud())
        {
            Debug.LogError(
                "[HUD] EditableGameplayHud was not found/configured. " +
                "Check the GameplayHUDCanvas references.", this);
            return;
        }

        if (hud.hudRoot != null) hud.hudRoot.SetActive(true);
        SetPaused(false);

        Canvas canvas = hud.GetComponent<Canvas>();
        if (canvas != null) canvas.enabled = true;

        GraphicRaycaster raycaster = hud.GetComponent<GraphicRaycaster>();
        if (raycaster != null) raycaster.enabled = true;

        WireButtons();
        BuildObjectiveList();
        EnsureTransientUi();

        IsPlayerHudActive = true;
        Refresh();
    }

    public void HideImmediate()
    {
        // Never allow a hidden/result/title screen to inherit a paused input state.
        SetPaused(false);
        IsPlayerHudActive = false;

        if (hud == null) BindSceneHud();
        if (hud != null)
        {
            if (hud.hudRoot != null) hud.hudRoot.SetActive(false);

            Canvas canvas = hud.GetComponent<Canvas>();
            if (canvas != null) canvas.enabled = false;

            GraphicRaycaster raycaster = hud.GetComponent<GraphicRaycaster>();
            if (raycaster != null) raycaster.enabled = false;
        }

        displayedFill = 0f;
        targetFill = 0f;
        lastCascadeCount = -1;
        lastBonusActive = false;
        lastBonusCompletedMoves = -1;
        pulseTimer = 0f;
        announcementTimer = 0f;
    }

    private void OnDestroy()
    {
        if (activePresenter == this)
        {
            activePresenter = null;
            IsPlayerHudActive = false;
        }
    }

    private void Update()
    {
        if (!IsPlayerHudActive || hud == null) return;
        Refresh();
        AnimateHotStreak();
        AnimateBankDrawer();
    }

    private bool BindSceneHud()
    {
        if (hud != null) return hud.IsConfigured;

        EditableGameplayHud[] allHuds =
            FindObjectsByType<EditableGameplayHud>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (allHuds == null || allHuds.Length == 0)
            return false;

        // Prefer an active scene-authored HUD. If there are duplicates, only the
        // selected HUD is allowed to render or receive clicks.
        for (int i = 0; i < allHuds.Length; i++)
        {
            if (allHuds[i] != null && allHuds[i].gameObject.activeInHierarchy)
            {
                hud = allHuds[i];
                break;
            }
        }

        if (hud == null)
            hud = allHuds[0];

        for (int i = 0; i < allHuds.Length; i++)
        {
            EditableGameplayHud other = allHuds[i];
            if (other == null || other == hud) continue;

            Canvas otherCanvas = other.GetComponent<Canvas>();
            if (otherCanvas != null) otherCanvas.enabled = false;

            GraphicRaycaster otherRaycaster = other.GetComponent<GraphicRaycaster>();
            if (otherRaycaster != null) otherRaycaster.enabled = false;

            Debug.LogWarning(
                $"[HUD] Disabled duplicate EditableGameplayHud on '{other.gameObject.name}'.",
                other);
        }

        if (hud.hotStreakRoot != null)
            hotStreakBaseScale = hud.hotStreakRoot.localScale;

        return hud.IsConfigured;
    }

    private void WireButtons()
    {
        if (hud.hintButton != null)
        {
            hud.hintButton.onClick.RemoveAllListeners();
            hud.hintButton.onClick.AddListener(() => input?.ShowHint());
        }

        if (hud.bankButton != null)
        {
            hud.bankButton.onClick.RemoveAllListeners();
            hud.bankButton.onClick.AddListener(() => input?.ToggleBankPanel());
        }

        if (hud.pauseButton != null)
        {
            hud.pauseButton.onClick.RemoveAllListeners();
            hud.pauseButton.onClick.AddListener(() => SetPaused(true));
        }

        if (hud.resumeButton != null)
        {
            hud.resumeButton.onClick.RemoveAllListeners();
            hud.resumeButton.onClick.AddListener(() => SetPaused(false));
        }

        if (hud.retryButton != null)
        {
            hud.retryButton.onClick.RemoveAllListeners();
            hud.retryButton.onClick.AddListener(() =>
            {
                SetPaused(false);
                retryAction?.Invoke();
            });
        }

        if (hud.quitButton != null)
        {
            hud.quitButton.onClick.RemoveAllListeners();
            hud.quitButton.onClick.AddListener(() =>
            {
                SetPaused(false);
                quitAction?.Invoke();
            });
        }

        WireBankSlot(hud.hLineSlot, SpecialType.LineHorizontal);
        WireBankSlot(hud.vLineSlot, SpecialType.LineVertical);
        WireBankSlot(hud.bombSlot, SpecialType.Bomb);
        WireBankSlot(hud.targetSlot, SpecialType.Target);
        WireBankSlot(hud.colorClearSlot, SpecialType.ColorClear);

        if (hud.saveSelectedButton != null)
        {
            hud.saveSelectedButton.onClick.RemoveAllListeners();
            hud.saveSelectedButton.onClick.AddListener(() => input?.SaveSelectedSpecial());
        }

        if (hud.settingsButton != null)
        {
            // Settings is intentionally reserved for the later audio/haptics milestone.
            hud.settingsButton.onClick.RemoveAllListeners();
            hud.settingsButton.interactable = false;
        }

        ApplyBankVisibility();
        PrepareBankDrawerPositions();
    }

    private void SetPaused(bool paused)
    {
        pauseMenuOpen = paused;

        if (hud != null)
        {
            if (hud.pauseMenu != null)
                hud.pauseMenu.SetActive(paused);

            if (hud.pauseButton != null)
                hud.pauseButton.gameObject.SetActive(!paused);
        }

        // PieceView can call InputController directly even when the MonoBehaviour
        // component is disabled, so pause must be an explicit gameplay-input state.
        if (input != null)
            input.SetGameplayPaused(paused);
    }

    private void ApplyBankVisibility()
    {
        if (hud == null) return;

        if (hud.bankButton != null)
            hud.bankButton.gameObject.SetActive(bankEnabled);

        if (hud.saveSelectedButton != null)
            hud.saveSelectedButton.gameObject.SetActive(bankEnabled);

        if (hud.bankDrawer != null)
            hud.bankDrawer.gameObject.SetActive(bankEnabled);
    }

    private void WireBankSlot(Button button, SpecialType specialType)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => input?.SelectBankedSpecialType(specialType));
    }

    private void Refresh()
    {
        if (hud.levelText != null)
            hud.levelText.text = levelTitle;

        if (hud.scoreText != null)
            hud.scoreText.text = $"SCORE\n{(scoring != null ? scoring.TotalScore : 0):N0}";

        LevelAttemptModel attempt = progress != null ? progress.Model : null;
        if (hud.movesText != null)
        {
            hud.movesText.text = attempt != null && attempt.UseMoveLimit
                ? $"MOVES\n{attempt.MovesRemaining}"
                : "MOVES\n∞";
        }

        RefreshObjectiveCards();
        RefreshHotStreak();
        RefreshActions();
    }

    private void BuildObjectiveList()
    {
        if (hud.objectiveCardTemplate == null || hud.objectiveTemplateText == null) return;

        // Remove clones from a previous Show/restart, but keep the authored template.
        for (int i = hud.objectivesRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = hud.objectivesRoot.GetChild(i);
            if (child.gameObject != hud.objectiveCardTemplate &&
                child.name.StartsWith("ObjectiveCard_Runtime"))
            {
                Destroy(child.gameObject);
            }
        }

        objectiveCardTexts.Clear();
        objectiveCardTexts.Add(hud.objectiveTemplateText);

        ObjectiveModel model = objectives != null ? objectives.Model : null;
        int desiredCount = model != null && model.Objectives.Count > 0
            ? model.Objectives.Count
            : 1;

        hud.objectiveCardTemplate.SetActive(true);

        for (int i = 1; i < desiredCount; i++)
        {
            GameObject clone = Instantiate(
                hud.objectiveCardTemplate,
                hud.objectivesRoot,
                false);

            clone.name = $"ObjectiveCard_Runtime_{i + 1}";

            TMP_Text text = clone.GetComponentInChildren<TMP_Text>(true);
            if (text != null) objectiveCardTexts.Add(text);
        }
    }

    private void RefreshObjectiveCards()
    {
        if (objectiveCardTexts.Count == 0) BuildObjectiveList();
        if (objectiveCardTexts.Count == 0) return;

        ObjectiveModel model = objectives != null ? objectives.Model : null;

        if (model == null || model.Objectives.Count == 0)
        {
            objectiveCardTexts[0].text = "FREE PLAY";
            return;
        }

        // If a level with a different objective count was loaded, rebuild safely.
        if (objectiveCardTexts.Count != model.Objectives.Count)
        {
            BuildObjectiveList();
            if (objectiveCardTexts.Count == 0) return;
        }

        for (int i = 0; i < model.Objectives.Count && i < objectiveCardTexts.Count; i++)
        {
            ObjectiveProgress objective = model.Objectives[i];
            string marker = objective.IsComplete ? "✓ " : "";
            string machineText = objective.RemainingGenerators > 0
                ? $"\n+{objective.RemainingGenerators} MACHINE{(objective.RemainingGenerators == 1 ? "" : "S")}"
                : "";

            objectiveCardTexts[i].text =
                $"{marker}{objective.Label}\n{objective.Current:N0} / {objective.Target:N0}{machineText}";
        }
    }

    private void RefreshHotStreak()
    {
        int cascades = scoring != null ? scoring.CurrentCascadeCount : 0;
        MoveSource source = scoring != null
            ? scoring.CurrentCascadeSource
            : MoveSource.Player;

        int trigger = input != null ? input.CascadeBonusTrigger : 10;
        bool enabled = input != null && input.CascadeBonusEnabled;
        bool bonusActive = input != null && input.IsCascadeBonusActive;
        int completedBonusMoves = input != null ? input.CascadeBonusCompletedMoves : 0;

        if (lastCascadeCount >= 0 &&
            cascades > lastCascadeCount &&
            source == MoveSource.Player &&
            !bonusActive)
        {
            pulseTimer = 0.32f;

        }

        if (bonusActive && !lastBonusActive)
        {
            pulseTimer = 0.75f;
            ShowAnnouncement("HOT STREAK!", 1.35f);
        }
        else if (bonusActive &&
                 completedBonusMoves != lastBonusCompletedMoves &&
                 completedBonusMoves > 0)
        {
            pulseTimer = 0.28f;
        }

        lastCascadeCount = cascades;
        lastBonusActive = bonusActive;
        lastBonusCompletedMoves = completedBonusMoves;

        float progressValue = enabled
            ? Mathf.Clamp01(cascades / (float)Mathf.Max(1, trigger))
            : 0f;

        targetFill = bonusActive ? 1f : progressValue;

        if (hud.hotStreakText != null)
        {
            if (!enabled)
            {
                hud.hotStreakText.text = "HOT\nSTREAK\nOFF";
            }
            else if (bonusActive)
            {
                int total = Mathf.Max(1, input.CascadeBonusTotalMoves);
                int shownMove = Mathf.Min(input.CascadeBonusCompletedMoves + 1, total);
                hud.hotStreakText.text = $"HOT\nSTREAK!\n{shownMove}/{total}";
            }
            else
            {
                hud.hotStreakText.text =
                    $"HOT\nSTREAK\n{Mathf.Min(cascades, trigger)} / {trigger}";
            }
        }

    }

    private void RefreshActions()
    {
        if (input == null) return;

        bool ready = input.PlayerActionsReady;

        if (hud.hintButton != null)
            hud.hintButton.interactable = ready;

        if (hud.bankButton != null)
            hud.bankButton.interactable = bankEnabled && ready && input.BankAvailable;

        // Retry belongs to the pause menu and must remain usable while gameplay input is paused.
        if (hud.retryButton != null)
            hud.retryButton.interactable = true;

        if (hud.quitButton != null)
            hud.quitButton.interactable = true;

        if (!bankEnabled) return;

        RefreshBankSlot(hud.hLineSlot, SpecialType.LineHorizontal, ready);
        RefreshBankSlot(hud.vLineSlot, SpecialType.LineVertical, ready);
        RefreshBankSlot(hud.bombSlot, SpecialType.Bomb, ready);
        RefreshBankSlot(hud.targetSlot, SpecialType.Target, ready);
        RefreshBankSlot(hud.colorClearSlot, SpecialType.ColorClear, ready);

        if (hud.saveSelectedButton != null)
            hud.saveSelectedButton.interactable = input.CanSaveSelectedSpecial;

        if (hud.bankStatusText != null)
        {
            if (!string.IsNullOrEmpty(input.BankMessage))
                hud.bankStatusText.text = input.BankMessage;
            else if (input.CanSaveSelectedSpecial)
                hud.bankStatusText.text = "Selected special can be saved.";
            else if (input.BankPlacementPending)
                hud.bankStatusText.text = "Click a normal tile to place your special.";
            else
                hud.bankStatusText.text = "Select a saved piece to use it.";
        }
    }

    private void RefreshBankSlot(
        Button button,
        SpecialType specialType,
        bool ready)
    {
        if (button == null || input == null) return;

        int usable = input.GetBankUsableCount(specialType);
        int owned = input.GetBankOwnedCount(specialType);

        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
            text.text = $"{usable} / {owned}";

        button.interactable = ready && usable > 0;
    }

    private void PrepareBankDrawerPositions()
    {
        if (hud == null || hud.bankDrawer == null || bankDrawerPositionsReady) return;

        bankDrawerOpenPosition = hud.bankDrawer.anchoredPosition;

        float drawerWidth = hud.bankDrawer.rect.width;
        if (drawerWidth <= 1f) drawerWidth = hud.bankDrawer.sizeDelta.x;
        if (drawerWidth <= 1f) drawerWidth = 700f;

        // The position you authored in Unity is the OPEN position.
        // Closed is one drawer-width to the left, so it slides out to the right.
        bankDrawerClosedPosition =
            bankDrawerOpenPosition + Vector2.left * (drawerWidth + 24f);

        hud.bankDrawer.anchoredPosition = bankDrawerClosedPosition;

        // A RectTransform does not clip its children. Without this CanvasGroup,
        // drawer children can remain visible/clickable after the drawer slides left.
        bankDrawerCanvasGroup = hud.bankDrawer.GetComponent<CanvasGroup>();
        if (bankDrawerCanvasGroup == null)
            bankDrawerCanvasGroup = hud.bankDrawer.gameObject.AddComponent<CanvasGroup>();

        bankDrawerCanvasGroup.alpha = 0f;
        bankDrawerCanvasGroup.interactable = false;
        bankDrawerCanvasGroup.blocksRaycasts = false;

        bankDrawerPositionsReady = true;
    }

    private void AnimateBankDrawer()
    {
        if (hud == null || hud.bankDrawer == null || input == null || !bankEnabled) return;

        PrepareBankDrawerPositions();

        bool open = input.BankOpen;
        Vector2 target = open
            ? bankDrawerOpenPosition
            : bankDrawerClosedPosition;

        // Make the drawer visible as soon as it starts opening.
        // While closed/closing it never blocks the separate BANK button.
        if (bankDrawerCanvasGroup != null)
        {
            bankDrawerCanvasGroup.alpha = open ? 1f : 0f;
            bankDrawerCanvasGroup.interactable = open;
            bankDrawerCanvasGroup.blocksRaycasts = open;
        }

        hud.bankDrawer.anchoredPosition = Vector2.MoveTowards(
            hud.bankDrawer.anchoredPosition,
            target,
            BankDrawerSlideSpeed * Time.unscaledDeltaTime);
    }

    private void AnimateHotStreak()
    {
        float dt = Time.unscaledDeltaTime;

        if (hud.hotStreakFill != null)
        {
            displayedFill = Mathf.MoveTowards(
                displayedFill,
                targetFill,
                dt * 1.8f);

            hud.hotStreakFill.fillAmount = displayedFill;
        }

        if (hud.hotStreakRoot != null)
        {
            if (pulseTimer > 0f)
            {
                pulseTimer = Mathf.Max(0f, pulseTimer - dt);
                float strength =
                    Mathf.Sin((1f - pulseTimer / 0.75f) * Mathf.PI);

                hud.hotStreakRoot.localScale =
                    hotStreakBaseScale *
                    (1f + 0.08f * Mathf.Max(0f, strength));
            }
            else
            {
                hud.hotStreakRoot.localScale = hotStreakBaseScale;
            }
        }

        if (announcementTimer > 0f && streakAnnouncement != null)
        {
            announcementTimer = Mathf.Max(0f, announcementTimer - dt);
            float life = Mathf.Clamp01(announcementTimer / 1.35f);

            Color c = streakAnnouncement.color;
            c.a = Mathf.Clamp01(life * 1.6f);
            streakAnnouncement.color = c;

            streakAnnouncement.rectTransform.localScale =
                Vector3.one * (1f + (1f - life) * 0.18f);

            if (announcementTimer <= 0f)
                streakAnnouncement.gameObject.SetActive(false);
        }
    }

    private void ShowAnnouncement(string message, float duration)
    {
        EnsureTransientUi();
        if (streakAnnouncement == null) return;

        streakAnnouncement.text = message;
        streakAnnouncement.gameObject.SetActive(true);
        announcementTimer = Mathf.Max(0.1f, duration);
    }

    private void EnsureTransientUi()
    {
        if (hud == null) return;

        if (streakAnnouncement == null)
        {
            GameObject go = new GameObject(
                "HotStreakAnnouncement_Runtime",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));

            go.transform.SetParent(hud.transform, false);

            streakAnnouncement = go.GetComponent<TextMeshProUGUI>();
            streakAnnouncement.font = hud.hotStreakText.font;
            streakAnnouncement.fontSize = 64f;
            streakAnnouncement.fontStyle = FontStyles.Bold;
            streakAnnouncement.alignment = TextAlignmentOptions.Center;
            streakAnnouncement.color = Color.white;
            streakAnnouncement.raycastTarget = false;

            RectTransform rect = streakAnnouncement.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 180f);
            rect.sizeDelta = new Vector2(900f, 150f);

            go.SetActive(false);
        }

    }
}
