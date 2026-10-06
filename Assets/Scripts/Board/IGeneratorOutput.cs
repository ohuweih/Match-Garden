using System;

// An output chooses eligible destinations and records every board mutation for playback.
// Future bird houses/region injectors can provide another implementation here.
public interface IGeneratorOutput
{
    bool TryGenerate(BoardModel board, BoardCell source, ResolutionRound round, Random random);
}
