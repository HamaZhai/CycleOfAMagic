using System.Collections;
using UnityEngine;

public enum PieceState
{
    OnPerimeter,
    InCenter,
    Finished
}

public class Piece : MonoBehaviour
{
    public PieceState state = PieceState.OnPerimeter;

    public bool completedLoop = false;
    public bool isInPlay = false;

    public int perimeterIndex = 0;
    private int centerIndex = -1;

    private bool hasLeftStart = false;

    private BoardGenerator board;
    private GameController game;

    public void Init(BoardGenerator boardGenerator, GameController controller)
    {
        board = boardGenerator;
        game = controller;
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
        while (steps > 0)
        {
            // -------- PERIMETER --------
            if (state == PieceState.OnPerimeter)
            {
                int nextIndex = (perimeterIndex + 1) % board.PerimeterPath.Count;

                yield return MoveTo(board.PerimeterPath[nextIndex]);

                perimeterIndex = nextIndex;

                if (perimeterIndex != board.startIndex)
                    hasLeftStart = true;

                if (perimeterIndex == board.startIndex && hasLeftStart)
                {
                    completedLoop = true;
                    state = PieceState.InCenter;
                    centerIndex = -1;
                }
            }
            // -------- CENTER --------
            else if (state == PieceState.InCenter)
            {
                int nextIndex = centerIndex + 1;

                if (nextIndex >= board.CenterPath.Count)
                {
                    state = PieceState.Finished;
                    yield break;
                }

                yield return MoveTo(board.CenterPath[nextIndex]);
                centerIndex = nextIndex;
            }

            steps--;
        }
    }

    IEnumerator MoveTo(Vector2Int gridPos)
    {
        Vector3 target = board.GridToWorld(gridPos);

        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                6f * Time.deltaTime
            );
            yield return null;
        }
    }
}