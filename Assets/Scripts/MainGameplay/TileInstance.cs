using UnityEngine;

public enum TileZone
{
    Corner,
    Start,
    Progress,
    Triumph,
    Finish
}

// Все визуальные состояния плитки через enum.
// Когда добавишь эффекты — просто добавляешь новый вариант сюда.
// Никакой магии с цветами разбросанной по коду.
public enum TileHighlight
{
    Normal,         // базовое состояние
    Occupied,       // на клетке стоит фигура
    Available,      // можно ходить сюда (для подсветки допустимых ходов)
    Effect,         // клетка имеет активный эффект (плитка сработала)
    Selected        // выбрана игроком
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

    // Текущий highlight — публичный для чтения, приватный для записи.
    // Другие системы видят состояние, но не пишут напрямую.
    public TileHighlight CurrentHighlight { get; private set; } = TileHighlight.Normal;

    // Цвета вынесены в инспектор — дизайнер меняет без программиста.
    // Сериализованы, поэтому не сбрасываются при компиляции.
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

    // SetPiece и ClearPiece теперь только меняют данные.
    // Визуал — через SetHighlight. Разделение ответственности.
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

    // Единственный метод для изменения визуала.
    // Добавить новый эффект = добавить case сюда.
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
        if (isStartCorner)
        {
            game.OnStartTileClicked();
        }
    }
}