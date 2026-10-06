public class PieceData
{
    public bool IsPackage { get; private set; }
    public bool IsColored => !IsMovableObjective && !IsPackage;
    public static PieceData CreatePackage(PieceType underlyingColor = PieceType.Yellow) =>
        new PieceData(underlyingColor) { IsPackage = true };

    public bool IsMovableObjective { get; private set; }
    public long DistanceTraveled { get; private set; }

    public BirdRescueGoal RescueGoal { get; private set; }
    public int BirdNumber { get; private set; }

    public static PieceData CreateMovableObjective(BirdRescueGoal rescueGoal = null, int birdNumber = 0)
    {
        return new PieceData(PieceType.Yellow)
        {
            IsMovableObjective = true,
            RescueGoal = rescueGoal,
            BirdNumber = birdNumber
        };
    }

    internal void RecordTravel(int fromX, int fromY, int toX, int toY)
    {
        if (IsMovableObjective)
            DistanceTraveled += System.Math.Abs(toX - fromX) + System.Math.Abs(toY - fromY);
    }

    public PieceType Color { get; set; }

    public SpecialType Special { get; set; }

    public bool IsSpecial =>
        IsColored && Special != SpecialType.None;

    public bool IsMatchProtected { get; set; }

    public PieceData(
        PieceType color,
        SpecialType special = SpecialType.None)
    {
        Color = color;
        Special = special;

        IsMatchProtected = false;
    }
}