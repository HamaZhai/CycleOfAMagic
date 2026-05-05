using System.Collections.Generic;
using UnityEngine;

public class InactivitySystem : MonoBehaviour
{
    private const int IdlePenaltyPerTurn = 1;
    private const int DistributionDebtGain = 1;

    private readonly Dictionary<Piece, int> idleTurns = new();
    private readonly Dictionary<Piece, int> debt = new();

    public void RegisterPiece(Piece piece)
    {
        idleTurns[piece] = 0;
        debt.Remove(piece);
    }

    public void UnregisterPiece(Piece piece)
    {
        idleTurns.Remove(piece);
        debt.Remove(piece);
    }

    public void Clear()
    {
        idleTurns.Clear();
        debt.Clear();
    }

    public void AddDebt(IReadOnlyCollection<Piece> debtPieces)
    {
        if (debtPieces == null)
            return;

        foreach (var piece in debtPieces)
        {
            if (piece == null || piece.IsFinished)
                continue;

            debt[piece] = DistributionDebtGain;
            piece.Visuals?.SetDebtDisplay(GetDebt(piece));
            piece.Visuals?.SetPenaltyDisplay(GetIdlePenalty(piece));
            Debug.Log($"[Inactivity] {piece.name} debt +{DistributionDebtGain}, total debt {debt[piece]}");
        }
    }

    public void OnTurnEnd(Piece movedPiece, List<Piece> allPieces)
    {
        var movedPieces = new HashSet<Piece>();
        if (movedPiece != null)
            movedPieces.Add(movedPiece);

        OnTurnEnd(movedPieces, allPieces);
    }

    public void OnTurnEnd(IReadOnlyCollection<Piece> movedPieces, List<Piece> allPieces)
    {
        foreach (var piece in allPieces)
        {
            if (piece == null || piece.IsFinished)
                continue;

            if (!idleTurns.ContainsKey(piece))
                idleTurns[piece] = 0;

            if (WasMovedThisTurn(piece, movedPieces))
            {
                idleTurns[piece] = 0;
                ClearDebt(piece);
            }
            else
            {
                idleTurns[piece]++;
                Debug.Log($"[Inactivity] {piece.name} idle {idleTurns[piece]} turn(s), penalty -{GetPenalty(piece)}");
            }
        }
    }

    public int GetPenalty(Piece piece)
    {
        if (!idleTurns.TryGetValue(piece, out int idle))
            return GetDebt(piece);

        return GetIdlePenalty(piece) + GetDebt(piece);
    }

    public int ApplyPenalty(Piece piece, int diceValue)
    {
        int penalty = GetPenalty(piece);
        if (penalty == 0) return diceValue;

        int result = diceValue - penalty;
        Debug.Log($"[Inactivity] {piece.name} penalized: {diceValue} - {penalty} = {result}");

        piece.Visuals?.SetState(PieceVisualState.Penalty);

        return result;
    }

    public int GetIdleTurns(Piece piece) =>
        idleTurns.TryGetValue(piece, out int value) ? value : 0;

    public int GetIdlePenalty(Piece piece) =>
        GetIdleTurns(piece) * IdlePenaltyPerTurn;

    public int GetDebt(Piece piece) =>
        debt.TryGetValue(piece, out int value) ? value : 0;

    public bool HasDebt(Piece piece) => GetDebt(piece) > 0;

    private void ClearDebt(Piece piece)
    {
        if (!debt.Remove(piece))
            return;

        piece.Visuals?.SetDebtDisplay(0);
    }

    private bool WasMovedThisTurn(Piece piece, IReadOnlyCollection<Piece> movedPieces)
    {
        if (movedPieces == null)
            return false;

        foreach (var movedPiece in movedPieces)
        {
            if (movedPiece == piece)
                return true;
        }

        return false;
    }
}
