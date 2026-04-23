using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
    WaitingRoll,
    SelectOrSpawn,
    Moving
}

public class GameController : MonoBehaviour
{
    public DiceController dice;
    public BoardGenerator board;
    public GameObject piecePrefab;

    private GameState state = GameState.WaitingRoll;
    private int diceValue;

    private readonly List<Piece> pieces = new();

    private void Start()
    {
        board.Init();
        dice.OnDiceRolled += OnDiceRolled;
        dice.CanRoll = () => state == GameState.WaitingRoll;

        if (!HasAnyPiecesOnBoard())
        {
            SpawnPiece();
            return;
        }
    }

    private void OnDiceRolled(int value)
    {
        if (state != GameState.WaitingRoll) return;

        diceValue = value;

        if (!HasAnyValidMoves(diceValue))
        {
            state = GameState.WaitingRoll;
            return;
        }

        state = GameState.SelectOrSpawn;
    }

    public void OnStartTileClicked()
    {
        if (state != GameState.SelectOrSpawn) return;
        if (diceValue != 6) return;

        SpawnPiece();
    }

    public bool HasAnyPiecesOnBoard()
    {
        return pieces.Count > 0;
    }

    public bool HasAnyValidMoves(int diceValue)
    {
        foreach (var p in pieces)
        {
            if (!p.isFinished && board.CanMove(p, diceValue))
                return true;
        }
        return false;
    }

    private void SpawnPiece()
    {
        Vector2Int start = board.PerimeterPath[board.startIndex];
        TileInstance tile = board.GetTile(start);

        if (tile.IsOccupied()) return;

        GameObject obj = Instantiate(piecePrefab, board.GridToWorld(start), Quaternion.identity);

        Piece p = obj.GetComponent<Piece>();
        p.Init(board, this);

        p.PlaceAtStart(board.startIndex);

        tile.SetPiece(p);
        pieces.Add(p);

        EndTurn();
    }

    public void OnPieceClicked(Piece piece)
    {
        if (state != GameState.SelectOrSpawn) return ;
        if (diceValue == 0) return;
        if (piece.isFinished) return;
        if (!board.CanMove(piece, diceValue)) return;

        state = GameState.Moving;

        int move = diceValue;
        diceValue = 0;

        piece.Move(move);
    }

    public void NotifyMoveFinished()
    {
        EndTurn();
    }

    private void EndTurn()
    {
        diceValue = 0;
        state = GameState.WaitingRoll; 
    }
}