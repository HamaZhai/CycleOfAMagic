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

// GameController v0.5
//
// Два режима после броска:
//
// ActivePieceCount() == 0 → режим ожидания спавна
//   Любой бросок != 6 игнорируется (возвращаемся в WaitingRoll).
//   Бросок == 6 → спавн, если старт свободен.
//
// ActivePieceCount() >= 1 → режим игры
//   Проверяем HasAnyValidAction. Нет ни одного → проигрыш.
//   Есть хотя бы одно → SelectOrSpawn.
public class GameController : MonoBehaviour
{
    private const int MaxPieces = 3;

    [Header("References")]
    public DiceController dice;
    public BoardGenerator board;
    public GameObject piecePrefab;
    public VictoryCondition victoryCondition;
    public InactivitySystem inactivitySystem;

    public GameState State { get; private set; } = GameState.Initializing;
    public int CurrentDiceValue { get; private set; }

    public System.Action OnGameStarted;
    public System.Action OnTurnEnded;
    public System.Action<VictoryCondition.VictoryResult> OnGameOver;
    public System.Action<Piece> OnEnteredCenter;
    public System.Action OnLoss;

    private readonly List<Piece> pieces = new();
    private readonly MoveRangePlanner moveRangePlanner = new();
    private readonly List<MoveOption> selectedMoveOptions = new();
    private readonly List<TileInstance> highlightedTiles = new();
    private int nextCenterSlot;
    private Piece selectedPiece;

    private void Start()
    {
        board.Init();

        dice.OnDiceRolled += OnDiceRolled;
        dice.CanRoll = () => State == GameState.WaitingRoll;

        victoryCondition.Init(board);
        victoryCondition.OnVictory += HandleVictory;
        OnEnteredCenter += OnPieceEnteredCenter;

        nextCenterSlot = board.CenterPath.Count - 1;
    }

    public void StartGame()
    {
        if (State != GameState.Initializing) return;
        State = GameState.WaitingRoll;
        OnGameStarted?.Invoke();
        Debug.Log("[Game] Started");
    }

    // ── Бросок кубика ─────────────────────────────────────────

    private void OnDiceRolled(int rawValue)
    {
        if (State != GameState.WaitingRoll) return;

        ClearMoveSelection();
        CurrentDiceValue = rawValue;

        // ── Режим ожидания: нет активных фигур ──
        // Бросаем пока не выпадет 6, штрафов нет, проигрыша нет.
        if (ActivePieceCount() == 0)
        {
            if (rawValue == 6 && !board.GetTile(board.PerimeterPath[board.startIndex]).IsOccupied())
            {
                // Спавн первой фигуры
                State = GameState.SelectOrSpawn;
                // Сразу обрабатываем как клик на старт
                OnStartTileClicked();
            }
            else
            {
                // Не 6 — просто ждём следующего броска
                Debug.Log($"[Game] No active pieces, waiting for 6 (rolled {rawValue})");
                State = GameState.WaitingRoll;
                OnTurnEnded?.Invoke();
            }
            return;
        }

        // ── Режим игры: есть активные фигуры ──
        // Проигрыш только если нет ни одного валидного действия.
        if (!HasAnyValidAction(rawValue))
        {
            Debug.Log($"[Game] LOSS — no valid actions for dice {rawValue}");
            HandleLoss();
            return;
        }

        State = GameState.SelectOrSpawn;
    }

    // ── Клики ─────────────────────────────────────────────────

    public void OnTileClicked(TileInstance tile)
    {
        if (State != GameState.SelectOrSpawn) return;

        if (TryExecuteSelectedMove(tile))
            return;

        if (tile.isStartCorner)
            OnStartTileClicked();
    }

    public void OnStartTileClicked()
    {
        if (State != GameState.SelectOrSpawn) return;
        ClearMoveSelection();

        if (CurrentDiceValue != 6) return;
        if (ActivePieceCount() >= MaxPieces)
        {
            Debug.Log($"[Game] Max active pieces ({MaxPieces}) reached");
            return;
        }

        TileInstance startTile = board.GetTile(board.PerimeterPath[board.startIndex]);
        if (startTile.IsOccupied())
        {
            Debug.Log("[Game] Start tile occupied");
            return;
        }

        CreatePiece();
        EndTurn(null);
    }

    public void OnPieceClicked(Piece piece)
    {
        if (State != GameState.SelectOrSpawn) return;
        if (piece.IsFinished) return;

        int maxSteps = MaxSelectableSteps(piece, CurrentDiceValue);

        if (maxSteps <= 0)
        {
            piece.Visuals?.SetState(PieceVisualState.Penalty);
            Debug.Log($"[Game] {piece.name} range max = {maxSteps}, can't move");
            return;
        }

        List<MoveOption> options = moveRangePlanner.BuildOptions(
            piece,
            CurrentDiceValue,
            inactivitySystem.GetPenalty(piece),
            board
        );

        if (options.Count == 0)
        {
            Debug.Log($"[Game] {piece.name} has no legal move in range [1..{maxSteps}]");
            return;
        }

        SelectPieceMoveOptions(piece, options);
    }

    private void SelectPieceMoveOptions(Piece piece, List<MoveOption> options)
    {
        ClearMoveSelection();

        selectedPiece = piece;
        selectedMoveOptions.AddRange(options);

        TileInstance currentTile = board.GetTile(board.GetPiecePosition(piece));
        currentTile.SetHighlight(TileHighlight.Selected);
        highlightedTiles.Add(currentTile);

        foreach (var option in selectedMoveOptions)
        {
            TileInstance tile = board.GetTile(option.destination);
            if (highlightedTiles.Contains(tile)) continue;

            tile.SetHighlight(TileHighlight.Available);
            highlightedTiles.Add(tile);
        }

        int maxSteps = MaxSelectableSteps(piece, CurrentDiceValue);
        Debug.Log($"[Game] Selected {piece.name}, range [1..{maxSteps}], legal options: {selectedMoveOptions.Count}");
    }

    private bool TryExecuteSelectedMove(TileInstance tile)
    {
        if (selectedPiece == null || selectedMoveOptions.Count == 0)
            return false;

        for (int i = 0; i < selectedMoveOptions.Count; i++)
        {
            MoveOption option = selectedMoveOptions[i];
            if (board.GetTile(option.destination) != tile)
                continue;

            ExecuteMove(option);
            return true;
        }

        return false;
    }

    private void ExecuteMove(MoveOption option)
    {
        ClearMoveSelection();

        State = GameState.Moving;
        CurrentDiceValue = 0;

        option.piece.Move(option.steps);
    }

    private void ClearMoveSelection()
    {
        foreach (var tile in highlightedTiles)
        {
            if (tile == null) continue;
            tile.SetHighlight(tile.IsOccupied() ? TileHighlight.Occupied : TileHighlight.Normal);
        }

        highlightedTiles.Clear();
        selectedMoveOptions.Clear();
        selectedPiece = null;
    }

    // ── Уведомление от Piece ──────────────────────────────────

    public void NotifyMoveFinished(Piece piece, Vector2Int _fromPos)
    {
        EndTurn(piece);
    }

    // ── Спавн ─────────────────────────────────────────────────

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

        inactivitySystem.RegisterPiece(piece);
    }

    private void OnPieceEnteredCenter(Piece piece)
    {
        if (piece.assignedCenterSlot >= 0) return;
        piece.AssignCenterSlot(nextCenterSlot);
        nextCenterSlot = Mathf.Max(0, nextCenterSlot - 1);
        Debug.Log($"[Game] {piece.name} assigned center slot {piece.assignedCenterSlot}");
    }

    // ── Конец хода ────────────────────────────────────────────

    private void EndTurn(Piece movedPiece)
    {
        ClearMoveSelection();
        CurrentDiceValue = 0;

        if (movedPiece != null)
            inactivitySystem.OnTurnEnd(movedPiece, pieces);

        RefreshPenaltyDisplays();

        victoryCondition.CheckAfterTurn();
        if (State == GameState.GameOver) return;

        State = GameState.WaitingRoll;
        OnTurnEnded?.Invoke();
    }

    // ── Победа / Проигрыш ─────────────────────────────────────

    private void HandleVictory(VictoryCondition.VictoryResult result)
    {
        State = GameState.GameOver;
        Debug.Log($"[Game] Victory in {result.turnsElapsed} turns.");
        OnGameOver?.Invoke(result);
    }

    private void HandleLoss()
    {
        State = GameState.GameOver;
        OnLoss?.Invoke();
        OnGameOver?.Invoke(new VictoryCondition.VictoryResult { turnsElapsed = -1 });
    }

    // ── Валидация действий ────────────────────────────────────

    // Вызывается только когда ActivePieceCount() >= 1.
    // Спавн считается валидным действием даже если все активные заблокированы штрафами.
    private bool HasAnyValidAction(int dice)
    {
        // Спавн?
        if (dice == 6
            && ActivePieceCount() < MaxPieces
            && !board.GetTile(board.PerimeterPath[board.startIndex]).IsOccupied())
        {
            return true;
        }

        // Движение хотя бы одной фигуры?
        foreach (var p in pieces)
        {
            if (p == null || p.IsFinished) continue;
            if (moveRangePlanner.HasAnyOption(p, dice, inactivitySystem.GetPenalty(p), board))
                return true;
        }

        return false;
    }

    private int MaxSelectableSteps(Piece piece, int dice)
        => moveRangePlanner.GetMaxSteps(dice, inactivitySystem.GetPenalty(piece));

    // ── Визуал штрафов ────────────────────────────────────────

    private void RefreshPenaltyDisplays()
    {
        foreach (var p in pieces)
        {
            if (p == null) continue;
            int penalty = inactivitySystem.GetPenalty(p);
            p.Visuals?.SetPenaltyDisplay(penalty);
        }
    }

    // ── Хелперы ───────────────────────────────────────────────

    public bool CanSpawnMore() => ActivePieceCount() < MaxPieces;

    private int ActivePieceCount()
    {
        int count = 0;
        foreach (var p in pieces)
            if (p != null && !p.IsFinished) count++;
        return count;
    }
}
