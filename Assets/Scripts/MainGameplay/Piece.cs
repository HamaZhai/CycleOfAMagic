using JetBrains.Annotations;
using System.Collections;
using System.Resources;
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

    private BoardGenerator board;
    private GameController game;

    public int currentIndex = -1;
    public int lapCount = 0;
    public int centerIndex = 0;
    public bool isInPlay = false;


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

    public void EnterCenter(int steps)
    {
        if(lapCount < 1 && currentIndex != 0)
            return;

        state = PieceState.InCenter;
        centerIndex = 0;

        StartCoroutine(MoveInCenter(steps));
    }

    IEnumerator MoveRoutine(int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            int nextIndex = currentIndex + 1;

            if (nextIndex >= board.Path.Count)
            {
                nextIndex = 0;
                lapCount++;
            }


            Vector3 target = board.GetWorldPosition(nextIndex);

            while (Vector3.Distance(transform.position, target) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    target,
                    6f * Time.deltaTime
                );
                yield return null;
            }

            currentIndex = nextIndex;
        }

    }

    public bool canEnterCenter => lapCount >= 1 && currentIndex == 0;

    IEnumerator MoveInCenter(int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            int nextindex = centerIndex + 1;

            if (nextindex >= board.CenterPath.Count)
            {
                state = PieceState.Finished;
                yield break;
            }

            Vector3 target = new Vector3(board.CenterPath[nextindex].x * board.tileSize, board.CenterPath[nextindex].y * board.tileSize,0) - board.transform.position;

            while (Vector3.Distance(transform.position , target) > 0.01f) 
            {
                transform.position = Vector3.MoveTowards(transform.position, target, 6f * Time.deltaTime);
                yield return null;
            }

            centerIndex = nextindex;
        }
    }
}