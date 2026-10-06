// Immutable visual state for a piece created/replaced before later cascades mutate it.
public sealed class PieceSnapshot
{
    public PieceType Color { get; }
    public SpecialType Special { get; }
    public bool IsPackage { get; }
    public bool IsBird { get; }
    public BirdRescueGoal RescueGoal { get; }
    public int BirdNumber { get; }

    public PieceSnapshot(PieceData piece)
    {
        Color = piece.Color; Special = piece.Special; IsPackage = piece.IsPackage;
        IsBird = piece.IsMovableObjective; RescueGoal = piece.RescueGoal; BirdNumber = piece.BirdNumber;
    }

    public PieceData CreatePiece() => IsPackage ? PieceData.CreatePackage(Color)
        : IsBird ? PieceData.CreateMovableObjective(RescueGoal, BirdNumber) : new PieceData(Color, Special);
}
