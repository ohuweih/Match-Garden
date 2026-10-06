using System.Collections.Generic;

public class ResolutionSequence
{
    public System.Guid Id { get; } = System.Guid.NewGuid();
    public MoveSource Source { get; }
    public bool ConsumesNormalMove => Source == MoveSource.Player;
    public bool CanTriggerCascadeBonus => Source == MoveSource.Player;
    public int CascadeCount => Rounds.FindAll(round => round.IsMatchRound).Count;
    public List<ResolutionRound> Rounds { get; }

    public ResolutionSequence(MoveSource source = MoveSource.Player)
    {
        Source = source;
        Rounds =
            new List<ResolutionRound>();
    }
}