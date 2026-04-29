using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

// Клетка, на которой закончился ход фигуры, блокируется на 2 следующих хода.
// Визуально окрашивает тайл в trailColor и плавно убирает цвет когда блок снимается.
public class TrailSystem : MonoBehaviour
{
    [Header("Trail Visual")]
    [SerializeField] private Color trailColor = new Color(0.8f, 0.3f, 1f, 1f); // яркий фиолетовый
    [SerializeField] private float fadeDuration = 0.35f;

    // pos → сколько ходов блокировка ещё активна
    private readonly Dictionary<Vector2Int, int> blockedTiles = new();

    // Храним ссылки на тайлы чтобы красить / снимать цвет
    private readonly Dictionary<Vector2Int, TileInstance> trackedTiles = new();

    // Вызывается из GameController после завершения хода фигуры
    public void RegisterTrail(Vector2Int pos, TileInstance tile)
    {
        blockedTiles[pos] = 2; // блок на 2 хода
        trackedTiles[pos] = tile;

        ApplyTrailColor(tile, trailColor);

        Debug.Log($"[Trail] Tile {pos} blocked for 2 turns");
    }

    // Вызывается из GameController в начале каждого хода (до броска)
    public void TickDown()
    {
        // Копируем ключи — нельзя менять словарь во время итерации
        var keys = new List<Vector2Int>(blockedTiles.Keys);

        foreach (var key in keys)
        {
            blockedTiles[key]--;

            if (blockedTiles[key] <= 0)
            {
                // Снимаем блок и возвращаем базовый цвет
                blockedTiles.Remove(key);

                if (trackedTiles.TryGetValue(key, out var tile) && tile != null)
                {
                    var sr = tile.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.DOKill();
                        sr.DOColor(tile.GetBaseColor(), fadeDuration)
                          .OnComplete(() => tile.SetHighlight(TileHighlight.Normal));
                    }
                    trackedTiles.Remove(key);
                }

                Debug.Log($"[Trail] Tile {key} unblocked");
            }
            else
            {
                // Второй ход — тусклый цвет, визуально показываем что скоро снимется
                if (trackedTiles.TryGetValue(key, out var tile) && tile != null)
                    ApplyTrailColor(tile, trailColor);
            }
        }
    }

    public bool IsTrailBlocked(Vector2Int pos) => blockedTiles.ContainsKey(pos);

    // ── helpers ──────────────────────────────────────────────

    private void ApplyTrailColor(TileInstance tile, Color color)
    {
        if (tile == null) return;
        var sr = tile.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        sr.DOKill();
        sr.DOColor(color, fadeDuration);
    }

    private void OnDestroy()
    {
        DOTween.KillAll();
    }
}