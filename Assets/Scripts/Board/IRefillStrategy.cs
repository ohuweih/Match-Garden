using System.Collections.Generic;

public interface IRefillStrategy
{
    void BeginResolution();

    void Apply(
        BoardModel board,
        ResolutionRound round,
        List<MatchResult> matches
    );
}