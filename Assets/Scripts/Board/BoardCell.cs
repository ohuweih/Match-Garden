public class BoardCell
{
    public int X { get; private set; }
    public int Y { get; private set; }

    public bool IsPlayable { get; private set; }

    public int MatchesUntilUnlock { get; private set; }
    public bool IsLocked => !IsPlayable && MatchesUntilUnlock > 0;

    public BoardGenerator Generator { get; private set; }

    public void PlaceGenerator(BoardGenerator generator, bool allowExistingFrost = false)
    {
        if (generator == null) throw new System.ArgumentNullException(nameof(generator));
        if (!IsPlayable || (!allowExistingFrost && FrostLayers > 0) || (Piece != null && Piece.IsMovableObjective))
            throw new System.InvalidOperationException("Generator needs a playable, unfrosted cell without a bird.");
        Piece = null;
        Generator = generator;
        IsPlayable = false;
    }

    // Delayed until after refill: the blocker stays solid throughout its final hit round.
    internal void RemoveDestroyedGenerator()
    {
        if (Generator == null || Generator.IsAlive) return;
        Generator = null;
        IsPlayable = true;
    }

    public int FrostLayers { get; private set; }

    public void SetFrostLayers(int layers)
    {
        if (layers < 0 || layers > 2) throw new System.ArgumentOutOfRangeException(nameof(layers));
        if (layers > 0 && !IsPlayable) throw new System.InvalidOperationException("Frost needs a playable cell.");
        FrostLayers = layers;
    }

    internal bool RemoveFrostLayer()
    {
        if (FrostLayers == 0) return false;
        FrostLayers--;
        return true;
    }

    // Called once for each distinct adjacent match, never once per piece.
    internal bool RegisterAdjacentMatch()
    {
        if (!IsLocked) return false;
        MatchesUntilUnlock--;
        if (MatchesUntilUnlock == 0) IsPlayable = true;
        return true;
    }

    private PieceData piece;
    public PieceData Piece
    {
        get => piece;
        set
        {
            if (!IsPlayable && value != null)
                throw new System.InvalidOperationException("Cannot place a piece in dead space.");
            piece = value;
        }
    }

    public bool IsEmpty => Piece == null;

    // Frost blocks player swaps, but the piece still participates in matches.
    public bool CanSwap => IsPlayable && !IsEmpty && FrostLayers == 0;

    public BoardCell(
        int x,
        int y,
        PieceData piece,
        bool isPlayable = true,
        int matchesUntilUnlock = 0)
    {
        X = x;
        Y = y;
        if (matchesUntilUnlock < 0)
            throw new System.ArgumentOutOfRangeException(nameof(matchesUntilUnlock));
        MatchesUntilUnlock = matchesUntilUnlock;
        IsPlayable = isPlayable && matchesUntilUnlock == 0;
        Piece = piece;
    }
}