using System.Collections;
using UnityEngine;

public class Piece : MonoBehaviour
{
    private BoardGenerator board;
    private GameController game;

    public int currentIndex = -1;

    public void Init(BoardGenerator boardGenerator, GameController controller)
    {
        board = boardGenerator;
        game = controller;
    }

    public void SpawnAtStart()
    {
        currentIndex = 0;
        transform.position = board.GetWorldPosition(currentIndex);
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
        for (int i = 0; i < steps; i++)
        {
            int nextIndex = currentIndex + 1;

            if (nextIndex >= board.Path.Count)
                nextIndex = 0;

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

        game.OnPieceFinishedMove();
    }
}