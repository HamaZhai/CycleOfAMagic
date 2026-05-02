using System.Collections;
using UnityEngine;

public struct MoveState
{
    public int perimeterIndex;
    public int centerIndex;
    public bool hasLeftStart;
    public bool canEnterCenter;
}

public class Piece : MonoBehaviour
{
    // Состояние фигуры — публичное для чтения, управляется изнутри
    public int perimeterIndex { get; private set; }
    public int centerIndex { get; private set; } = -1;
    public bool hasLeftStart { get; private set; }
    public bool canEnterCenter { get; private set; }
    public bool IsFinished { get; private set; }
    public PieceVisuals Visuals { get; private set; }

    private BoardGenerator board;
    private GameController game;

    public int assignedCenterSlot { get; private set; } = -1; // -1 = не назначен

    public void AssignCenterSlot(int slot)
    {
        assignedCenterSlot = slot;
    }

    private void Awake()
    {
        Visuals = GetComponent<PieceVisuals>();
    }

    public void Init(BoardGenerator b, GameController g)
    {
        board = b;
        game = g;
    }

    public void PlaceAtStart(int index)
    {
        perimeterIndex = index;
        hasLeftStart = false;
        IsFinished = false;
    }

    // Нужен BoardGenerator.CanMove — снапшот текущего состояния.
    // Struct копируется по значению — безопасно.
    public MoveState GetMoveState() => new MoveState
    {
        perimeterIndex = perimeterIndex,
        centerIndex = centerIndex,
        hasLeftStart = hasLeftStart,
        canEnterCenter = canEnterCenter
    };

    public void HandleClick()
    {
        game.OnPieceClicked(this);
    }

    public bool CanMove(int steps) => board.CanMove(this, steps);

    public void Move(int steps)
    {
        StartCoroutine(MoveRoutine(steps));
    }

   private IEnumerator MoveRoutine(int steps)
    {
        MoveState state = GetMoveState();
 
        Vector2Int startPos = GetCurrentPos(state);

        while (steps-- > 0)
        {
            Vector2Int from = GetCurrentPos(state);
            bool WasInCenter = state.centerIndex >= 0;
 
            if (!board.TryStep(ref state, this, true))
            {
                break;
            }
 
            if (!WasInCenter && state.centerIndex >= 0)
            {
                // Вошли в центр — уведомляем GameController для статистики и возможного ИИ
                game.OnEnteredCenter?.Invoke(this);
            }
            Vector2Int to = GetCurrentPos(state);
 
            board.GetTile(from).ClearPiece();
            board.GetTile(to).SetPiece(this);
 
            yield return AnimateMoveTo(to);
        }
 
        ApplyState(state);
 
        bool reachedAssignedSlot = assignedCenterSlot >= 0 && centerIndex == assignedCenterSlot;
        bool reachedEnd = centerIndex == board.CenterPath.Count - 1;

        // Проверка финиша — фигура дошла до конца центрального пути
        if (reachedAssignedSlot || reachedEnd)
        {
            IsFinished = true;
            Visuals?.SetState(PieceVisualState.Finished);
            Visuals?.SetPenaltyDisplay(0); // убираем метку штрафа при финише
            Debug.Log($"[Piece] {name} finished!");
        }

        game.NotifyMoveFinished(this, startPos);
    }

    private Vector2Int GetCurrentPos(MoveState state)
    {
        if (state.centerIndex >= 0)
            return board.CenterPath[state.centerIndex];

        return board.PerimeterPath[state.perimeterIndex];
    }

    private void ApplyState(MoveState state)
    {
        perimeterIndex = state.perimeterIndex;
        centerIndex = state.centerIndex;
        hasLeftStart = state.hasLeftStart;
        canEnterCenter = state.canEnterCenter;
    }

    private IEnumerator AnimateMoveTo(Vector2Int pos)
    {
        Vector3 target = board.GridToWorld(pos);

        while ((transform.position - target).sqrMagnitude > 0.001f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, 12f * Time.deltaTime);
            yield return null;
        }

        transform.position = target;
    }
}