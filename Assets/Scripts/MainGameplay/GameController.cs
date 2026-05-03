using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum GameState
{
    Initializing,
    WaitingRoll,
    SelectOrSpawn,
    Moving,
    GameOver
}

public enum TurnMoveMode
{
    Single,
    Distribution
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
    public TurnMoveMode CurrentMoveMode { get; private set; } = TurnMoveMode.Single;
    public int CurrentDiceValue { get; private set; }
    public int CurrentDistributionBudget { get; private set; }
    public int RemainingDistributionBudget { get; private set; }

    public System.Action OnGameStarted;
    public System.Action OnTurnEnded;
    public System.Action<VictoryCondition.VictoryResult> OnGameOver;
    public System.Action<Piece> OnEnteredCenter;
    public System.Action OnLoss;

    private readonly List<Piece> pieces = new();
    private readonly MoveRangePlanner moveRangePlanner = new();
    private readonly List<MoveOption> selectedMoveOptions = new();
    private readonly List<TileInstance> highlightedTiles = new();
    private readonly List<Piece> distributionPieces = new();
    private readonly List<Piece> movedDistributionOrder = new();
    private readonly HashSet<Piece> movedDistributionPieces = new();
    private readonly List<Piece> pendingInstabilityPieces = new();
    private int nextCenterSlot;
    private Piece selectedPiece;
    private bool executingDistributionMove;

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

    private void Update()
    {
        if (State != GameState.SelectOrSpawn) return;
        if (CurrentMoveMode != TurnMoveMode.Distribution) return;
        if (executingDistributionMove || movedDistributionPieces.Count > 0) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            CancelDistributionSelection();
    }

    public void StartGame()
    {
        if (State == GameState.GameOver)
            ResetRuntimeState();

        if (State != GameState.Initializing) return;

        ResetTurnState();
        CreatePiece();

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

        if (CurrentMoveMode == TurnMoveMode.Distribution)
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
        EndTurn((Piece)null);
    }

    public void OnPieceClicked(Piece piece)
    {
        OnPieceClicked(piece, additiveSelection: false);
    }

    public void OnPieceClicked(Piece piece, bool additiveSelection)
    {
        if (State != GameState.SelectOrSpawn) return;
        if (piece.IsFinished) return;

        if (additiveSelection)
        {
            ToggleDistributionPiece(piece);
            return;
        }

        if (CurrentMoveMode == TurnMoveMode.Distribution && distributionPieces.Count >= 2)
        {
            SelectDistributionMoveOptions(piece);
            return;
        }

        if (CurrentMoveMode == TurnMoveMode.Distribution && distributionPieces.Count < 2)
            ResetDistributionSelection();

        int maxSteps = MaxSelectableSteps(piece, CurrentDiceValue);

        if (maxSteps <= 0)
        {
            piece.Visuals?.SetState(PieceVisualState.Penalty);
            Debug.Log($"[Game] LOSS — {piece.name} range max = {maxSteps}");
            HandleLoss();
            return;
        }

        List<MoveOption> options = moveRangePlanner.BuildOptions(
            piece,
            maxSteps,
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

        Debug.Log($"[Game] Selected {piece.name}, legal options: {selectedMoveOptions.Count}");
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

            if (CurrentMoveMode == TurnMoveMode.Distribution)
                ExecuteDistributionMove(option);
            else
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

    private void ExecuteDistributionMove(MoveOption option)
    {
        if (!distributionPieces.Contains(option.piece) || movedDistributionPieces.Contains(option.piece))
            return;

        ClearMoveSelection();

        executingDistributionMove = true;
        RemainingDistributionBudget -= option.steps;
        State = GameState.Moving;

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

        RefreshDistributionHighlights();
    }

    // ── Уведомление от Piece ──────────────────────────────────

    public void NotifyMoveFinished(Piece piece, Vector2Int _fromPos)
    {
        if (executingDistributionMove)
        {
            executingDistributionMove = false;
            movedDistributionPieces.Add(piece);
            movedDistributionOrder.Add(piece);

            if (movedDistributionPieces.Count < distributionPieces.Count)
            {
                State = GameState.SelectOrSpawn;
                ClearMoveSelection();
                Debug.Log($"[Game] Distribution remaining budget: {RemainingDistributionBudget}");

                if (!HasAnyPendingDistributionMove())
                {
                    Debug.Log("[Game] LOSS — distribution cannot be completed");
                    HandleLoss();
                }

                return;
            }

            QueueDistributionInstability();
            EndTurn(new HashSet<Piece>(movedDistributionPieces));
            return;
        }

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
        if (movedPiece == null)
        {
            EndTurn((IReadOnlyCollection<Piece>)null);
            return;
        }

        var movedPieces = new HashSet<Piece>();
        movedPieces.Add(movedPiece);

        EndTurn(movedPieces);
    }

    private void EndTurn(IReadOnlyCollection<Piece> movedPieces)
    {
        ResetDistributionSelection();
        ClearMoveSelection();
        CurrentDiceValue = 0;
        CurrentDistributionBudget = 0;
        RemainingDistributionBudget = 0;

        if (movedPieces != null)
            inactivitySystem.OnTurnEnd(movedPieces, pieces);

        if (pendingInstabilityPieces.Count > 0)
        {
            inactivitySystem.MarkUnstable(pendingInstabilityPieces);
            pendingInstabilityPieces.Clear();
        }

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
            if (moveRangePlanner.HasAnyOption(p, MaxSelectableSteps(p, dice), 0, board))
                return true;
        }

        return false;
    }

    private int MaxSelectableSteps(Piece piece, int dice)
        => moveRangePlanner.GetSingleMoveMaxSteps(dice, inactivitySystem.GetPenalty(piece), ActivePieceCount());

    private void ToggleDistributionPiece(Piece piece)
    {
        ClearMoveSelection();

        if (movedDistributionPieces.Count > 0)
        {
            Debug.Log("[Game] Distribution selection is locked after the first distributed move");
            return;
        }

        if (distributionPieces.Contains(piece))
            distributionPieces.Remove(piece);
        else
            distributionPieces.Add(piece);

        if (!moveRangePlanner.IsValidDistributionPieceCount(distributionPieces.Count))
        {
            CurrentMoveMode = distributionPieces.Count == 0 ? TurnMoveMode.Single : TurnMoveMode.Distribution;
            CurrentDistributionBudget = 0;
            RemainingDistributionBudget = 0;
            RefreshDistributionHighlights();
            Debug.Log($"[Game] Select {MoveRangePlanner.MinDistributionPieces}-{MoveRangePlanner.MaxDistributionPieces} pieces for distribution");
            return;
        }

        CurrentMoveMode = TurnMoveMode.Distribution;
        CurrentDistributionBudget = moveRangePlanner.GetDistributionMaxSteps(CurrentDiceValue, distributionPieces, inactivitySystem);
        RemainingDistributionBudget = CurrentDistributionBudget;

        if (CurrentDistributionBudget <= 0)
        {
            Debug.Log($"[Game] LOSS — distribution range max = {CurrentDistributionBudget}");
            HandleLoss();
            return;
        }

        if (CurrentDistributionBudget < distributionPieces.Count)
        {
            Debug.Log($"[Game] Distribution needs at least {distributionPieces.Count} steps, budget is {CurrentDistributionBudget}");
            return;
        }

        RefreshDistributionHighlights();
        Debug.Log($"[Game] Distribution selected: {distributionPieces.Count} pieces, range [1..{CurrentDistributionBudget}]");
    }

    private void SelectDistributionMoveOptions(Piece piece)
    {
        if (!distributionPieces.Contains(piece) || movedDistributionPieces.Contains(piece))
        {
            Debug.Log("[Game] Select one of the pending distribution pieces");
            return;
        }

        int pendingAfterThis = distributionPieces.Count - movedDistributionPieces.Count - 1;
        int maxSteps = RemainingDistributionBudget - pendingAfterThis;

        if (maxSteps <= 0)
        {
            Debug.Log("[Game] Not enough distribution budget for this piece");
            return;
        }

        List<MoveOption> options = moveRangePlanner.BuildOptions(piece, maxSteps, board);

        if (options.Count == 0)
        {
            Debug.Log($"[Game] {piece.name} has no legal distributed move in range [1..{maxSteps}]");
            return;
        }

        SelectPieceMoveOptions(piece, options);
        Debug.Log($"[Game] Distribution moving {piece.name}, allowed [1..{maxSteps}], remaining {RemainingDistributionBudget}");
    }

    private void RefreshDistributionHighlights()
    {
        if (CurrentMoveMode != TurnMoveMode.Distribution)
            return;

        foreach (var piece in distributionPieces)
        {
            if (piece == null || piece.IsFinished) continue;
            if (movedDistributionPieces.Contains(piece)) continue;

            TileInstance tile = board.GetTile(board.GetPiecePosition(piece));
            if (tile == null || highlightedTiles.Contains(tile)) continue;

            tile.SetHighlight(TileHighlight.Effect);
            highlightedTiles.Add(tile);
        }
    }

    private void ResetDistributionSelection()
    {
        CurrentMoveMode = TurnMoveMode.Single;
        distributionPieces.Clear();
        movedDistributionOrder.Clear();
        movedDistributionPieces.Clear();
        executingDistributionMove = false;
    }

    private void QueueDistributionInstability()
    {
        pendingInstabilityPieces.Clear();

        if (distributionPieces.Count == 2)
        {
            pendingInstabilityPieces.AddRange(distributionPieces);
            return;
        }

        if (distributionPieces.Count == 3)
        {
            for (int i = 0; i < movedDistributionOrder.Count && pendingInstabilityPieces.Count < 2; i++)
                pendingInstabilityPieces.Add(movedDistributionOrder[i]);

            Debug.Log("[Game] Three-piece distribution: first two moved pieces become unstable");
        }
    }

    public void CancelDistributionSelection()
    {
        if (CurrentMoveMode != TurnMoveMode.Distribution) return;
        if (executingDistributionMove || movedDistributionPieces.Count > 0) return;

        ClearMoveSelection();
        ResetDistributionSelection();
        ClearMoveSelection();
        Debug.Log("[Game] Distribution cancelled");
    }

    private bool HasAnyPendingDistributionMove()
    {
        int pendingCount = distributionPieces.Count - movedDistributionPieces.Count;

        foreach (var piece in distributionPieces)
        {
            if (piece == null || piece.IsFinished) continue;
            if (movedDistributionPieces.Contains(piece)) continue;

            int pendingAfterThis = pendingCount - 1;
            int maxSteps = RemainingDistributionBudget - pendingAfterThis;
            if (moveRangePlanner.BuildOptions(piece, maxSteps, board).Count > 0)
                return true;
        }

        return false;
    }

    private void ResetRuntimeState()
    {
        ClearMoveSelection();
        ResetDistributionSelection();

        foreach (var piece in pieces)
        {
            if (piece == null) continue;

            Vector2Int pos = board.GetPiecePosition(piece);
            TileInstance tile = board.GetTile(pos);
            if (tile != null && tile.OccupiedPiece == piece)
                tile.ClearPiece();

            Destroy(piece.gameObject);
        }

        pieces.Clear();
        inactivitySystem.Clear();
        victoryCondition.Init(board);
        nextCenterSlot = board.CenterPath.Count - 1;
        ResetTurnState();

        State = GameState.Initializing;
    }

    private void ResetTurnState()
    {
        CurrentMoveMode = TurnMoveMode.Single;
        CurrentDiceValue = 0;
        CurrentDistributionBudget = 0;
        RemainingDistributionBudget = 0;
        selectedMoveOptions.Clear();
        highlightedTiles.Clear();
        selectedPiece = null;
        distributionPieces.Clear();
        movedDistributionOrder.Clear();
        movedDistributionPieces.Clear();
        pendingInstabilityPieces.Clear();
        executingDistributionMove = false;
    }

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
