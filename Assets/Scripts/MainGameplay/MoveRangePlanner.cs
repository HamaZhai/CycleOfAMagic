using System.Collections.Generic;
using UnityEngine;

public struct MoveOption
{
    public Piece piece;
    public int steps;
    public Vector2Int destination;
}

public class MoveRangePlanner
{
    public int GetMaxSteps(int diceValue, int penalty)
    {
        return diceValue - penalty;
    }

    public List<MoveOption> BuildOptions(Piece piece, int diceValue, int penalty, BoardGenerator board)
    {
        var options = new List<MoveOption>();
        int maxSteps = GetMaxSteps(diceValue, penalty);

        if (piece == null || piece.IsFinished || maxSteps <= 0)
            return options;

        for (int steps = 1; steps <= maxSteps; steps++)
        {
            if (!board.TryGetMoveDestination(piece, steps, out Vector2Int destination))
                continue;

            options.Add(new MoveOption
            {
                piece = piece,
                steps = steps,
                destination = destination
            });
        }

        return options;
    }

    public bool HasAnyOption(Piece piece, int diceValue, int penalty, BoardGenerator board)
    {
        int maxSteps = GetMaxSteps(diceValue, penalty);
        if (piece == null || piece.IsFinished || maxSteps <= 0)
            return false;

        for (int steps = 1; steps <= maxSteps; steps++)
        {
            if (board.TryGetMoveDestination(piece, steps, out _))
                return true;
        }

        return false;
    }
}
