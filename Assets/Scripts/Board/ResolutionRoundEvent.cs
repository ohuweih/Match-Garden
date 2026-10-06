using System;
using System.Collections.Generic;

// Immutable gameplay facts, published as the round's clear animation finishes.
public sealed class ResolutionRoundEvent
{
    public IReadOnlyList<GeneratorChange> GeneratorChanges { get; }
    public IReadOnlyList<PackageChange> PackageChanges { get; }
    public Guid ResolutionId { get; }
    public IReadOnlyList<FrostTileChange> FrostChanges { get; }
    public IReadOnlyList<BirdRescueRecord> RescuedBirds { get; }
    public MoveSource Source { get; }
    public int RoundIndex { get; }
    public int CascadeNumber { get; }
    public bool IsMatchRound { get; }
    public IReadOnlyList<ClearedPieceRecord> ClearedPieces { get; }
    public IReadOnlyList<LockedTileChange> LockedTileChanges { get; }

    public ResolutionRoundEvent(ResolutionSequence sequence, int roundIndex, int cascadeNumber)
    {
        var round = sequence.Rounds[roundIndex];
        PackageChanges = new List<PackageChange>(round.PackageChanges).AsReadOnly();
        GeneratorChanges = new List<GeneratorChange>(round.GeneratorChanges).AsReadOnly();
        FrostChanges = new List<FrostTileChange>(round.FrostChanges).AsReadOnly();
        ResolutionId = sequence.Id;
        Source = sequence.Source;
        RoundIndex = roundIndex;
        CascadeNumber = cascadeNumber;
        IsMatchRound = round.IsMatchRound;
        RescuedBirds = new List<BirdRescueRecord>(round.RescuedBirds).AsReadOnly();
        ClearedPieces = new List<ClearedPieceRecord>(round.ClearedPieces).AsReadOnly();
        LockedTileChanges = new List<LockedTileChange>(round.LockedTileChanges).AsReadOnly();
    }
}
