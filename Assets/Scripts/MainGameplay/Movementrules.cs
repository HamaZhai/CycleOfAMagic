using System.Collections.Generic;
using UnityEngine;

public interface IBoardOccupancy
{
    bool IsTileOccupied(Vector2Int pos);
    Piece GetOccupant(Vector2Int pos);
}

public class MovementRules
{
    private readonly List<Vector2Int> perimeterPath;
    private readonly List<Vector2Int> centerPath;
    private readonly int startIndex;
    private readonly IBoardOccupancy occupancy;
    private readonly TrailSystem trailSystem;

    public MovementRules(
        List<Vector2Int> perimeterPath,
        List<Vector2Int> centerPath,
        int startIndex,
        IBoardOccupancy occupancy,
        TrailSystem trailSystem)
    {
        this.perimeterPath = perimeterPath;
        this.centerPath = centerPath;
        this.startIndex = startIndex;
        this.occupancy = occupancy;
        this.trailSystem = trailSystem;
    }

    public bool TryStep(ref MoveState state, Piece movingPiece, bool apply)
    {
        if (state.centerIndex >= 0)
            return TryStepInCenter(ref state, movingPiece, apply);

        if (state.canEnterCenter)
            return TryEnterCenter(ref state, movingPiece, apply);

        return TryStepOnPerimeter(ref state, movingPiece, apply);
    }

    public bool CanMove(MoveState state, Piece piece, int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            bool wasInCenter = state.centerIndex >= 0;
            bool couldEnterCenter = state.canEnterCenter;

            // apply=true на копии (struct) — state реально двигается вперёд
            if (!TryStep(ref state, piece, apply: true))
            {
                if (wasInCenter || couldEnterCenter)
                    return i > 0;
    
                return false;
            }
        }

        return true;
    }

    private bool TryStepInCenter(ref MoveState state, Piece movingPiece, bool apply)
    {
        int next = state.centerIndex + 1;

        if (next >= centerPath.Count)
        {
            // apply=true: анимация дошла до конца, останавливаемся.
            // apply=false: симуляция CanMove — двигаться некуда, возвращаем false.
            if (apply) state.centerIndex = centerPath.Count - 1;
            return apply;
        }

        Vector2Int nextPos = centerPath[next];
        if (IsTileBlockedByOther(nextPos, movingPiece))
            return false;

        if (apply) state.centerIndex = next;
        return true;
    }

    private bool TryEnterCenter(ref MoveState state, Piece movingPiece, bool apply)
    {
        Vector2Int centerEntry = centerPath[0];

        if (IsTileBlockedByOther(centerEntry, movingPiece))
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

        if (IsTileBlockedByOther(nextPos, movingPiece))
            return false;

        if (apply)
        {
            // hasLeftStart выставляем когда фигура УХОДИТ со стартовой клетки —
            // то есть когда текущая позиция = старт, а следующая уже нет.
            // Это гарантирует что фигура действительно сделала хотя бы один шаг.
            if (state.perimeterIndex == startIndex && !state.hasLeftStart)
                state.hasLeftStart = true;

            state.perimeterIndex = next;

            // canEnterCenter — когда фигура возвращается на старт после круга.
            // Проверяем hasLeftStart который уже true — значит фигура уходила.
            if (next == startIndex && state.hasLeftStart)
                state.canEnterCenter = true;
        }

        return true;
    }

    private bool IsTileBlockedByOther(Vector2Int pos, Piece movingPiece)
    {
        // 🟦 Конфликт Пространства: клетка заблокирована следом
        if (trailSystem != null && trailSystem.IsTrailBlocked(pos))
        return true;

        if (!occupancy.IsTileOccupied(pos)) return false;
        return occupancy.GetOccupant(pos) != movingPiece;
    }
}