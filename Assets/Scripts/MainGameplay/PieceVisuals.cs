using UnityEngine;
using DG.Tweening;
using TMPro;

public enum PieceVisualState
{
    Idle,
    Penalty,   // активный штраф — оранжевый пульс при выборе
    Finished
}

// PieceVisuals v0.5
//
// Новое: штрафной счётчик над фигурой.
//   penaltyLabel (TextMeshPro) — дочерний объект на фигуре.
//   Показывает "-N" оранжевым когда idle > 0, скрывается при idle = 0.
//
// Убрано: Priority, PriorityUrgent состояния.
// PrioritySystem вырезана из диздока v0.5.
public class PieceVisuals : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color idleColor = Color.white;
    [SerializeField] private Color penaltyColor = new Color(1f, 0.5f, 0f, 1f);
    [SerializeField] private Color finishedColor = new Color(0.5f, 0.5f, 1f, 1f);

    [Header("Timing")]
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float penaltyPulseDuration = 0.18f;
    [SerializeField] private int penaltyPulseCount = 3;

    [Header("Penalty Label")]
    // Назначь TextMeshPro дочерний объект в префабе фигуры.
    // Он будет показывать "-1", "-2" и т.д. над спрайтом.
    [SerializeField] private TextMeshPro penaltyLabel;
    [SerializeField] private Vector3 labelOffset = new Vector3(0f, 0.55f, -0.1f);

    private SpriteRenderer sr;
    private PieceVisualState currentState = PieceVisualState.Idle;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        idleColor = sr.color;

        // Если label не назначен в инспекторе — создаём динамически
        if (penaltyLabel == null)
            penaltyLabel = CreatePenaltyLabel();

        penaltyLabel.gameObject.SetActive(false);
    }

    // ── Состояние фигуры ──────────────────────────────────────

    public void SetState(PieceVisualState state)
    {
        if (currentState == state) return;
        currentState = state;

        sr.DOKill();

        switch (state)
        {
            case PieceVisualState.Idle:
                sr.DOColor(idleColor, fadeDuration);
                break;

            case PieceVisualState.Penalty:
                // Короткий вспыхивающий акцент — показываем что штраф применён
                sr.DOColor(penaltyColor, penaltyPulseDuration)
                  .SetLoops(penaltyPulseCount * 2, LoopType.Yoyo)
                  .SetEase(Ease.InOutFlash)
                  .OnComplete(() =>
                  {
                      currentState = PieceVisualState.Idle;
                      sr.DOColor(idleColor, fadeDuration);
                  });
                break;

            case PieceVisualState.Finished:
                sr.DOColor(finishedColor, fadeDuration);
                penaltyLabel.gameObject.SetActive(false);
                break;
        }
    }

    // ── Штрафной счётчик ──────────────────────────────────────

    // Вызывается из GameController/InactivitySystem после каждого OnTurnEnd.
    // penalty = 0 → скрыть метку
    // penalty > 0 → показать "-N" над фигурой
    public void SetPenaltyDisplay(int penalty)
    {
        if (penalty <= 0)
        {
            penaltyLabel.gameObject.SetActive(false);
            return;
        }

        penaltyLabel.gameObject.SetActive(true);
        penaltyLabel.text = $"-{penalty}";

        // Мигнуть меткой когда штраф увеличивается
        penaltyLabel.transform.DOKill();
        penaltyLabel.transform.localScale = Vector3.one * 1.4f;
        penaltyLabel.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
    }

    public PieceVisualState CurrentState => currentState;

    // ── Вспомогательное ──────────────────────────────────────

    private TextMeshPro CreatePenaltyLabel()
    {
        var go = new GameObject("PenaltyLabel");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = labelOffset;

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.fontSize = 3f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = penaltyColor;

        // Сортировка поверх спрайта
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 10;

        return tmp;
    }

    private void OnDestroy()
    {
        sr?.DOKill();
        penaltyLabel?.transform.DOKill();
    }
}