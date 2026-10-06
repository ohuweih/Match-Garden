using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>References to the scene-authored ResultCanvas. Layout remains controlled in Unity.</summary>
public sealed class EditableResultScreen : MonoBehaviour
{
    [Header("Root")]
    public GameObject resultRoot;
    public CanvasGroup canvasGroup;

    [Header("Text")]
    public TMP_Text resultTitleText;
    public TMP_Text scoreText;
    public TMP_Text resultMessageText;

    [Header("Actions")]
    public Button retryButton;
    public Button continueButton;
    public Button quitButton;

    [Header("Outcome Artwork")]
    public GameObject victoryDecoration;
    public GameObject retryDecoration;

    [Header("Retry Button Appearance")]
    public Sprite primaryActionSprite;
    public Sprite secondaryActionSprite;
    public Color primaryActionTextColor = Color.white;
    public Color secondaryActionTextColor = new Color(0.145f, 0.298f, 0.239f);

    public bool IsConfigured =>
        resultRoot != null &&
        resultTitleText != null &&
        scoreText != null &&
        resultMessageText != null &&
        retryButton != null &&
        continueButton != null &&
        quitButton != null;
}
