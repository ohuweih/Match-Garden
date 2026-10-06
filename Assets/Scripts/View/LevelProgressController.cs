using System;
using UnityEngine;

public sealed class LevelProgressController : MonoBehaviour
{
    public event Action Finished;

    public string WinTitle => winTitle;
    public string WinMessage => winMessage;
    public string WinButton => winButton;
    public LevelAttemptModel Model { get; private set; }
    private BoardView view;
    private InputController input;
    private ObjectiveController objectives;
    private Action retry;
    private Action onWin;
    private Action winAction;
    private bool completionHandled;
    private string winTitle, winMessage, winButton;
    private bool subscribed;

    public void Setup(BoardView boardView, InputController inputController, ObjectiveController tracker,
        LevelAttemptModel model, Action restart, Action won = null)
    {
        Unsubscribe();
        view = boardView;
        input = inputController;
        objectives = tracker;
        Model = model;
        retry = restart;
        onWin = won;
        completionHandled = false;
        ConfigureWinResult("Level complete!", "All objectives completed.", "Retry", restart);
        if (isActiveAndEnabled) Subscribe();
    }

    public void ConfigureWinResult(string title, string message, string button, Action action)
    {
        winTitle = title; winMessage = message; winButton = button; winAction = action;
    }

    private void OnEnable() { Subscribe(); }
    private void OnDisable() { Unsubscribe(); }
    private void Subscribe()
    {
        if (subscribed || view == null || input == null || Model == null) return;
        view.ResolutionStarted += OnResolutionStarted;
        input.ResolutionChainCompleted += OnChainCompleted;
        subscribed = true;
    }
    private void Unsubscribe()
    {
        if (!subscribed) return;
        if (view != null) view.ResolutionStarted -= OnResolutionStarted;
        if (input != null) input.ResolutionChainCompleted -= OnChainCompleted;
        subscribed = false;
    }
    private void OnResolutionStarted(ResolutionSequence sequence) { Model.RecordResolution(sequence); }
    private void OnChainCompleted(MoveSource source)
    {
        if (source == MoveSource.CascadeBonus) return;
        Model.CompleteChain(objectives.Model != null && objectives.Model.AllComplete);
        if (!Model.IsFinished || completionHandled) return;
        completionHandled = true;
        input.EndLevel();
        Debug.Log(Model.Status == LevelAttemptStatus.Won
            ? $"[Level] Complete! {Model.MovesRemaining} move(s) remaining."
            : "[Level] Out of moves. Try again!", this);
        if (Model.Status == LevelAttemptStatus.Won) onWin?.Invoke();
        NotifyFinished();
    }
    public void InvokeWinAction() { winAction?.Invoke(); }
    public void InvokeRetry() { retry?.Invoke(); }

    private void NotifyFinished()
    {
        Finished?.Invoke();
    }
}
