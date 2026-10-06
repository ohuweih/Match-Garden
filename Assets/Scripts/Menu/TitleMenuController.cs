using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class TitleMenuController : MonoBehaviour
{
    private enum StartDestination { None, Campaign, FreePlay }

    [Header("Game")]
    [SerializeField] private GameManager gameManager;

    [Header("Screens")]
    [SerializeField] private GameObject titleRoot;
    [SerializeField] private GameObject gameModeRoot;
    [SerializeField] private GameObject playStyleRoot;
    [SerializeField] private GameObject aboutRoot;
    [SerializeField] private GameObject campaignRoot;
    [SerializeField] private GameObject levelSelectRoot;

    [Header("Title Buttons")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button aboutButton;

    [Header("Game Mode Buttons")]
    [SerializeField] private Button campaignButton;
    [SerializeField] private Button freePlayButton;
    [SerializeField] private Button challengesButton;
    [SerializeField] private Button dailyPuzzleButton;
    [SerializeField] private Button gameModeBackButton;

    [Header("Play Style Buttons")]
    [SerializeField] private Button classicButton;
    [SerializeField] private Button normalButton;
    [SerializeField] private Button chaosButton;
    [SerializeField] private Button playStyleBackButton;

    [Header("Campaign Screen")]
    [SerializeField] private TMP_Text campaignHeaderText;
    [SerializeField] private TMP_Text campaignProgressText;
    [SerializeField] private Button campaignContinueButton;
    [SerializeField] private Button campaignLevelSelectButton;
    [SerializeField] private Button campaignBackButton;

    [Header("Level Select")]
    [SerializeField] private TMP_Text levelSelectHeaderText;
    [SerializeField] private TMP_Text levelSelectProgressText;
    [SerializeField] private Transform levelButtonRoot;
    [SerializeField] private GameObject levelButtonTemplate;
    [SerializeField] private Button levelSelectBackButton;

    [Header("About")]
    [SerializeField] private Button aboutBackButton;

    private StartDestination pendingDestination;
    private RefillMode selectedCampaignMode = RefillMode.Normal;

    private void Awake()
    {
        WireButtons();
        if (levelButtonTemplate != null) levelButtonTemplate.SetActive(false);
        ShowTitle();
    }

    private void WireButtons()
    {
        startGameButton.onClick.AddListener(ShowGameModes);
        aboutButton.onClick.AddListener(ShowAbout);
        campaignButton.onClick.AddListener(() => ShowPlayStyles(StartDestination.Campaign));
        freePlayButton.onClick.AddListener(() => ShowPlayStyles(StartDestination.FreePlay));
        gameModeBackButton.onClick.AddListener(ShowTitle);
        playStyleBackButton.onClick.AddListener(ShowGameModes);
        aboutBackButton.onClick.AddListener(ShowTitle);
        classicButton.onClick.AddListener(() => SelectPlayStyle(RefillMode.Classic));
        normalButton.onClick.AddListener(() => SelectPlayStyle(RefillMode.Normal));
        chaosButton.onClick.AddListener(() => SelectPlayStyle(RefillMode.Chaos));

        if (campaignContinueButton != null) campaignContinueButton.onClick.AddListener(ContinueCampaign);
        if (campaignLevelSelectButton != null) campaignLevelSelectButton.onClick.AddListener(ShowLevelSelect);
        if (campaignBackButton != null) campaignBackButton.onClick.AddListener(() => ShowPlayStyles(StartDestination.Campaign));
        if (levelSelectBackButton != null) levelSelectBackButton.onClick.AddListener(ShowCampaignScreen);

        if (challengesButton != null) challengesButton.interactable = false;
        if (dailyPuzzleButton != null) dailyPuzzleButton.interactable = false;
    }

    public void ReturnToTitle() => ShowTitle();

    private void ShowTitle() { pendingDestination = StartDestination.None; ShowOnly(titleRoot); }
    private void ShowGameModes() { pendingDestination = StartDestination.None; ShowOnly(gameModeRoot); }
    private void ShowAbout() => ShowOnly(aboutRoot);
    private void ShowPlayStyles(StartDestination destination) { pendingDestination = destination; ShowOnly(playStyleRoot); }

    private void SelectPlayStyle(RefillMode refillMode)
    {
        Debug.Log($"[Title Menu] Selected {pendingDestination} - {refillMode}");
        if (gameManager == null) { Debug.LogError("[Title Menu] GameManager is not assigned."); return; }

        if (pendingDestination == StartDestination.Campaign)
        {
            selectedCampaignMode = refillMode;
            if (gameManager.PrepareCampaign(refillMode)) ShowCampaignScreen();
            return;
        }

        if (pendingDestination == StartDestination.FreePlay)
        {
            HideAllScreens();
            gameManager.StartFreePlay(refillMode);
        }
    }

    private void ShowCampaignScreen()
    {
        RefreshCampaignSummary();
        ShowOnly(campaignRoot);
    }

    private void RefreshCampaignSummary()
    {
        int completed = gameManager != null ? gameManager.CampaignCompletedCount : 0;
        int total = gameManager != null ? gameManager.CampaignLevelCount : 0;
        if (campaignHeaderText != null) campaignHeaderText.text = $"{selectedCampaignMode.ToString().ToUpperInvariant()} CAMPAIGN";
        if (campaignProgressText != null) campaignProgressText.text = $"{completed} / {total} LEVELS COMPLETE";
        if (campaignContinueButton != null) campaignContinueButton.interactable = gameManager != null && !gameManager.CampaignAllComplete;
    }

    private void ContinueCampaign()
    {
        if (gameManager == null || gameManager.CampaignAllComplete) return;
        HideAllScreens();
        gameManager.ContinueCampaign();
    }

    private void ShowLevelSelect()
    {
        BuildLevelButtons();
        ShowOnly(levelSelectRoot);
    }

    private void BuildLevelButtons()
    {
        if (gameManager == null || levelButtonRoot == null || levelButtonTemplate == null) return;

        for (int i = levelButtonRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = levelButtonRoot.GetChild(i);
            if (child.gameObject != levelButtonTemplate) Destroy(child.gameObject);
        }

        int completed = gameManager.CampaignCompletedCount;
        int total = gameManager.CampaignLevelCount;
        if (levelSelectHeaderText != null) levelSelectHeaderText.text = $"{selectedCampaignMode.ToString().ToUpperInvariant()} LEVELS";
        if (levelSelectProgressText != null) levelSelectProgressText.text = $"{completed} / {total} COMPLETE";

        for (int i = 0; i < total; i++)
        {
            int levelIndex = i;
            GameObject card = Instantiate(levelButtonTemplate, levelButtonRoot);
            card.name = $"LevelButton_{i + 1}";
            card.SetActive(true);

            Button button = card.GetComponentInChildren<Button>(true);
            TMP_Text[] texts = card.GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length > 0) texts[0].text = $"LEVEL {i + 1}";

            bool completedLevel = gameManager.IsCampaignLevelCompleted(i);
            bool unlocked = gameManager.IsCampaignLevelUnlocked(i);
            string status = completedLevel ? "COMPLETED - REPLAY" : (unlocked ? "CURRENT" : "LOCKED");
            if (texts.Length > 1) texts[1].text = status;

            if (button != null)
            {
                button.interactable = unlocked;
                button.onClick.RemoveAllListeners();
                if (unlocked) button.onClick.AddListener(() => StartSelectedLevel(levelIndex));
            }
        }
    }

    private void StartSelectedLevel(int index)
    {
        HideAllScreens();
        gameManager.StartCampaignLevel(index);
    }

    private void ShowOnly(GameObject screen)
    {
        SetScreen(titleRoot, screen);
        SetScreen(gameModeRoot, screen);
        SetScreen(playStyleRoot, screen);
        SetScreen(aboutRoot, screen);
        SetScreen(campaignRoot, screen);
        SetScreen(levelSelectRoot, screen);
    }

    private static void SetScreen(GameObject candidate, GameObject screen)
    {
        if (candidate != null) candidate.SetActive(candidate == screen);
    }

    private void HideAllScreens() => ShowOnly(null);
}
