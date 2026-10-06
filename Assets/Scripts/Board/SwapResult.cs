public class SwapResult
{
    public bool Success { get; }

    public BoardCell FirstCell { get; }

    public BoardCell SecondCell { get; }

    public BoardCell SpecialActivationCell { get; }

    public PieceType? ActivationColor { get; }

    public SpecialComboRequest SpecialCombo { get; }

    public bool HasDirectSpecialActivation =>
        SpecialActivationCell != null;

    public bool HasSpecialCombo =>
        SpecialCombo != null;

    public SwapResult(
        bool success,
        BoardCell firstCell,
        BoardCell secondCell,
        BoardCell specialActivationCell = null,
        PieceType? activationColor = null,
        SpecialComboRequest specialCombo = null)
    {
        Success = success;

        FirstCell = firstCell;
        SecondCell = secondCell;

        SpecialActivationCell =
            specialActivationCell;

        ActivationColor =
            activationColor;

        SpecialCombo =
            specialCombo;
    }
}