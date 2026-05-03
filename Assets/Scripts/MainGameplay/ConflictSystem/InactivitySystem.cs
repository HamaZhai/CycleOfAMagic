using System.Collections.Generic;
using UnityEngine;

public class InactivitySystem : MonoBehaviour
{
    private readonly Dictionary<Piece, int> idleTurns = new();
    private readonly Dictionary<Piece, int> instabilityTurns = new();

    public void RegisterPiece(Piece piece)
    {
        idleTurns[piece] = 0;
        instabilityTurns.Remove(piece);
    }

    public void UnregisterPiece(Piece piece)
    {
        idleTurns.Remove(piece);
        instabilityTurns.Remove(piece);
    }

    public void Clear()
    {
        idleTurns.Clear();
        instabilityTurns.Clear();
    }

    public void MarkUnstable(IReadOnlyCollection<Piece> unstablePieces)
    {
        if (unstablePieces == null)
            return;

        foreach (var piece in unstablePieces)
        {
            if (piece == null || piece.IsFinished)
                continue;

            instabilityTurns[piece] = 1;
            Debug.Log($"[Inactivity] {piece.name} unstable for next turn");
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
            }
            else
            {
                idleTurns[piece]++;
                Debug.Log($"[Inactivity] {piece.name} idle {idleTurns[piece]} turn(s), penalty -{idleTurns[piece]}");
            }

            TickInstability(piece);
        }
    }

    public int GetPenalty(Piece piece)
    {
        if (!idleTurns.TryGetValue(piece, out int idle))
            return 0;

        return idle + GetInstabilityPenalty(piece);
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

    public bool IsUnstable(Piece piece) =>
        instabilityTurns.TryGetValue(piece, out int turns) && turns > 0;

    private int GetInstabilityPenalty(Piece piece)
    {
        return IsUnstable(piece) ? 1 : 0;
    }

    private void TickInstability(Piece piece)
    {
        if (!instabilityTurns.TryGetValue(piece, out int turns))
            return;

        turns--;
        if (turns <= 0)
            instabilityTurns.Remove(piece);
        else
            instabilityTurns[piece] = turns;
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
