using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BoardManager", menuName = "Board/Manager")]
public class BoardManager : ScriptableObject
{
    public int boardSize = 10;

    // Два отдельных префаба
    public GameObject defaultTilePrefab;
    public GameObject cornerTilePrefab;

    public List<TileData> startDeck = new List<TileData>();
    public List<TileData> progressDeck = new List<TileData>();
    public List<TileData> triumphDeck = new List<TileData>();
    public List<TileData> finishDeck = new List<TileData>();

    // Генерация стандартной колоды
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

    public List<TileData> GetDeck(TileZone zone)
    {
        return zone switch
        {
            TileZone.Start => startDeck,
            TileZone.Progress => progressDeck,
            TileZone.Triumph => triumphDeck,
            TileZone.Finish => finishDeck,
            _ => null
        };
    }
}