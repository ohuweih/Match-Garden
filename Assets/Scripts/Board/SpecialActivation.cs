public class SpecialActivation
{
    public SpecialEffectRecord ParentEffect { get; set; }

    public int X { get; }
    public int Y { get; }

    public SpecialType Type { get; }

    public PieceType? TargetColor { get; }

    public SpecialActivation(
        int x,
        int y,
        SpecialType type,
        PieceType? targetColor = null)
    {
        X = x;
        Y = y;
        Type = type;
        TargetColor = targetColor;
    }
}