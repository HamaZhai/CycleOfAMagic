using UnityEngine;
using UnityEngine.EventSystems;


public enum TileZone
{
    Corner,
    Start,
    Progress,
    Triumph,
    Finish
}

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class TileInstance : MonoBehaviour
{
    public TileData data;
    private GameController game;

    public bool isStartCorner;
    public Piece OccupiedPiece { get; private set; }

    [SerializeField] private SpriteRenderer sr;
    private Color basecolor;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        basecolor = sr.color;
    }

    public bool IsOccupied()
    {
        return OccupiedPiece != null;
    }

    public void SetPiece(Piece piece)
    {
        OccupiedPiece = piece;
        sr.color = Color.magenta;
    }

    public void ClearPiece()
    {
        OccupiedPiece = null;
        sr.color = basecolor;
    }

    public void Init(GameController controller)
    {
        game = controller;

        if (game == null)
        {
            Debug.LogError("GameContoller Null");
        }
    }

    public void Initialize(TileData tileData)
    {
        data = new TileData(tileData.tileName, tileData.zone);
        data.effect = tileData.effect;
        gameObject.name = $"Tile_{data.tileName}";
    }

    public void HandleClick()
    {
        if (isStartCorner)
        {
            game.OnStartTileClicked();
            Debug.Log("Clicked: " + gameObject.name);
        }
    }
}