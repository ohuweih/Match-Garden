using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Drives the editable Win/Loss result screen without changing its authored layout.</summary>
public sealed class ResultScreenPresenter : MonoBehaviour
{
    private EditableResultScreen screen;
    private LevelProgressController progress;
    private ScoreController scoring;
    private GameplayHudPresenter gameplayHud;
    private bool campaignMode;
    private Action quitAction;

    public void Setup(LevelProgressController progressController, ScoreController scoreController,
        GameplayHudPresenter hudPresenter, bool isCampaign, Action onQuit)
    {
        Unsubscribe();
        progress = progressController;
        scoring = scoreController;
        gameplayHud = hudPresenter;
        campaignMode = isCampaign;
        quitAction = onQuit;
        Bind();
        Hide();
        if (progress != null) progress.Finished += OnFinished;
    }

    private void OnDestroy() { Unsubscribe(); }
    private void Unsubscribe()
    {
        if (progress != null) progress.Finished -= OnFinished;
    }

    private bool Bind()
    {
        if (screen != null) return screen.IsConfigured;
        screen = FindFirstObjectByType<EditableResultScreen>(FindObjectsInactive.Include);
        return screen != null && screen.IsConfigured;
    }

    private void OnFinished()
    {
        if (!Bind())
        {
            Debug.LogError("[Result] EditableResultScreen is missing or not fully assigned.", this);
            return;
        }

        gameplayHud?.HideImmediate();
        bool won = progress.Model.Status == LevelAttemptStatus.Won;

        if (screen.victoryDecoration != null) screen.victoryDecoration.SetActive(won);
        if (screen.retryDecoration != null) screen.retryDecoration.SetActive(!won);

        // Retry becomes the primary action when there is no campaign continuation.
        bool retryIsPrimary = !won || !campaignMode;
        Sprite retrySprite = retryIsPrimary ? screen.primaryActionSprite : screen.secondaryActionSprite;
        if (retrySprite != null && screen.retryButton.image != null)
        {
            screen.retryButton.image.sprite = retrySprite;
            TMP_Text retryLabel = screen.retryButton.GetComponentInChildren<TMP_Text>(true);
            if (retryLabel != null)
                retryLabel.color = retryIsPrimary ? screen.primaryActionTextColor : screen.secondaryActionTextColor;
        }

        screen.resultTitleText.text = won
            ? (campaignMode ? progress.WinTitle : "FREE PLAY COMPLETE!")
            : "OUT OF MOVES";
        screen.scoreText.text = $"SCORE\n{(scoring != null ? scoring.TotalScore : 0):N0}";
        screen.resultMessageText.text = won
            ? (campaignMode ? progress.WinMessage : "All objectives completed.")
            : "Try again to complete the objectives.";

        Wire(screen.retryButton, () => progress?.InvokeRetry());
        Wire(screen.quitButton, () => quitAction?.Invoke());

        if (screen.continueButton != null)
        {
            screen.continueButton.gameObject.SetActive(won && campaignMode);
            if (won && campaignMode)
            {
                SetButtonText(screen.continueButton, progress.WinButton);
                Wire(screen.continueButton, () => progress?.InvokeWinAction());
            }
        }

        Show();
    }

    private static void Wire(Button button, Action action)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => action?.Invoke());
    }

    private static void SetButtonText(Button button, string label)
    {
        TMP_Text text = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
        if (text != null) text.text = string.IsNullOrWhiteSpace(label) ? "CONTINUE" : label.ToUpperInvariant();
    }

    public void Show()
    {
        if (!Bind()) return;
        screen.resultRoot.SetActive(true);
        if (screen.canvasGroup != null)
        {
            screen.canvasGroup.alpha = 1f;
            screen.canvasGroup.interactable = true;
            screen.canvasGroup.blocksRaycasts = true;
        }
    }

    public void Hide()
    {
        if (!Bind()) return;
        if (screen.canvasGroup != null)
        {
            screen.canvasGroup.alpha = 0f;
            screen.canvasGroup.interactable = false;
            screen.canvasGroup.blocksRaycasts = false;
        }
        screen.resultRoot.SetActive(false);
    }
}
