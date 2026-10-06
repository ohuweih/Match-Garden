public class SpecialComboRequest
{
    public SpecialComboType Type { get; }

    public BoardCell FirstCell { get; }
    public BoardCell SecondCell { get; }

    public SpecialType FirstSpecial { get; }
    public SpecialType SecondSpecial { get; }

    public PieceType FirstColor { get; }
    public PieceType SecondColor { get; }

    public SpecialComboRequest(
        SpecialComboType type,
        BoardCell firstCell,
        BoardCell secondCell,
        SpecialType firstSpecial,
        SpecialType secondSpecial,
        PieceType firstColor,
        PieceType secondColor)
    {
        Type = type;

        FirstCell = firstCell;
        SecondCell = secondCell;

        FirstSpecial = firstSpecial;
        SecondSpecial = secondSpecial;

        FirstColor = firstColor;
        SecondColor = secondColor;
    }
}