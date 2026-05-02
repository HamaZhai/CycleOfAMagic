using System.Collections.Generic;
using UnityEngine;

// Система инерции фигур — v0.5
//
// Если фигура НЕ двигалась N ходов подряд:
//   idle = 1 → штраф -1 к броску
//   idle = 2 → штраф -2 к броску
//   (и т.д. — штраф = кол-во пропущенных ходов)
//
// Штраф применяется к ИТОГОВОМУ броску, не к шагам конкретной фигуры.
// Если итог ≤ 0 — GameController объявляет проигрыш.
//
// Визуал: фигура с активным штрафом пульсирует оранжевым при выборе.
public class InactivitySystem : MonoBehaviour
{
    private readonly Dictionary<Piece, int> idleTurns = new();

    public void RegisterPiece(Piece piece)
    {
        idleTurns[piece] = 0;
    }

    public void UnregisterPiece(Piece piece)
    {
        idleTurns.Remove(piece);
    }

    // Вызывается после каждого хода. Обновляет счётчики всех активных фигур.
    public void OnTurnEnd(Piece movedPiece, List<Piece> allPieces)
    {
        foreach (var p in allPieces)
        {
            if (p == null || p.IsFinished) continue;

            if (!idleTurns.ContainsKey(p))
                idleTurns[p] = 0;

            if (p == movedPiece)
            {
                idleTurns[p] = 0;
            }
            else
            {
                idleTurns[p]++;
                Debug.Log($"[Inactivity] {p.name} idle {idleTurns[p]} turn(s) → penalty -{idleTurns[p]}");
            }
        }
    }

    // Возвращает суммарный штраф для выбранной фигуры.
    // Штраф = кол-во ходов простоя (idle turns).
    // Если штраф > 0 — запускает визуальный сигнал на фигуре.
    public int GetPenalty(Piece piece)
    {
        if (!idleTurns.TryGetValue(piece, out int idle)) return 0;
        return idle; // штраф = idle напрямую
    }

    // Применяет штраф к броску и возвращает итоговое значение.
    // Итог может быть ≤ 0 — GameController должен проверить это и объявить проигрыш.
    public int ApplyPenalty(Piece piece, int diceValue)
    {
        int penalty = GetPenalty(piece);
        if (penalty == 0) return diceValue;

        int result = diceValue - penalty;
        Debug.Log($"[Inactivity] {piece.name} penalized: {diceValue} - {penalty} = {result}");

        // Показываем штраф визуально
        piece.Visuals?.SetState(PieceVisualState.Penalty);

        return result;
    }

    public int GetIdleTurns(Piece piece) =>
        idleTurns.TryGetValue(piece, out int v) ? v : 0;
}