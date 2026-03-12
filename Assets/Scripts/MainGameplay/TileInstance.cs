using UnityEngine;

public enum TileZone
{
    Corner,
    Start,
    Progress,
    Triumph,
    Finish
}

[RequireComponent(typeof(SpriteRenderer))]
public class TileInstance : MonoBehaviour
{
    public TileData data;

    // Инициализация плитки из колоды
    public void Initialize(TileData tileData)
    {
        // Копируем данные, чтобы доска имела уникальные экземпляры
        data = new TileData(tileData.tileName, tileData.zone);
        data.effect = tileData.effect;
        gameObject.name = $"Tile_{data.tileName}";
    }

    public void SetEffect(string effect)
    {
        data.effect = effect;
    }
}
