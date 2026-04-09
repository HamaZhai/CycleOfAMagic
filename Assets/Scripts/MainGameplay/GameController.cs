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

        Vector2Int startGrid = board.PerimeterPath[board.startIndex];
        TileInstance startTile = board.GetTile(startGrid);

        if (startTile.IsOccupied())
        {
            Debug.Log("Start blocked, choose another piece");
            return; 
        }

        SpawnPiece();

        waitingForInput = false;
        lastDiceValue = 0;
    }

    public bool CanMove(Piece piece, int steps)
    {
        List<Vector2Int> path = BuildPath(piece, steps);

        if (path.Count < steps)
        {
            return false;
        }

        foreach (var pos in path)
        {
            var tile = board.GetTile(pos);

            if (tile.IsOccupied() && tile.OccupiedPiece != piece)
                return false;
        }

        return true;
    }

    List<Vector2Int> BuildPath (Piece piece , int steps)
    {
        List<Vector2Int> result = new();

        int tempPerimeter = piece.perimeterIndex;
        int tempCenter = piece.centerIndex;
        PieceState tempState = piece.state;
        bool tempHasLeft = piece.hasLeftStart;

        while (steps > 0)
        {
            if (tempState == PieceState.OnPerimeter)
            {

                int next = (tempPerimeter + 1) % board.PerimeterPath.Count;
                result.Add(board.PerimeterPath[next]);

                if (next != board.startIndex)
                {
                    tempHasLeft = true;
                }

                if (next == board.startIndex && tempHasLeft)
                {

                    tempState = PieceState.InCenter;
                    tempCenter = -1;
                }

                tempPerimeter = next;
            }
            else if (tempState == PieceState.InCenter)
            {
                int next = tempCenter + 1;

                if (next >= board.CenterPath.Count)
                    break;

                result.Add(board.CenterPath[next]);
                tempCenter = next;
            }

            steps --;
        }

        return result;
    }
    
    void SpawnPiece()
    {
        Vector2Int startGrid = board.PerimeterPath[board.startIndex];
        TileInstance startTile = board.GetTile(startGrid);

        if (startTile.IsOccupied())
        {
            Debug.Log("Start tile occupied");
            return;
        }
        
        GameObject obj = Instantiate(piecePrefab, board.GridToWorld(startGrid), Quaternion.identity);

        Piece piece = obj.GetComponent<Piece>();
        piece.Init(board, this);
        
        
        piece.perimeterIndex = 0;
        piece.isInPlay = true;

        startTile.SetPiece(piece);

        pieces.Add(piece);
    }

    // Клик по фигуре
    public void OnPieceClicked(Piece piece)
    {
        if (!waitingForInput) return;
        if (lastDiceValue == 0) return;

        if (!CanMove (piece, lastDiceValue))
        {
            Debug.Log("Move blocked");
            return;
        }

        piece.Move(lastDiceValue);

        waitingForInput = false;
        lastDiceValue = 0;
    }
}