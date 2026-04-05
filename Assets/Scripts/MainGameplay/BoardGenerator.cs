using System.Collections.Generic;
using UnityEngine;

public class BoardGenerator : MonoBehaviour
{
    public float tileSize = 1f;
    public BoardManager boardManager;
    public GameObject tilePrefab; // визуальный префаб плитки
    public GameController gameController;
    public List<Vector2Int> Path => path; // публично путь дл€ фигур
    private List<Vector2Int> path = new List<Vector2Int>();
    public List<Vector2Int> CenterPath => centerPath;
    private List<Vector2Int> centerPath = new List<Vector2Int>();

    private List<TileInstance> spawnedTiles = new List<TileInstance>();
    private List<TileInstance> spawnedCenterTiles = new List<TileInstance>();
    public Vector3 GetWorldPosition(int index)
    {
        if (index < 0 || index >= spawnedTiles.Count) return Vector3.zero;
        return spawnedTiles[index].transform.position;
    }


    public TileInstance GetTileInstance(int index)
    {
        if (index < 0 || index >= spawnedTiles.Count) return null;
        return spawnedTiles[index];
    }

    [Range(0.5f, 1f)]
    public float boardPadding = 0.9f;

    

    void Start()
    {
        if (boardManager == null)
        {
            Debug.LogError("BoardManager не назначен!");
            return;
        }
    }

    public void Init()
    {

        FitBoardToCamera();
        GeneratePath(boardManager.boardSize);
        GenerateCenterPath(boardManager.boardSize);
        SpawnBoard();

    }
    void GeneratePath(int boardSize)
    {
        int max = boardSize - 1;

        for (int x = 0; x <= max; x++) path.Add(new Vector2Int(x, 0));
        for (int y = 1; y <= max; y++) path.Add(new Vector2Int(max, y));
        for (int x = max - 1; x >= 0; x--) path.Add(new Vector2Int(x, max));
        for (int y = max - 1; y > 0; y--) path.Add(new Vector2Int(0, y));
    }

    void GenerateCenterPath(int boardSize)
    {
        int center = (boardSize - 1) / 2;

        for (int i = 0; i <= center; i++)
        {
            centerPath.Add(new Vector2Int(i, i));
        }
    }

    TileZone GetZone(int index)
    {
        int b = boardManager.boardSize - 1;
        int corner1 = 0;
        int corner2 = b;
        int corner3 = 2 * b;
        int corner4 = 3 * b;

        if (index == corner1 || index == corner2 || index == corner3 || index == corner4)
            return TileZone.Corner;
        if (index > corner1 && index < corner2) return TileZone.Start;
        if (index > corner2 && index < corner3) return TileZone.Progress;
        if (index > corner3 && index < corner4) return TileZone.Triumph;
        return TileZone.Finish;
    }

    void SpawnBoard()
    {
        Vector3 offset = GetBoardOffset();

        var decksShuffled = new Dictionary<TileZone, List<TileData>>()
    {
        { TileZone.Start, ShuffleList(new List<TileData>(boardManager.startDeck)) },
        { TileZone.Progress, ShuffleList(new List<TileData>(boardManager.progressDeck)) },
        { TileZone.Triumph, ShuffleList(new List<TileData>(boardManager.triumphDeck)) },
        { TileZone.Finish, ShuffleList(new List<TileData>(boardManager.finishDeck)) }
    };

        var zoneIndices = new Dictionary<TileZone, int>()
    {
        { TileZone.Start, 0 },
        { TileZone.Progress, 0 },
        { TileZone.Triumph, 0 },
        { TileZone.Finish, 0 }
    };

     
         foreach (var gridpos in centerPath)
        {
            Vector3 pos = new Vector3(gridpos.x * tileSize, gridpos.y * tileSize, 0) - GetBoardOffset();

            GameObject tileGO = Instantiate(boardManager.defaultTilePrefab, pos, Quaternion.identity, transform);

            tileGO.transform.localScale = Vector3.one * tileSize;

            TileInstance instance = tileGO.GetComponent<TileInstance>();

            TileData tileData = new TileData("Center", TileZone.Finish);
            instance.Initialize(tileData);
            spawnedCenterTiles.Add(instance);
        }

        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int gridPos = path[i];

            Vector3 pos = new Vector3(
                gridPos.x * tileSize,
                gridPos.y * tileSize,
                0
            ) - offset;

            TileZone zone = GetZone(i);

            GameObject prefabToUse = zone == TileZone.Corner
                ? boardManager.cornerTilePrefab
                : boardManager.defaultTilePrefab;

            GameObject tileGO = Instantiate(prefabToUse, pos, Quaternion.identity, transform);

            tileGO.transform.localScale = Vector3.one * tileSize;

            TileInstance instance = tileGO.GetComponent<TileInstance>();


            if (zone == TileZone.Corner)
            {
                TileData cornerData = new TileData($"Corner_{i}", TileZone.Corner);

                if (i == 0)
                {
                    instance.isStartCorner = true;
                    instance.GetComponent<SpriteRenderer>().color = Color.green;
                }

                instance.Initialize(cornerData);
            }
            else
            {
                List<TileData> deck = decksShuffled[zone];
                int idx = zoneIndices[zone];
                instance.Initialize(deck[idx]);
                zoneIndices[zone]++;
            }

            instance.Init(gameController);
            spawnedTiles.Add(instance);
            
        }

       
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

    List<TileData> ShuffleList(List<TileData> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);
            TileData temp = list[i];
            list[i] = list[rnd];
            list[rnd] = temp;
        }
        return list;
    }

    void OnDrawGizmos()
    {
        if (path == null || path.Count == 0) return;

        Vector3 offset = Application.isPlaying ? GetBoardOffset() : Vector3.zero;

        for (int i = 0; i < path.Count; i++)
        {
            float t = (float)i / path.Count;
            Gizmos.color = Color.Lerp(Color.green, Color.red, t);

            Vector3 pos = new Vector3(
                path[i].x * tileSize,
                path[i].y * tileSize,
                0
            ) - offset;

            Gizmos.DrawSphere(pos, 0.12f);

            if (i < path.Count - 1)
            {
                Vector3 nextPos = new Vector3(
                    path[i + 1].x * tileSize,
                    path[i + 1].y * tileSize,
                    0
                ) - offset;

                Gizmos.DrawLine(pos, nextPos);
            }
        }

        // Center path Ч синий
        Gizmos.color = Color.cyan;

        foreach (var p in centerPath)
        {
            Vector3 pos = new Vector3(
                p.x * tileSize,
                p.y * tileSize,
                0
            ) - offset;

            Gizmos.DrawCube(pos, Vector3.one * 0.2f);
        }
    }
}