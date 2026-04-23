using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEngine;

public class BoardGenerator : MonoBehaviour
{
    public float tileSize = 1f;
    public int startIndex = 0;

    public BoardManager boardManager;
    public GameController gameController;

    public List<Vector2Int> PerimeterPath => perimeterPath;
    public List<Vector2Int> CenterPath => centerPath;

    private List<Vector2Int> perimeterPath = new List<Vector2Int>();
    private List<Vector2Int> centerPath = new List<Vector2Int>();

    private List<TileInstance> spawnedTiles = new List<TileInstance>();

    private Dictionary<Vector2Int, TileInstance> tilemap = new();

    [Range(0.5f, 1f)]
    public float boardPadding = 0.9f;

    public void Init()
    {
        FitBoardToCamera();
        GeneratePaths(boardManager.boardSize);
        SpawnBoard();
    }

    // ---------------- PATH GENERATION ----------------

    void GeneratePaths(int boardSize)
    {
        perimeterPath.Clear();
        centerPath.Clear();

        int max = boardSize - 1;

        // --- PERIMETER ---
        for (int x = 0; x <= max; x++) perimeterPath.Add(new Vector2Int(x, 0));
        for (int y = 1; y <= max; y++) perimeterPath.Add(new Vector2Int(max, y));
        for (int x = max - 1; x >= 0; x--) perimeterPath.Add(new Vector2Int(x, max));
        for (int y = max - 1; y > 0; y--) perimeterPath.Add(new Vector2Int(0, y));

        // --- CENTER ---
        int center = (boardSize - 1) / 2;
        Vector2Int start = perimeterPath[startIndex];

        for (int i = 1; i <= center; i++)
        {
            centerPath.Add(new Vector2Int(start.x + i, start.y + i));
        }
    }

    // ---------------- SPAWN ----------------

    void SpawnBoard()
    {
        spawnedTiles.Clear();

        var decksShuffled = new Dictionary<TileZone, List<TileData>>()
        {
            { TileZone.Start, Shuffle(boardManager.startDeck) },
            { TileZone.Progress, Shuffle(boardManager.progressDeck) },
            { TileZone.Triumph, Shuffle(boardManager.triumphDeck) },
            { TileZone.Finish, Shuffle(boardManager.finishDeck) }
        };

        var zoneIndex = new Dictionary<TileZone, int>()
        {
            { TileZone.Start, 0 },
            { TileZone.Progress, 0 },
            { TileZone.Triumph, 0 },
            { TileZone.Finish, 0 }
        };

        // --- PERIMETER ---
        for (int i = 0; i < perimeterPath.Count; i++)
        {
            Vector2Int pos = perimeterPath[i];
            Vector3 world = GridToWorld(pos);

            TileZone zone = GetZone(i);

            GameObject prefab = zone == TileZone.Corner
                ? boardManager.cornerTilePrefab
                : boardManager.defaultTilePrefab;

            GameObject tileGO = Instantiate(prefab, world, Quaternion.identity, transform);
            tileGO.transform.localScale = Vector3.one * tileSize;

            TileInstance instance = tileGO.GetComponent<TileInstance>();

            // --- CORNERS ---
            if (zone == TileZone.Corner)
            {
                instance.Initialize(new TileData("Corner", TileZone.Corner));
                tileGO.GetComponent<SpriteRenderer>().color = Color.red;

                if (i == startIndex)
                {
                    instance.isStartCorner = true;
                    tileGO.GetComponent<SpriteRenderer>().color = Color.green;
                }
            }
            else
            {
                var deck = decksShuffled[zone];
                int idx = zoneIndex[zone];

                instance.Initialize(deck[idx]);
                zoneIndex[zone]++;
            }

            instance.Init(gameController);
            spawnedTiles.Add(instance);
            tilemap[pos] = instance;
        }

        // --- CENTER ---
        foreach (var pos in centerPath)
        {
            Vector3 world = GridToWorld(pos);

            GameObject tileGO = Instantiate(
                boardManager.defaultTilePrefab,
                world,
                Quaternion.identity,
                transform
            );

            tileGO.transform.localScale = Vector3.one * tileSize;

            TileInstance instance = tileGO.GetComponent<TileInstance>();

            instance.Initialize(new TileData("Center", TileZone.Finish));

            // золотой центр
            tileGO.GetComponent<SpriteRenderer>().color = new Color(1f, 0.84f, 0f);

            instance.Init(gameController);
            spawnedTiles.Add(instance);
            tilemap[pos] = instance;

        }
    }

    public TileInstance GetTile(Vector2Int pos)
    {
        return tilemap[pos];
    }

    public bool CanMove(Piece piece, int steps)
    {
        int index = piece.perimeterIndex;

        for (int i = 0; i < steps; i++)
        {
            index = (index + 1) % PerimeterPath.Count;

            var tile = GetTile(PerimeterPath[index]);

            if (tile.IsOccupied() && tile.OccupiedPiece != piece)
            {
                return false;
            }
        }

        return true;
    }

    // ---------------- ZONES ----------------

    TileZone GetZone(int index)
    {
        int b = boardManager.boardSize - 1;

        int c1 = 0;
        int c2 = b;
        int c3 = 2 * b;
        int c4 = 3 * b;

        if (index == c1 || index == c2 || index == c3 || index == c4)
            return TileZone.Corner;

        if (index > c1 && index < c2) return TileZone.Start;
        if (index > c2 && index < c3) return TileZone.Progress;
        if (index > c3 && index < c4) return TileZone.Triumph;

        return TileZone.Finish;
    }

    // ---------------- HELPERS ----------------

    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        Vector3 offset = GetBoardOffset();

        return new Vector3(
            gridPos.x * tileSize,
            gridPos.y * tileSize,
            0
        ) - offset;
    }

    Vector3 GetBoardOffset()
    {
        float boardWorldSize = (boardManager.boardSize - 1) * tileSize;
        return new Vector3(boardWorldSize / 2f, boardWorldSize / 2f, 0);
    }

    void FitBoardToCamera()
    {
        Camera cam = Camera.main;

        float screenHeight = cam.orthographicSize * 2f;
        float screenWidth = screenHeight * cam.aspect;

        float usableWidth = screenWidth * boardPadding;
        float usableHeight = screenHeight * boardPadding;

        float boardSize = boardManager.boardSize;

        float sizeByWidth = usableWidth / boardSize;
        float sizeByHeight = usableHeight / boardSize;

        tileSize = Mathf.Min(sizeByWidth, sizeByHeight);
    }

    List<TileData> Shuffle(List<TileData> list)
    {
        var copy = new List<TileData>(list);

        for (int i = copy.Count - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);
            (copy[i], copy[rnd]) = (copy[rnd], copy[i]);
        }

        return copy;
    }

    // ---------------- GIZMOS ----------------

    void OnDrawGizmos()
    {
        if (perimeterPath == null || perimeterPath.Count == 0) return;

        // Периметр
        Gizmos.color = Color.cyan;
        for (int i = 0; i < perimeterPath.Count; i++)
        {
            Vector3 pos = GridToWorld(perimeterPath[i]);
            Gizmos.DrawSphere(pos, 0.1f);

            Vector3 next = GridToWorld(perimeterPath[(i + 1) % perimeterPath.Count]);
            Gizmos.DrawLine(pos, next);
        }

        // Центр
        Gizmos.color = Color.yellow;
        for (int i = 0; i < centerPath.Count; i++)
        {
            Vector3 pos = GridToWorld(centerPath[i]);
            Gizmos.DrawCube(pos, Vector3.one * 0.2f);

            if (i < centerPath.Count - 1)
            {
                Vector3 next = GridToWorld(centerPath[i + 1]);
                Gizmos.DrawLine(pos, next);
            }
        }
    }
}