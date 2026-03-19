#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class DeckBuilderWindow : EditorWindow
{
    private BoardManager boardManager;
    private int boardSize = 10;

    [MenuItem("Tools/Deck Builder")]
    public static void ShowWindow()
    {
        GetWindow<DeckBuilderWindow>("Deck Builder");
    }

    void OnGUI()
    {
        GUILayout.Label("Deck Builder", EditorStyles.boldLabel);

        boardManager = (BoardManager)EditorGUILayout.ObjectField("Board Manager", boardManager, typeof(BoardManager), false);
        boardSize = EditorGUILayout.IntField("Board Size", boardSize);

        if (boardManager == null)
        {
            EditorGUILayout.HelpBox("Assign a BoardManager ScriptableObject!", MessageType.Warning);
            return;
        }

        if (GUILayout.Button("Generate Decks"))
        {
            GenerateDecks();
            EditorUtility.SetDirty(boardManager);
            AssetDatabase.SaveAssets();
        }

        if (boardManager.startDeck.Count > 0)
        {
            GUILayout.Space(10);
            GUILayout.Label("Preview Start Deck", EditorStyles.boldLabel);
            foreach (var tile in boardManager.startDeck)
            {
                EditorGUILayout.BeginHorizontal();
                tile.tileName = EditorGUILayout.TextField("Name", tile.tileName);
                tile.effect = EditorGUILayout.TextField("Effect", tile.effect);
                EditorGUILayout.EndHorizontal();
            }
        }
    }

    void GenerateDecks()
    {
        int tilesPerZone = boardSize - 2;

        boardManager.startDeck.Clear();
        boardManager.progressDeck.Clear();
        boardManager.triumphDeck.Clear();
        boardManager.finishDeck.Clear();

        for (int i = 0; i < tilesPerZone; i++)
        {
            boardManager.startDeck.Add(new TileData($"Start_{i}", TileZone.Start));
            boardManager.progressDeck.Add(new TileData($"Progress_{i}", TileZone.Progress));
            boardManager.triumphDeck.Add(new TileData($"Triumph_{i}", TileZone.Triumph));
            boardManager.finishDeck.Add(new TileData($"Finish_{i}", TileZone.Finish));
        }

      

        Debug.Log("Decks generated successfully!");
    }
}
#endif