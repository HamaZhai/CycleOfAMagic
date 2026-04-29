using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Unity.VisualScripting;

// Фигура ближайшая к центру = "приоритетная".
// Нельзя игнорировать её более 1 хода подряд.
// Подсвечивается зелёным пульсом пока активен принудительный приоритет.
public class PrioritySystem : MonoBehaviour
{
    [Header("Priority Visual")]
    [SerializeField] private Color priorityColor = new Color(0.2f, 1f, 0.2f, 1f); // зелёный
    [SerializeField] private float pulseDuration = 0.5f;

    private Piece priorityPiece;
    private int turnsIgnored = 0;
    private Tween activePulseTween;

    // Вызывается после каждого хода.
    public void OnTurnEnd(Piece movedPiece, List<Piece> allPieces, BoardGenerator board)
    {
        UpdatePriority(allPieces, board);

        if (priorityPiece == null) return;

        if (movedPiece == priorityPiece)
        {
            turnsIgnored = 0;
            priorityPiece.Visuals?.SetState(PieceVisualState.Idle);
        }
        else
        {
            turnsIgnored++;
            Debug.Log($"[Priority] Priority piece ignored {turnsIgnored} turn(s)");
        }

        if (MustMovePriority())
            priorityPiece.Visuals?.SetState(PieceVisualState.Priority);
    }


    // true = игрок ОБЯЗАН двигать приоритетную фигуру в этот ход
    public bool MustMovePriority() => priorityPiece != null && turnsIgnored >= 1;

    public Piece GetPriorityPiece() => priorityPiece;

    // ── private ──────────────────────────────────────────────

    private void UpdatePriority(List<Piece> pieces, BoardGenerator board)
    {
        if (priorityPiece != null && priorityPiece.IsFinished)
        {
            priorityPiece = null;
            turnsIgnored = 0;
        }

        Piece closest = null;
        int closestDist = int.MaxValue;

        foreach (var p in pieces)
        {
            if (p == null || p.IsFinished) continue;
            int dist = GetDistanceToCenter(p, board);
            if (dist < closestDist) { closestDist = dist; closest = p; }
        }

        if (closest != priorityPiece)
        {
            // Сбрасываем визуал старой приоритетной
            if (priorityPiece != null && !priorityPiece.IsFinished)
                priorityPiece.Visuals?.SetState(PieceVisualState.Idle);

            priorityPiece = closest;
            turnsIgnored = 0;
            Debug.Log($"[Priority] New priority piece: {closest?.name}");
        }
    }

    // Расстояние до конца центрального пути
    private int GetDistanceToCenter(Piece piece, BoardGenerator board)
    {
        // Уже в центре — считаем оставшиеся шаги до финиша
        if (piece.centerIndex >= 0)
            return board.CenterPath.Count - 1 - piece.centerIndex;

        // Стоит на старте, готов войти в центр
        if (piece.canEnterCenter)
            return board.CenterPath.Count;

        int perimLen = board.PerimeterPath.Count;
        int start = board.startIndex;
        int current = piece.perimeterIndex;

        int stepsToStart = (start - current + perimLen) % perimLen;
        if (!piece.hasLeftStart) stepsToStart = perimLen;

        return stepsToStart + board.CenterPath.Count;
    }

    // ── визуал пульса ─────────────────────────────────────────

    private void StartPulse()
    {
        if (priorityPiece == null) return;

        var sr = priorityPiece.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        StopPulse();

        Color originalColor = sr.color;
        activePulseTween = sr.DOColor(priorityColor, pulseDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);

        Debug.Log($"[Priority] Pulse started on {priorityPiece.name}");
    }

    public void TriggerAttentionPulse()
    {
        priorityPiece?.Visuals?.SetState(PieceVisualState.PriorityUrgent);
    }


    private void StopPulse()
    {
        if (activePulseTween != null && activePulseTween.IsActive())
        {
            activePulseTween.Kill();
            activePulseTween = null;
        }

        // Сбрасываем цвет фигуры на дефолт
        if (priorityPiece != null)
        {
            var sr = priorityPiece.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.DOKill();
        }
    }
    private void OnDestroy() { }
}