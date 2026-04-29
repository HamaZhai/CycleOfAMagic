using UnityEngine;
using DG.Tweening;

public enum PieceVisualState
{
    Idle,
    Priority,       // обязаны двигать эту фигуру
    PriorityUrgent, // внимание! — резкий мигающий акцент
    Penalty,        // штраф за бездействие
    Finished        // финишировала
}

public class PieceVisuals : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color idleColor = Color.white;
    [SerializeField] private Color priorityColor = new Color(0.2f, 1f, 0.2f, 1f);
    [SerializeField] private Color penaltyColor = new Color(1f, 0.5f, 0f, 1f);
    [SerializeField] private Color finishedColor = new Color(0.5f, 0.5f, 1f, 1f);   

    [Header("Timing")]
    [SerializeField] private float pulseDuration = 0.5f;
    [SerializeField] private float urgentPulseDuration = 0.1f;
    [SerializeField] private float penaltyPulseDuration = 0.18f;
    [SerializeField] private int urgentPulseCount = 6;
    [SerializeField] private int penaltyPulseCount = 4;

    private SpriteRenderer sr;
    private PieceVisualState currentState = PieceVisualState.Idle;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        idleColor = sr.color; // запоминаем исходный цвет из инспектора
    }

    public void SetState(PieceVisualState state)
    {
        if (currentState == state) return;
        currentState = state;

        sr.DOKill();

        switch (state)
        {
            case PieceVisualState.Idle:
                sr.DOColor(idleColor, pulseDuration * 0.5f);
                break;

            case PieceVisualState.Priority:
                sr.DOColor(priorityColor, pulseDuration)
                  .SetLoops(-1, LoopType.Yoyo)
                  .SetEase(Ease.InOutSine);
                break;

            case PieceVisualState.PriorityUrgent:
                sr.DOColor(priorityColor, urgentPulseDuration)
                  .SetLoops(urgentPulseCount, LoopType.Yoyo)
                  .SetEase(Ease.InOutSine)
                  .OnComplete(() => SetState(PieceVisualState.Priority));
                break;

            case PieceVisualState.Penalty:
                sr.DOColor(penaltyColor, penaltyPulseDuration)
                    .SetLoops(penaltyPulseCount * 2, LoopType.Yoyo)
                    .SetEase(Ease.InOutFlash)
                    .OnComplete(() =>
                    {
                        currentState = PieceVisualState.Idle;
                        sr.DOColor(idleColor, penaltyPulseDuration * 0.5f);
                    });
                break;

            case PieceVisualState.Finished:
                sr.DOColor(finishedColor, pulseDuration);
                break;
        }
    }

    public PieceVisualState CurrentState => currentState;

    private void OnDestroy()
    {
        sr?.DOKill();
    }
}