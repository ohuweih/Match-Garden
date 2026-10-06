using System;

public enum ObjectiveType { Score, ClearColor, UnlockTiles, RescueBirds, ClearFrost, DestroyFrostMachines }

[Serializable]
public class ObjectiveDefinition
{
    public ObjectiveType Type = ObjectiveType.Score;
    public int Target = 500;
    public PieceType Color = PieceType.Red;
}
