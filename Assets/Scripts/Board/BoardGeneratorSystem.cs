using System;
using System.Collections.Generic;

// One instance per loaded board/resolver. Action identity prevents duplicate ticks.
public sealed class BoardGeneratorSystem
{
    private readonly HashSet<Guid> completedActions = new HashSet<Guid>();
    private readonly Func<GeneratorType, IGeneratorOutput> outputFactory;
    private static readonly IGeneratorOutput frost = new FrostGeneratorOutput();

    public BoardGeneratorSystem(Func<GeneratorType, IGeneratorOutput> outputFactory = null)
    {
        this.outputFactory = outputFactory ?? DefaultOutput;
    }

    private static IGeneratorOutput DefaultOutput(GeneratorType type)
    {
        if (type == GeneratorType.FrostMachine) return frost;
        throw new ArgumentException("Unsupported generator output.");
    }

    public bool GenerateAfterPlayerAction(BoardModel board, ResolutionSequence action, ResolutionRound round, Random random)
    {
        if (action.Source != MoveSource.Player || !completedActions.Add(action.Id)) return false;
        bool produced = false;
        for (int y = 0; y < board.Height; y++)
            for (int x = 0; x < board.Width; x++)
            {
                var cell = board.GetCell(x, y);
                var generator = cell.Generator;
                if (generator == null || !generator.AdvancePlayerAction()) continue;
                if (!outputFactory(generator.Type).TryGenerate(board, cell, round, random)) continue;
                round.GeneratorActivations.Add(new GeneratorActivation(x, y, generator.Type));
                produced = true;
            }
        return produced;
    }

    public static void DamageAdjacentMatches(BoardModel board, List<MatchResult> matches, ResolutionRound round)
    {
        if (matches == null || round == null) return;
        foreach (var match in matches)
            foreach (var cell in match.Cells)
            {
                Hit(board, cell.X - 1, cell.Y, round);
                Hit(board, cell.X + 1, cell.Y, round);
                Hit(board, cell.X, cell.Y - 1, round);
                Hit(board, cell.X, cell.Y + 1, round);
            }
    }

    private static void Hit(BoardModel board, int x, int y, ResolutionRound round)
    {
        if (x >= 0 && x < board.Width && y >= 0 && y < board.Height) round.HitGenerator(board.GetCell(x, y));
    }
}
