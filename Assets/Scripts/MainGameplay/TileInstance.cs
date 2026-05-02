using UnityEngine;

public enum TileZone
{
    Corner,
    Start,
    Progress,
    Triumph,
    Finish
}

public enum TileHighlight
{
    Normal,
    Occupied,
    Available,
    Effect,
    Selected
}

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class TileInstance : MonoBehaviour
{
    public TileData data;
    public bool isStartCorner;

    public Piece OccupiedPiece { get; private set; }

    [SerializeField] private SpriteRenderer sr;
    private GameController game;
    private Color baseColor;

    public TileHighlight CurrentHighlight { get; private set; } = TileHighlight.Normal;

    [Header("Highlight Colors")]
    [SerializeField] private Color occupiedColor = Color.magenta;
    [SerializeField] private Color availableColor = Color.cyan;
    [SerializeField] private Color effectColor = Color.yellow;
    [SerializeField] private Color selectedColor = Color.white;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;
    }

    public void Init(GameController controller)
    {
        game = controller;
    }

    public void Initialize(TileData tileData)
    {
        data = new TileData(tileData.tileName, tileData.zone);
        data.effect = tileData.effect;
        gameObject.name = $"Tile_{data.tileName}";
    }

    public bool IsOccupied() => OccupiedPiece != null;

    // Возвращает базовый цвет тайла — нужен TrailSystem для восстановления цвета
    public Color GetBaseColor() => baseColor;

    public void SetPiece(Piece piece)
    {
        if (OccupiedPiece != null)
        {
            Debug.LogError($"[Tile {name}] Already occupied by {OccupiedPiece.name}, tried to set {piece.name}");
            return;
        }

        OccupiedPiece = piece;
        SetHighlight(TileHighlight.Occupied);
    }

    public void ClearPiece()
    {
        OccupiedPiece = null;
        SetHighlight(TileHighlight.Normal);
    }

    public void SetHighlight(TileHighlight highlight)
    {
        CurrentHighlight = highlight;

        sr.color = highlight switch
        {
            TileHighlight.Normal    => baseColor,
            TileHighlight.Occupied  => occupiedColor,
            TileHighlight.Available => availableColor,
            TileHighlight.Effect    => effectColor,
            TileHighlight.Selected  => selectedColor,
            _                       => baseColor
        };
    }

    public void HandleClick()
    {
        game.OnTileClicked(this);
    }
}
