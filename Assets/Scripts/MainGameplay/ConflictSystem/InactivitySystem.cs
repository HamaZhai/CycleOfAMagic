using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

// Фигура не двигалась 2 хода подряд → следующий ход -1 шаг (минимум 1).
// При применении штрафа фигура пульсирует оранжевым.
public class InactivitySystem : MonoBehaviour
{
    [Header("Penalty Visual")]
    [SerializeField] private Color penaltyColor = new Color(1f, 0.5f, 0f, 1f); // оранжевый
    [SerializeField] private float pulseInterval = 0.18f;
    [SerializeField] private int pulseCount = 4; // кол-во вспышек

    private readonly Dictionary<Piece, int> idleTurns = new();

    public void RegisterPiece(Piece piece)
    {
        idleTurns[piece] = 0;
    }

    // Вызывается после хода. Обновляет счётчики для всех фигур.
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
                Debug.Log($"[Inactivity] {p.name} idle {idleTurns[p]} turn(s)");
            }
        }
    }

    // Возвращает финальное количество шагов с учётом штрафа.
    // Если штраф применяется — запускает визуальный пульс на фигуре.
    public int ApplyPenalty(Piece piece, int diceValue)
    {
        if (!idleTurns.TryGetValue(piece, out int idle)) return diceValue;
        if (idle < 2) return diceValue;

        int penalized = Mathf.Max(1, diceValue - 1);
        Debug.Log($"[Inactivity] {piece.name} penalized: {diceValue} → {penalized}");

        piece.Visuals?.SetState(PieceVisualState.Penalty);

        return penalized;
    }

    // PlayPenaltyPulse — удалить полностью

    public int GetIdleTurns(Piece piece) =>
        idleTurns.TryGetValue(piece, out int v) ? v : 0;

    // ── визуал штрафа ─────────────────────────────────────────

    private void PlayPenaltyPulse(Piece piece)
    {
        var sr = piece.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        Color original = sr.color;
        sr.DOKill();

        // pulseCount вспышек туда-обратно
        sr.DOColor(penaltyColor, pulseInterval)
          .SetLoops(pulseCount * 2, LoopType.Yoyo)
          .SetEase(Ease.InOutFlash)
          .OnComplete(() =>
          {
              // Восстанавливаем исходный цвет после анимации
              sr.DOColor(original, pulseInterval * 0.5f);
          });
    }
}