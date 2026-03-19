using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DiceController : MonoBehaviour
{
    public Image diceImage;
    public Sprite[] diceFaces; // 0 = 1 ... 5 = 6

    public float rollDuration = 1f;
    public float rollSpeed = 0.05f;

    private bool isRolling = false;
    private int currentValue;

    public void OnDiceClicked()
    {
        if (isRolling) return;

        StartCoroutine(RollDice());
    }

    IEnumerator RollDice()
    {
        isRolling = true;

        float t = 0f;

        // ¿Õ»Ã¿÷»ﬂ ¡–Œ— ¿ (ÒÔ‡Ï ÒÔ‡ÈÚÓ‚)
        while (t < rollDuration)
        {
            int randomFace = Random.Range(0, 6);
            diceImage.sprite = diceFaces[randomFace];

            yield return new WaitForSeconds(rollSpeed);
            t += rollSpeed;
        }

        // ‘»Õ¿À‹Õ€… –≈«”À‹“¿“
        currentValue = Random.Range(1, 7);
        diceImage.sprite = diceFaces[currentValue - 1];

        isRolling = false;
    }

    public int GetValue()
    {
        return currentValue;
    }
}