using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DiceController : MonoBehaviour
{
    public Image diceImage;
    public Sprite[] diceFaces;

    public float rollDuration = 1f;
    public float rollSpeed = 0.05f;

    private bool isRolling = false;
    private int currentValue;

    public System.Action<int> OnDiceRolled;
    public System.Func<bool> CanRoll;

    public void OnDiceClicked()
    {
        if (isRolling) return;
        if (CanRoll != null && !CanRoll()) return;

        StartCoroutine(RollDice());
    }

    IEnumerator RollDice()
    {
        isRolling = true;

        // 1. ФИКСИРУЕМ результат заранее
        currentValue = Random.Range(1, 7);


        float t = 0f;

        // 2. Анимация (хаотичная прокрутка)
        while (t < rollDuration)
        {
            int randomFace = Random.Range(0, 6);
            diceImage.sprite = diceFaces[randomFace];

            yield return new WaitForSeconds(rollSpeed);
            t += rollSpeed;
        }

        // 3. Устанавливаем итоговую грань
        diceImage.sprite = diceFaces[currentValue - 1];

        Debug.Log("Dice rolled: " + currentValue);

        isRolling = false;

        // 4. Сообщаем системе результат
        OnDiceRolled?.Invoke(currentValue);
    }

    public int GetValue() => currentValue;
}