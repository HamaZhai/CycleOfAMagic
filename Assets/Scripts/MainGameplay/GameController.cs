using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
    Initializing,
    WaitingRoll,
    SelectOrSpawn,
    Moving,
    GameOver
}

public class GameController : MonoBehaviour
{
    [Header("References")]
    public DiceController dice;
    public BoardGenerator board;
    public GameObject piecePrefab;
    public VictoryCondition victoryCondition;

    public GameState State { get; private set; } = GameState.Initializing;
    public int CurrentDiceValue { get; private set; }

    public System.Action OnGameStarted;
    public System.Action OnTurnEnded;
    public System.Action<VictoryCondition.VictoryResult> OnGameOver;

    private readonly List<Piece> pieces = new();

    private void Start()
    {
        board.Init();
        dice.OnDiceRolled += OnDiceRolled;
        dice.CanRoll = () => State == GameState.WaitingRoll;

        victoryCondition.Init(board);
        victoryCondition.OnVictory += HandleVictory;

        SpawnFirstPiece();
    }

    public void StartGame()
    {
        if (State != GameState.Initializing) return;

        State = GameState.WaitingRoll;
        OnGameStarted?.Invoke();

        Debug.Log("[Game] Started");
    }

    private void OnDiceRolled(int value)
    {
        if (State != GameState.WaitingRoll) return;

        CurrentDiceValue = value;

        bool canSpawn = CurrentDiceValue == 6
            && !board.GetTile(board.PerimeterPath[board.startIndex]).IsOccupied();

        if (!canSpawn && !HasAnyValidMoves(CurrentDiceValue))
        {
            Debug.Log($"[Game] No valid moves for dice {value} -- skipping turn");
            EndTurn();
            return;
        }

        State = GameState.SelectOrSpawn;
    }

    private void SpawnFirstPiece()
    {
        TileInstance startTile = board.GetTile(board.PerimeterPath[board.startIndex]);
        if (startTile.IsOccupied()) return;

        CreatePiece();
        Debug.Log("[Game] First piece spawned");
    }

    public void OnStartTileClicked()
    {
        if (State != GameState.SelectOrSpawn) return;
        if (CurrentDiceValue != 6) return;

        TileInstance startTile = board.GetTile(board.PerimeterPath[board.startIndex]);
        if (startTile.IsOccupied())
        {
            Debug.Log("[Game] Start tile occupied -- cannot spawn");
            return;
        }

        CreatePiece();
        EndTurn();
    }

    public void OnPieceClicked(Piece piece)
    {
        if (State != GameState.SelectOrSpawn) return;
        if (CurrentDiceValue == 0) return;
        if (piece.IsFinished) return;
        if (!piece.CanMove(CurrentDiceValue)) return;

        State = GameState.Moving;

        int move = CurrentDiceValue;
        CurrentDiceValue = 0;

        piece.Move(move);
    }

    public void NotifyMoveFinished()
    {
        EndTurn();
    }

    private void CreatePiece()
    {
        Vector2Int startPos = board.PerimeterPath[board.startIndex];
        Vector3 worldPos = board.GridToWorld(startPos);

        GameObject obj = Instantiate(piecePrefab, worldPos, Quaternion.identity);
        Piece piece = obj.GetComponent<Piece>();
        piece.Init(board, this);
        piece.PlaceAtStart(board.startIndex);

        board.GetTile(startPos).SetPiece(piece);
        pieces.Add(piece);
    }

    private void EndTurn()
    {
        CurrentDiceValue = 0;

        // Проверка победы до смены состояния.
        // Если HandleVictory уже перевёл в GameOver -- не перезаписываем WaitingRoll.
        victoryCondition.CheckAfterTurn();

        if (State == GameState.GameOver) return;

        State = GameState.WaitingRoll;
        OnTurnEnded?.Invoke();
    }

    private void HandleVictory(VictoryCondition.VictoryResult result)
    {
        State = GameState.GameOver;
        Debug.Log($"[Game] Victory! Center filled in {result.turnsElapsed} turns.");
        OnGameOver?.Invoke(result);
    }

    public bool HasAnyValidMoves(int diceValue)
    {
        foreach (var piece in pieces)
        {
            if (!piece.IsFinished && piece.CanMove(diceValue))
                return true;
        }
        return false;
    }
}