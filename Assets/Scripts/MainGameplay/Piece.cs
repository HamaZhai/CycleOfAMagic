using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class Piece : MonoBehaviour
{
    public int perimeterIndex;
    public int centerIndex = -1;
    public bool hasLeftStart;

    private BoardGenerator board;
    private GameController game;

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
                    board.GetTile(board.CenterPath[centerIndex]).ClearPiece();
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

            // -------- PERIMETER MOVEMENT --------
            int nextIndex = (perimeterIndex + 1) % board.PerimeterPath.Count;

            var current = board.GetTile(board.PerimeterPath[perimeterIndex]);
            var nextTileP = board.GetTile(board.PerimeterPath[nextIndex]);

            current.ClearPiece();

            yield return MoveTo(board.PerimeterPath[nextIndex]);

            perimeterIndex = nextIndex;

            if (nextIndex == board.startIndex && hasLeftStart)
            {
                hasLeftStart = true;

                current.ClearPiece();

                centerIndex = 0;

                var centerTile = board.GetTile(board.CenterPath[0]);

                if (centerTile.IsOccupied())
                {
                    Debug.LogError("Center tile already occupied — logic broken");
                }

                centerTile.SetPiece(this);

                game.NotifyMoveFinished();
                yield break;
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
    }
}