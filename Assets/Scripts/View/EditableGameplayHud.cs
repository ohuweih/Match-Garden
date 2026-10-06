using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// References to the HUD authored directly in the Unity scene.
/// This component never positions your UI.
/// </summary>
public sealed class EditableGameplayHud : MonoBehaviour
{
    [Header("Root")]
    [Tooltip("Assign GameplayHUDCanvas/HUDRoot. It is switched on only while gameplay is active.")]
    public GameObject hudRoot;
    [Header("Top HUD")]
    public TMP_Text levelText;
    public TMP_Text scoreText;
    public TMP_Text movesText;

    [Header("Objectives")]
    public RectTransform objectivesRoot;
    public GameObject objectiveCardTemplate;
    public TMP_Text objectiveTemplateText;

    [Header("Hot Streak")]
    public RectTransform hotStreakRoot;
    public TMP_Text hotStreakText;
    public Image hotStreakFill;
    public TMP_Text hotStreakSourceText;

    [Header("Actions")]
    public Button bankButton;
    public Button hintButton;
    public Button pauseButton;

    [Header("Pause Menu")]
    [Tooltip("Assign HUDRoot/PauseMenu. Keep it inactive by default in the scene.")]
    public GameObject pauseMenu;
    public Button resumeButton;
    public Button retryButton;
    public Button quitButton;
    public Button settingsButton;

    [Header("Bank Drawer")]
    public RectTransform bankDrawer;
    public Button hLineSlot;
    public Button vLineSlot;
    public Button bombSlot;
    public Button targetSlot;
    public Button colorClearSlot;
    public TMP_Text bankStatusText;
    public Button saveSelectedButton;

    public bool IsConfigured =>
        hudRoot != null &&
        levelText != null &&
        scoreText != null &&
        movesText != null &&
        objectivesRoot != null &&
        objectiveCardTemplate != null &&
        objectiveTemplateText != null &&
        hotStreakRoot != null &&
        hotStreakText != null &&
        hotStreakFill != null &&
        bankButton != null &&
        hintButton != null &&
        pauseButton != null &&
        pauseMenu != null &&
        resumeButton != null &&
        retryButton != null &&
        quitButton != null &&
        bankDrawer != null &&
        hLineSlot != null &&
        vLineSlot != null &&
        bombSlot != null &&
        targetSlot != null &&
        colorClearSlot != null &&
        bankStatusText != null &&
        saveSelectedButton != null;

    private void Awake()
    {
        if (hotStreakFill != null)
        {
            hotStreakFill.type = Image.Type.Filled;
            hotStreakFill.fillMethod = Image.FillMethod.Vertical;
            hotStreakFill.fillOrigin = (int)Image.OriginVertical.Bottom;
        }
    }
}
