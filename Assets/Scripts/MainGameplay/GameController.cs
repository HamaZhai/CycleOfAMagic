using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public DiceController dice;
    public BoardGenerator board;
    public GameObject piecePrefab;

    private int lastDiceValue = 0;
    private bool waitingForInput = false;

    private List<Piece> pieces = new List<Piece>();

    void Start()
    {
        board.Init();
        dice.OnDiceRolled += OnDiceRolled;
    }

    void OnDiceRolled(int value)
    {
        lastDiceValue = value;
        waitingForInput = true;
    }

    // Клик по стартовой клетке
    public void OnStartTileClicked()
    {
        if (!waitingForInput) return;
        if (lastDiceValue != 6) return;

        SpawnPiece();

        waitingForInput = false;
    }

    void SpawnPiece()
    {
        Vector3 startpos = board.GetWorldPosition(0);

        GameObject obj = Instantiate(piecePrefab, startpos, Quaternion.identity);

        Piece piece = obj.GetComponent<Piece>();
        
        
        piece.Init(board, this);
        piece.currentIndex = 0;
        piece.isInPlay = true;

        pieces.Add(piece);
    }

    // Клик по фигуре
    public void OnPieceClicked(Piece piece)
    {
        if (!waitingForInput) return;
        if (lastDiceValue == 0) return;

        if (piece.isInPlay)
            return;

        if (piece.canEnterCenter)
        {
            piece.EnterCenter(lastDiceValue);
        }
        else
        {
            piece.Move(lastDiceValue);
        }

        waitingForInput = false;
    }
}