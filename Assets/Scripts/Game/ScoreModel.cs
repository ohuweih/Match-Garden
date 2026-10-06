using System;
using System.Collections.Generic;

public sealed class ScoreModel
{
    private readonly ColorRarityTable colorRarity;
    public long Total { get; private set; }
    public int PointsPerPiece { get; }
    public bool AwardOpeningPoints { get; }
    public event Action<long, long> ScoreChanged;
    private readonly HashSet<(Guid, int)> processedRounds = new HashSet<(Guid, int)>();

    public ScoreModel(int pointsPerPiece = 10, bool awardOpeningPoints = true, ColorRarityTable colorRarity = null)
    {
        if (pointsPerPiece < 0) throw new ArgumentOutOfRangeException(nameof(pointsPerPiece));
        this.colorRarity = colorRarity;
        PointsPerPiece = pointsPerPiece;
        AwardOpeningPoints = awardOpeningPoints;
    }

    public void Apply(ResolutionRoundEvent result)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));
        if (!processedRounds.Add((result.ResolutionId, result.RoundIndex))) return;
        if (result.Source == MoveSource.OpeningCascade && !AwardOpeningPoints) return;
        long earned = 0;
        foreach (var piece in result.ClearedPieces)
            earned += (long)PointsPerPiece * (colorRarity == null ? 1 : colorRarity.ScoreMultiplier(piece.Color));
        if (earned == 0) return;
        Total += earned;
        ScoreChanged?.Invoke(Total, earned);
    }
}
