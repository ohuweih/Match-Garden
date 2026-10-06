public static class RefillStrategyFactory
{
    public static IRefillStrategy Create(
        RefillMode mode)
    {
        switch (mode)
        {
            case RefillMode.Classic:
                return new ClassicRefillStrategy();

            case RefillMode.Chaos:
                return new ChaosRefillStrategy();

            case RefillMode.Normal:
                return new MatchDrivenRefillStrategy();

            default:
                return new ClassicRefillStrategy();
        }
    }
}