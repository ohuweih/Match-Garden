using UnityEngine;

public class ObjectiveController : MonoBehaviour
{
    private ScoreController scoring;
    private ObjectiveModel model;
    private bool subscribed;
    public ObjectiveModel Model => model;

    public void Setup(ScoreController scoreController, ObjectiveDefinition[] definitions, bool countOpeningCascades, int frostMachines = 0)
    {
        Unsubscribe();
        if (model != null)
        {
            model.ObjectiveCompleted -= OnObjectiveCompleted;
            model.AllCompleted -= OnAllCompleted;
        }
        scoring = scoreController;
        model = new ObjectiveModel(definitions, countOpeningCascades, frostMachines);
        model.ObjectiveCompleted += OnObjectiveCompleted;
        model.AllCompleted += OnAllCompleted;
        if (isActiveAndEnabled) Subscribe();
    }

    private void OnEnable() { Subscribe(); }
    private void OnDisable() { Unsubscribe(); }

    private void Subscribe()
    {
        if (subscribed || scoring == null || model == null) return;
        scoring.RoundScored += OnRoundScored;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (subscribed && scoring != null) scoring.RoundScored -= OnRoundScored;
        subscribed = false;
    }

    private void OnRoundScored(ResolutionRoundEvent result, long awarded)
    {
        model.Apply(result, awarded);
    }

    private void OnObjectiveCompleted(ObjectiveProgress objective)
    {
        Debug.Log($"[Objectives] Complete: {objective.Label} ({objective.Target}).", this);
    }

    private void OnAllCompleted()
    {
        Debug.Log("[Objectives] All objectives complete!", this);
    }

    // Player-facing objective HUD is rendered by GameplayHudPresenter.
}
