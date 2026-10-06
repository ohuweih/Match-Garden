public static class SpecialComboRules
{
    public static SpecialComboType GetComboType(
        SpecialType first,
        SpecialType second)
    {
        bool firstIsLine =
            IsLine(first);

        bool secondIsLine =
            IsLine(second);

        if (
            first == SpecialType.ColorClear &&
            second == SpecialType.ColorClear
        )
        {
            return SpecialComboType.ColorClearColorClear;
        }

        if (
            first == SpecialType.Target &&
            second == SpecialType.Target
        )
        {
            return SpecialComboType.TargetTarget;
        }

        if (
            first == SpecialType.ColorClear ||
            second == SpecialType.ColorClear
        )
        {
            return SpecialComboType.ColorClearSpecial;
        }

        if (
            first == SpecialType.Target &&
            second == SpecialType.LineHorizontal ||
            second == SpecialType.Target &&
            first == SpecialType.LineHorizontal
        )
        {
            return SpecialComboType.TargetHorizontal;
        }

        if (
            first == SpecialType.Target &&
            second == SpecialType.LineVertical ||
            second == SpecialType.Target &&
            first == SpecialType.LineVertical
        )
        {
            return SpecialComboType.TargetVertical;
        }

        if (
            first == SpecialType.Target &&
            second == SpecialType.Bomb ||
            second == SpecialType.Target &&
            first == SpecialType.Bomb
        )
        {
            return SpecialComboType.TargetBomb;
        }

        if (
            firstIsLine &&
            secondIsLine
        )
        {
            return SpecialComboType.LineLine;
        }

        if (
            first == SpecialType.Bomb &&
            secondIsLine ||
            second == SpecialType.Bomb &&
            firstIsLine
        )
        {
            return SpecialComboType.BombLine;
        }

        if (
            first == SpecialType.Bomb &&
            second == SpecialType.Bomb
        )
        {
            return SpecialComboType.BombBomb;
        }

        return SpecialComboType.None;
    }

    private static bool IsLine(
        SpecialType type)
    {
        return
            type == SpecialType.LineHorizontal ||
            type == SpecialType.LineVertical;
    }
}