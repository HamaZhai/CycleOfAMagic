using System.Collections.Generic;
using UnityEngine;

public class BoardGenerator : MonoBehaviour, IBoardOccupancy
{
    [Header("Config")]
    public float tileSize = 1f;
    public int startIndex = 0;

    [Range(0.5f, 1f)]
    public float boardPadding = 0.9f;

    [Header("References")]
    public BoardManager boardManager;
    public GameController gameController;

    public IReadOnlyList<Vector2Int> PerimeterPath => perimeterPath;
    public IReadOnlyList<Vector2Int> CenterPath => centerPath;

    private List<Vector2Int> perimeterPath = new();
    private List<Vector2Int> centerPath = new();
    private Dictionary<Vector2Int, TileInstance> tilemap = new();

    private MovementRules movementRules;

    public void Init()
    {
        FitBoardToCamera();
        GeneratePaths(boardManager.boardSize);
        SpawnBoard();

        movementRules = new MovementRules(
            new List<Vector2Int>(perimeterPath),
            new List<Vector2Int>(centerPath),
            startIndex,
            this
        );
    }

    // ---- IBoardOccupancy ----

    public bool IsTileOccupied(Vector2Int pos)
        => tilemap.TryGetValue(pos, out var tile) && tile.IsOccupied();

    public Piece GetOccupant(Vector2Int pos)
        => tilemap.TryGetValue(pos, out var tile) ? tile.OccupiedPiece : null;

    // ---- Публичный API для Piece ----

    public bool TryStep(ref MoveState state, Piece piece, bool apply)
        => movementRules.TryStep(ref state, piece, apply);

    public bool CanMove(Piece piece, int steps)
        => movementRules.CanMove(piece.GetMoveState(), piece, steps);

    public bool TryGetMoveDestination(Piece piece, int steps, out Vector2Int destination)
        => movementRules.TryGetDestination(piece.GetMoveState(), piece, steps, out destination);

    public Vector2Int GetPiecePosition(Piece piece)
        => movementRules.GetCurrentPosition(piece.GetMoveState());

    public TileInstance GetTile(Vector2Int pos)
    {
        if (!tilemap.TryGetValue(pos, out var tile))
        {
            Debug.LogError($"[Board] Tile not found at {pos}");
            return null;
        }
        return tile;
    }

    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        Vector3 offset = GetBoardOffset();
        return new Vector3(gridPos.x * tileSize, gridPos.y * tileSize, 0) - offset;
    }

    // ---- Генерация путей ----

    private void GeneratePaths(int boardSize)
    {
        perimeterPath.Clear();
        centerPath.Clear();

        int max = boardSize - 1;

        for (int x = 0; x <= max; x++) perimeterPath.Add(new Vector2Int(x, 0));
        for (int y = 1; y <= max; y++) perimeterPath.Add(new Vector2Int(max, y));
        for (int x = max - 1; x >= 0; x--) perimeterPath.Add(new Vector2Int(x, max));
        for (int y = max - 1; y > 0; y--) perimeterPath.Add(new Vector2Int(0, y));

        int center = (boardSize - 1) / 2;
        Vector2Int start = perimeterPath[startIndex];

        for (int i = 1; i <= center; i++)
            centerPath.Add(new Vector2Int(start.x + i, start.y + i));
    }

    // ---- Спавн ----

    private void SpawnBoard()
    {
        var decks = new Dictionary<TileZone, List<TileData>>
        {
            { TileZone.Start,    Shuffle(boardManager.startDeck) },
            { TileZone.Progress, Shuffle(boardManager.progressDeck) },
            { TileZone.Triumph,  Shuffle(boardManager.triumphDeck) },
            { TileZone.Finish,   Shuffle(boardManager.finishDeck) }
        };

        var deckIndex = new Dictionary<TileZone, int>
        {
            { TileZone.Start, 0 }, { TileZone.Progress, 0 },
            { TileZone.Triumph, 0 }, { TileZone.Finish, 0 }
        };

        for (int i = 0; i < perimeterPath.Count; i++)
            SpawnPerimeterTile(i, decks, deckIndex);

        foreach (var pos in centerPath)
            SpawnCenterTile(pos);
    }

    private void SpawnPerimeterTile(int index, Dictionary<TileZone, List<TileData>> decks, Dictionary<TileZone, int> deckIndex)
    {
        Vector2Int pos = perimeterPath[index];
        TileZone zone = GetZone(index);

        GameObject prefab = zone == TileZone.Corner
            ? boardManager.cornerTilePrefab
            : boardManager.defaultTilePrefab;

        TileInstance instance = SpawnTile(prefab, pos);

        if (zone == TileZone.Corner)
        {
            instance.Initialize(new TileData("Corner", TileZone.Corner));
            instance.SetHighlight(TileHighlight.Normal);

            if (index == startIndex)
            {
                instance.isStartCorner = true;
                instance.GetComponent<SpriteRenderer>().color = Color.green;
            }
            else
            {
                instance.GetComponent<SpriteRenderer>().color = Color.red;
            }
        }
        else
        {
            var deck = decks[zone];
            instance.Initialize(deck[deckIndex[zone]++]);
        }
    }

    private void SpawnCenterTile(Vector2Int pos)
    {
        TileInstance instance = SpawnTile(boardManager.defaultTilePrefab, pos);
        instance.Initialize(new TileData("Center", TileZone.Finish));
        instance.GetComponent<SpriteRenderer>().color = new Color(1f, 0.84f, 0f);
    }

    private TileInstance SpawnTile(GameObject prefab, Vector2Int pos)
    {
        Vector3 world = GridToWorld(pos);
        GameObject go = Instantiate(prefab, world, Quaternion.identity, transform);
        go.transform.localScale = Vector3.one * tileSize;

        TileInstance instance = go.GetComponent<TileInstance>();
        instance.Init(gameController);
        tilemap[pos] = instance;

        return instance;
    }

    // ---- Хелперы ----

    private TileZone GetZone(int index)
    {
        int b = boardManager.boardSize - 1;
        int c1 = 0, c2 = b, c3 = 2 * b, c4 = 3 * b;

        if (index == c1 || index == c2 || index == c3 || index == c4) return TileZone.Corner;
        if (index > c1 && index < c2) return TileZone.Start;
        if (index > c2 && index < c3) return TileZone.Progress;
        if (index > c3 && index < c4) return TileZone.Triumph;

        return TileZone.Finish;
    }

    private Vector3 GetBoardOffset()
    {
        float worldSize = (boardManager.boardSize - 1) * tileSize;
        return new Vector3(worldSize / 2f, worldSize / 2f, 0);
    }

    private void FitBoardToCamera()
    {
        Camera cam = Camera.main;
        float h = cam.orthographicSize * 2f;
        float w = h * cam.aspect;

        float usableW = w * boardPadding;
        float usableH = h * boardPadding;

        float size = boardManager.boardSize;
        tileSize = Mathf.Min(usableW / size, usableH / size);
    }

    private List<TileData> Shuffle(List<TileData> list)
    {
        var copy = new List<TileData>(list);
        for (int i = copy.Count - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);
            (copy[i], copy[rnd]) = (copy[rnd], copy[i]);
        }
        return copy;
    }

    // ---- Gizmos ----

    private void OnDrawGizmos()
    {
        if (perimeterPath == null || perimeterPath.Count == 0) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < perimeterPath.Count; i++)
        {
            Vector3 pos = GridToWorld(perimeterPath[i]);
            Gizmos.DrawSphere(pos, 0.1f);
            Gizmos.DrawLine(pos, GridToWorld(perimeterPath[(i + 1) % perimeterPath.Count]));
        }

        Gizmos.color = Color.yellow;
        for (int i = 0; i < centerPath.Count; i++)
        {
            Vector3 pos = GridToWorld(centerPath[i]);
            Gizmos.DrawCube(pos, Vector3.one * 0.2f);
            if (i < centerPath.Count - 1)
                Gizmos.DrawLine(pos, GridToWorld(centerPath[i + 1]));
        }
    }
}
