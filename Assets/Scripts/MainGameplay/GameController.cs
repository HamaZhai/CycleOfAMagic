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

    [Header("Systems")]
    public TrailSystem trailSystem;
    public PrioritySystem prioritySystem;
    public InactivitySystem inactivitySystem;


    public GameState State { get; private set; } = GameState.Initializing;
    public int CurrentDiceValue { get; private set; }

    public System.Action OnGameStarted;
    public System.Action OnTurnEnded;
    public System.Action<VictoryCondition.VictoryResult> OnGameOver;
    public System.Action<Piece> OnEnteredCenter;

    private readonly List<Piece> pieces = new();
    private Piece lastMovedPiece;

    private int nextCenterSlot;

    private void Start()
    {
        board.Init();
        dice.OnDiceRolled += OnDiceRolled;
        dice.CanRoll = () => State == GameState.WaitingRoll;

        victoryCondition.Init(board);
        victoryCondition.OnVictory += HandleVictory;

        SpawnFirstPiece();

        nextCenterSlot = board.CenterPath.Count - 1; // стартуем с дальнего конца центра
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
            EndTurn(null);
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
        EndTurn(null);
    }

    public void OnPieceClicked(Piece piece)
    {
        if (State != GameState.SelectOrSpawn) return;
        if (CurrentDiceValue == 0) return;
        if (piece.IsFinished) return;
        if (!piece.CanMove(CurrentDiceValue)) return;

         // 🟨 Конфликт Приоритетов: если обязаны двигать приоритетную — блокируем другие
        if (prioritySystem.MustMovePriority() && piece != prioritySystem.GetPriorityPiece())
        {
            prioritySystem.TriggerAttentionPulse(); // ← добавить
            Debug.Log($"[Priority] Must move priority piece: {prioritySystem.GetPriorityPiece()?.name}");
            return;
        }

        // 🟥 Конфликт Времени: применяем штраф за бездействие
        int steps = inactivitySystem.ApplyPenalty(piece, CurrentDiceValue);
        
        if (!piece.CanMove(steps))
        {
            // Попробуем хотя бы 1 шаг если штраф срезал слишком много
            if (steps > 1 && piece.CanMove(1)) steps = 1;
            else return;
        }

        State = GameState.Moving;
        lastMovedPiece = piece;
        CurrentDiceValue = 0;

        piece.Move(steps);
    }
    
    // Вызывается из Piece по окончании движения
    public void NotifyMoveFinished(Piece piece, Vector2Int fromPos)
    {
        // Назначение слота убрано отсюда — теперь в OnPieceEnteredCenter
        TileInstance tile = board.GetTile(fromPos);
        trailSystem.RegisterTrail(fromPos, tile);
        EndTurn(piece);
    }

    private void CreatePiece()
    {
        Vector2Int startPos = board.PerimeterPath[board.startIndex];
        Vector3 worldPos = board.GridToWorld(startPos);

        GameObject obj = Instantiate(piecePrefab, worldPos, Quaternion.identity);
        Piece piece = obj.GetComponent<Piece>();
        piece.Init(board, this);
        piece.PlaceAtStart(board.startIndex);

        OnEnteredCenter += OnPieceEnteredCenter;
        board.GetTile(startPos).SetPiece(piece);
        pieces.Add(piece);

        // Регистрируем в системе бездействия
        inactivitySystem.RegisterPiece(piece);
    }

    private void OnPieceEnteredCenter(Piece piece)
    {
        if (piece.assignedCenterSlot >= 0) return; // уже назначен, игнорируем повторные входы

        piece.AssignCenterSlot(nextCenterSlot);
        nextCenterSlot = Mathf.Max(0, nextCenterSlot - 1); // двигаемся от конца к началу центра
        Debug.Log($"[Game] {piece.name} entered center, assigned slot {piece.assignedCenterSlot}");
    }

    private void EndTurn(Piece movedPiece)
    {
        CurrentDiceValue = 0;

        // Обновляем системы конфликтов
        if (movedPiece != null)
        {
            prioritySystem.OnTurnEnd(movedPiece, pieces, board);
            inactivitySystem.OnTurnEnd(movedPiece, pieces);
        }

        trailSystem.TickDown();
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