using UnityEngine;
using UnityEngine.UI;

// Простой мост между кнопкой "Начать игру" и GameController.
// GameController не знает о UI — GameStarter знает о обоих.
//
// Для мультиплеера: заменяешь этот скрипт на NetworkGameStarter
// который ждёт готовности всех игроков — GameController не меняется.
//
// Для туториала: TutorialGameStarter который показывает инструкции
// перед вызовом StartGame() — GameController не меняется.
public class GameStarter : MonoBehaviour
{
    [Header("References")]
    public GameController gameController;

    [Header("UI (опционально)")]
    public GameObject startScreen;   // экран до начала игры
    public Button startButton;       // кнопка на экране

    private void Start()
    {
        // Показываем стартовый экран если он есть
        if (startScreen != null)
            startScreen.SetActive(true);

        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);

        // Подписываемся на событие старта чтобы скрыть экран
        gameController.OnGameStarted += OnGameStarted;
    }

    private void OnDestroy()
    {
        if (gameController != null)
            gameController.OnGameStarted -= OnGameStarted;
    }

    // Вызывается кнопкой из UI
    public void OnStartClicked()
    {
        gameController.StartGame();
    }

    private void OnGameStarted()
    {
        if (startScreen != null)
            startScreen.SetActive(false);
    }

    // Для тестирования в редакторе — старт по пробелу
    
}