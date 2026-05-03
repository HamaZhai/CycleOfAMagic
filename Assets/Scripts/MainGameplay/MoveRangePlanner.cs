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
    private const int SingleActivePieceRangePenalty = 2;
    public const int MinDistributionPieces = 2;
    public const int MaxDistributionPieces = 3;

    public int GetMaxSteps(int diceValue, int penalty)
    {
        return diceValue - penalty;
    }

    public int GetSingleMoveMaxSteps(int diceValue, int penalty, int activePieceCount)
    {
        int maxSteps = diceValue - penalty;
        if (maxSteps <= 0)
            return maxSteps;

        if (activePieceCount == 1)
            maxSteps = ApplySingleActivePieceLimit(maxSteps);

        return maxSteps;
    }

    public bool IsValidDistributionPieceCount(int pieceCount)
    {
        return pieceCount >= MinDistributionPieces && pieceCount <= MaxDistributionPieces;
    }

    public int GetDistributionMaxSteps(int diceValue, IReadOnlyList<Piece> selectedPieces, InactivitySystem inactivitySystem)
    {
        if (selectedPieces == null || !IsValidDistributionPieceCount(selectedPieces.Count))
            return 0;

        int totalPenalty = 0;
        foreach (var piece in selectedPieces)
        {
            if (piece == null || piece.IsFinished)
                continue;

            totalPenalty += inactivitySystem.GetPenalty(piece);
        }

        return diceValue - totalPenalty;
    }

    private int ApplySingleActivePieceLimit(int maxSteps)
    {
        return Mathf.Max(1, maxSteps - SingleActivePieceRangePenalty);
    }

    public List<MoveOption> BuildOptions(Piece piece, int diceValue, int penalty, BoardGenerator board)
    {
        return BuildOptions(piece, GetMaxSteps(diceValue, penalty), board);
    }

    public List<MoveOption> BuildOptions(Piece piece, int maxSteps, BoardGenerator board)
    {
        var options = new List<MoveOption>();

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
