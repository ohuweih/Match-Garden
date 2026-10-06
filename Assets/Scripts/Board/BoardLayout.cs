using System;

public enum BoardShape { Rectangle, Diamond, Cross, X }

public static class BoardLayout
{
    public static bool[,] CreateMask(int width, int height, BoardShape shape)
    {
        if (width < 1 || height < 1)
            throw new ArgumentOutOfRangeException("Board dimensions must be positive.");
        var mask = new bool[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Coordinates symmetric around the center, including even dimensions.
                double nx = (2.0 * x + 1 - width) / width;
                double ny = (2.0 * y + 1 - height) / height;
                switch (shape)
                {
                    case BoardShape.Rectangle: mask[x, y] = true; break;
                    case BoardShape.Diamond: mask[x, y] = Math.Abs(nx) + Math.Abs(ny) <= 1.0; break;
                    case BoardShape.Cross: mask[x, y] = Math.Abs(nx) <= 0.4 || Math.Abs(ny) <= 0.4; break;
                    case BoardShape.X: mask[x, y] = Math.Abs(Math.Abs(nx) - Math.Abs(ny)) <= 0.4; break;
                    default: throw new ArgumentOutOfRangeException(nameof(shape));
                }
            }
        }
        return mask;
    }
}
