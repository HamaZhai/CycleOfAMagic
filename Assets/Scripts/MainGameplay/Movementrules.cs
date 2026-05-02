using System.Collections.Generic;
using UnityEngine;

public interface IBoardOccupancy
{
    bool IsTileOccupied(Vector2Int pos);
    Piece GetOccupant(Vector2Int pos);
}

// MovementRules v0.5
//
// Правило точного входа в центр:
//   Нельзя пройти МИМО входа (продолжить по периметру после старта).
//   Нельзя пройти НАСКВОЗЬ (выйти за конец centerPath).
//   Можно войти в середину центра если шагов ровно столько.
//
//   Реализация: CanMove не блокирует canEnterCenter в середине пути —
//   TryStep сам маршрутизирует через TryEnterCenter → TryStepInCenter.
//   Переполнение ловит TryStepInCenter (next >= centerPath.Count → false).
public class MovementRules
{
    private readonly List<Vector2Int> perimeterPath;
    private readonly List<Vector2Int> centerPath;
    private readonly int startIndex;
    private readonly IBoardOccupancy occupancy;

    public MovementRules(
        List<Vector2Int> perimeterPath,
        List<Vector2Int> centerPath,
        int startIndex,
        IBoardOccupancy occupancy)
    {
        this.perimeterPath = perimeterPath;
        this.centerPath = centerPath;
        this.startIndex = startIndex;
        this.occupancy = occupancy;
    }

    public bool TryStep(ref MoveState state, Piece movingPiece, bool apply)
    {
        if (state.centerIndex >= 0)
            return TryStepInCenter(ref state, movingPiece, apply);

        if (state.canEnterCenter)
            return TryEnterCenter(ref state, movingPiece, apply);

        return TryStepOnPerimeter(ref state, movingPiece, apply);
    }

    // Проверяет может ли фигура пройти ровно steps шагов.
    //
    // Точный вход реализован через TryStep:
    //   — если фигура на старте (canEnterCenter=true), TryStep идёт в TryEnterCenter,
    //     а не продолжает по периметру — обход невозможен автоматически.
    //   — если внутри центра шагов больше чем осталось клеток — TryStepInCenter
    //     вернёт false (next >= centerPath.Count).
    //
    // Дополнительная проверка: если фигура УЖЕ стоит с canEnterCenter=true
    // и шагов больше чем клеток в центре — сразу false (экономим итерации).
    public bool CanMove(MoveState state, Piece piece, int steps)
    {
        return TryGetDestination(state, piece, steps, out _);
    }

    public bool TryGetDestination(MoveState state, Piece piece, int steps, out Vector2Int destination)
    {
        destination = GetCurrentPosition(state);

        if (steps <= 0)
            return false;

        // Быстрая проверка переполнения: фигура уже готова войти,
        // но шагов больше чем весь центр вмещает.
        if (state.canEnterCenter && steps > centerPath.Count)
            return false;

        for (int i = 0; i < steps; i++)
        {
            if (!TryStep(ref state, piece, apply: true))
                return false;
        }

        destination = GetCurrentPosition(state);
        return true;
    }

    public Vector2Int GetCurrentPosition(MoveState state)
    {
        if (state.centerIndex >= 0)
            return centerPath[state.centerIndex];

        return perimeterPath[state.perimeterIndex];
    }

    private bool TryStepInCenter(ref MoveState state, Piece movingPiece, bool apply)
    {
        int next = state.centerIndex + 1;

        // Переполнение — за конец центра выйти нельзя
        if (next >= centerPath.Count)
            return false;

        Vector2Int nextPos = centerPath[next];
        if (IsOccupiedByOther(nextPos, movingPiece))
            return false;

        if (apply) state.centerIndex = next;
        return true;
    }

    private bool TryEnterCenter(ref MoveState state, Piece movingPiece, bool apply)
    {
        Vector2Int centerEntry = centerPath[0];

        if (IsOccupiedByOther(centerEntry, movingPiece))
            return false;

        if (apply)
        {
            state.centerIndex = 0;
            state.canEnterCenter = false;
        }

        return true;
    }

    private bool TryStepOnPerimeter(ref MoveState state, Piece movingPiece, bool apply)
    {
        int next = (state.perimeterIndex + 1) % perimeterPath.Count;
        Vector2Int nextPos = perimeterPath[next];

        if (IsOccupiedByOther(nextPos, movingPiece))
            return false;

        if (apply)
        {
            if (state.perimeterIndex == startIndex && !state.hasLeftStart)
                state.hasLeftStart = true;

            state.perimeterIndex = next;

            if (next == startIndex && state.hasLeftStart)
                state.canEnterCenter = true;
        }

        return true;
    }

    private bool IsOccupiedByOther(Vector2Int pos, Piece movingPiece)
    {
        if (!occupancy.IsTileOccupied(pos)) return false;
        return occupancy.GetOccupant(pos) != movingPiece;
    }
}
