using System;

[Serializable]
public sealed class BankedSpecial
{
    public string id;
    public string sourceLevelId;
    public PieceType color;
    public SpecialType special;

    public BankedSpecial Copy() => new BankedSpecial
    {
        id = id, sourceLevelId = sourceLevelId, color = color, special = special
    };

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(sourceLevelId))
            throw new ArgumentException("A banked special is missing its identity or source level.");
        if (!Enum.IsDefined(typeof(PieceType), color) ||
            !Enum.IsDefined(typeof(SpecialType), special) || special == SpecialType.None)
            throw new ArgumentException("A banked special has an invalid color or type.");
    }

    public string Label => color + " " + SpecialName(special);

    public static string SpecialName(SpecialType type)
    {
        switch (type)
        {
            case SpecialType.LineHorizontal: return "Horizontal line";
            case SpecialType.LineVertical: return "Vertical line";
            case SpecialType.ColorClear: return "Color clear";
            default: return type.ToString();
        }
    }
}

// Persist the inventory change before modifying the board. A failed write
// leaves both the selected piece and the bank exactly as they were.
public static class SpecialBankActions
{
    public static bool TryBank(CampaignProgress campaign, LevelDefinition level, BoardCell cell, out string error)
    {
        error = null;
        if (cell == null || !cell.IsPlayable || cell.IsEmpty || !cell.Piece.IsSpecial)
        {
            error = "Select a special piece first.";
            return false;
        }
        if (campaign == null) { error = "The special bank needs a level catalog."; return false; }
        if (!campaign.TryBankSpecial(level, cell.Piece, out error)) return false;
        cell.Piece.Special = SpecialType.None;
        return true;
    }

    public static bool TryPlace(CampaignProgress campaign, LevelDefinition level, string bankId, BoardCell cell, out string error)
    {
        error = null;
        if (cell == null || !cell.IsPlayable || cell.IsEmpty || cell.Piece.IsSpecial || !cell.Piece.IsColored)
        {
            error = "Choose a normal colored tile.";
            return false;
        }
        if (campaign == null) { error = "The special bank needs a level catalog."; return false; }
        if (!campaign.TryUseBankedSpecial(level, bankId, out var piece, out error)) return false;
        cell.Piece = piece;
        return true;
    }
}
