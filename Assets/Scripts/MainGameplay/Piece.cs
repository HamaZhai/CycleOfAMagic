using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public struct MoveState
{
    public int perimeterIndex;
    public int centerIndex;
    public bool hasLeftStart;
    public bool canEnterCenter;
}

public class Piece : MonoBehaviour
{
    public int perimeterIndex;
    public int centerIndex = -1;
    public bool hasLeftStart;
    public bool isFinished;

    private BoardGenerator board;
    private GameController game;

    public bool canEnterCenter = false;

    public void Init(BoardGenerator b, GameController g)
    {
        board = b;
        game = g;
    }

    public void PlaceAtStart(int index)
    {
        perimeterIndex = index;
        hasLeftStart = false;
    }

    public void HandleClick()
    {
        game.OnPieceClicked(this);
    }

    public bool CanMove(int steps)
    {
        MoveState state = new MoveState
        {
            perimeterIndex = perimeterIndex,
            centerIndex = centerIndex,
            hasLeftStart = hasLeftStart,
            canEnterCenter = canEnterCenter
        };

        for (int i = 0 ; i < steps ; i++)
        {
            if(!board.TryStep(ref state, this, false))
            {   return false;}
        }

        return true;
    }

    public void Move(int steps)
    {
        StartCoroutine(MoveRoutine(steps));
    }

    IEnumerator MoveRoutine(int steps)
    {
        MoveState state = new MoveState
        {
            perimeterIndex = perimeterIndex,
            centerIndex = centerIndex,
            hasLeftStart = hasLeftStart,
            canEnterCenter = canEnterCenter
        };

        while (steps-- > 0)
        {
            Vector2Int from;
            Vector2Int to;

            if (state.centerIndex >= 0)
                from = board.CenterPath[state.centerIndex];
            else
                from = board.PerimeterPath[state.perimeterIndex];

            if (!board.TryStep(ref state, this, true))
                yield break;

            if (state.centerIndex >= 0)
                to = board.CenterPath[state.centerIndex];
            else
                to = board.PerimeterPath[state.perimeterIndex];

            board.GetTile(from).ClearPiece();
            board.GetTile(to).SetPiece(this);

            yield return MoveTo(to);
        }

        perimeterIndex = state.perimeterIndex;
        centerIndex = state.centerIndex;
        hasLeftStart = state.hasLeftStart;
        canEnterCenter = state.canEnterCenter;

        game.NotifyMoveFinished();
    }

    IEnumerator MoveTo(Vector2Int pos)
    {
        Vector3 target = board.GridToWorld(pos);

        while ((transform.position - target).sqrMagnitude > 0.001f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, 6f * Time.deltaTime);
            yield return null;
        }

        transform.position = target;
    }
}