using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

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

    public void Move(int steps)
    {
        StartCoroutine(MoveRoutine(steps));
    }

    IEnumerator MoveRoutine(int steps)
    {
        while (steps-- > 0)
        {
            // -------- CENTER MOVEMENT --------
            if (centerIndex >= 0)
            {
                int next = centerIndex + 1;

                if (next >= board.CenterPath.Count)
                {
                    isFinished = true;
                    game.NotifyMoveFinished();
                    yield break;
                }

                var currentTile = board.GetTile(board.CenterPath[centerIndex]);
                var nextTile = board.GetTile(board.CenterPath[next]);

                currentTile.ClearPiece();

                yield return MoveTo(board.CenterPath[next]);

                nextTile.SetPiece(this);

                centerIndex = next;

                continue;
            }

            if (canEnterCenter)
            {
                var centerTile = board.GetTile(board.CenterPath[0]);

                if (!centerTile.IsOccupied())
                {
                    var currentTile = board.GetTile(board.PerimeterPath[perimeterIndex]);
                    currentTile.ClearPiece();

                    yield return MoveTo(board.CenterPath[0]);

                    centerTile.SetPiece(this);

                    centerIndex = 0;
                    canEnterCenter = false;

                    continue;
                }

            }

            // -------- PERIMETER MOVEMENT --------
            int nextIndex = (perimeterIndex + 1) % board.PerimeterPath.Count;

            var current = board.GetTile(board.PerimeterPath[perimeterIndex]);
            var nextTileP = board.GetTile(board.PerimeterPath[nextIndex]);

            current.ClearPiece();

            yield return MoveTo(board.PerimeterPath[nextIndex]);

            perimeterIndex = nextIndex;

            if (nextIndex == board.startIndex && hasLeftStart)
            {
                canEnterCenter = true;
            }

            hasLeftStart = true;

            nextTileP.SetPiece(this);
        }

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