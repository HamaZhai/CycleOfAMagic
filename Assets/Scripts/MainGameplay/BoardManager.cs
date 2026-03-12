using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BoardManager", menuName = "Board/Manager")]
public class BoardManager : ScriptableObject
{
    public int boardSize = 10;

    public TileData cornerTile;          // данные угловой плитки
    public GameObject defaultTilePrefab; // визуальный префаб для всех плиток

    public List<TileData> startDeck = new List<TileData>();
    public List<TileData> progressDeck = new List<TileData>();
    public List<TileData> triumphDeck = new List<TileData>();
    public List<TileData> finishDeck = new List<TileData>();

    // Генерация стандартной колоды (если нужно)
    public void GenerateDecks()
    {
        int tilesPerZone = boardSize - 2;

        startDeck.Clear();
        progressDeck.Clear();
        triumphDeck.Clear();
        finishDeck.Clear();

        for (int i = 0; i < tilesPerZone; i++)
        {
            startDeck.Add(new TileData($"Start_{i}", TileZone.Start));
            progressDeck.Add(new TileData($"Progress_{i}", TileZone.Progress));
            triumphDeck.Add(new TileData($"Triumph_{i}", TileZone.Triumph));
            finishDeck.Add(new TileData($"Finish_{i}", TileZone.Finish));
        }
    }

    // Получение колоды по зоне
    public List<TileData> GetDeck(TileZone zone)
    {
        switch (zone)
        {
            case TileZone.Start: return startDeck;
            case TileZone.Progress: return progressDeck;
            case TileZone.Triumph: return triumphDeck;
            case TileZone.Finish: return finishDeck;
            default: return null;
        }
    }
}
