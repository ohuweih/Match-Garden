using UnityEngine;

public class ScoreController : MonoBehaviour
{
    // Published after scoring, including zero-point rounds, for objective consumers.
    public event System.Action<ResolutionRoundEvent, long> RoundScored;
    private BoardView boardView;
    private ScoreModel score;
    private bool subscribed;
    private int cascadeCount;
    private LevelAttemptModel attempt;
    private MoveSource cascadeSource = MoveSource.Player;
    public long TotalScore => score == null ? 0 : score.Total;
    public int CurrentCascadeCount => cascadeCount;
    public MoveSource CurrentCascadeSource => cascadeSource;

    public void Setup(BoardView view, int pointsPerPiece, bool awardOpeningPoints, ColorRarityTable colorRarity = null, LevelAttemptModel levelAttempt = null)
    {
        attempt = levelAttempt;
        Unsubscribe();
        boardView = view;
        cascadeCount = 0;
        cascadeSource = MoveSource.Player;
        score = new ScoreModel(pointsPerPiece, awardOpeningPoints, colorRarity);
        if (isActiveAndEnabled) Subscribe();
    }

    private void OnEnable() { Subscribe(); }
    private void OnDisable() { Unsubscribe(); }

    private void Subscribe()
    {
        if (subscribed || boardView == null || score == null) return;
        boardView.ResolutionStarted += OnResolutionStarted;
        boardView.RoundCleared += OnRoundCleared;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (subscribed && boardView != null)
        {
            boardView.ResolutionStarted -= OnResolutionStarted;
            boardView.RoundCleared -= OnRoundCleared;
        }
        subscribed = false;
    }

    private void OnResolutionStarted(ResolutionSequence sequence)
    {
        if (sequence.Source == MoveSource.Generator) return;
        cascadeCount = 0;
        cascadeSource = sequence.Source;
    }

    private void OnRoundCleared(ResolutionRoundEvent result)
    {
        if (result.Source != MoveSource.Generator || result.IsMatchRound)
        {
            cascadeCount = result.CascadeNumber;
            cascadeSource = result.Source;
        }
        long before = score.Total;
        score.Apply(result);
        RoundScored?.Invoke(result, score.Total - before);
        if (score.Total != before)
            Debug.Log($"[Score] {result.Source}: +{score.Total - before}, total {score.Total} (round {result.RoundIndex + 1}).", this);
    }

    // Player-facing score/cascade HUD is rendered by GameplayHudPresenter.
}
