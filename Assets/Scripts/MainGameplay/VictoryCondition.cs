using UnityEngine;

// Единственная ответственность: знать, выиграна ли игра.
//
// GameController не проверяет победу сам — он подписывается на OnVictory.
// Это значит:
// - хочешь другое условие победы (рогалик, мультиплеер) → пишешь новый класс
// - GameController не трогаешь вообще
//
// Проверка вызывается снаружи (из GameController после каждого хода),
// а не внутри Update() — чтобы не тратить ресурсы каждый кадр
// и чтобы момент проверки был явным и предсказуемым.
public class VictoryCondition : MonoBehaviour
{
    // Подписчик получает этот контекст — достаточно для любого UI или аналитики.
    // Расширяй поля когда появятся: кто победил, за сколько ходов, какой счёт.
    public struct VictoryResult
    {
        public int turnsElapsed;
        // сюда позже: public Player winner; public int score; и т.д.
    }

    // GameController (и любой UI) подписывается сюда.
    // Срабатывает ровно один раз за игру.
    public System.Action<VictoryResult> OnVictory;

    private BoardGenerator board;
    private bool isResolved = false;
    private int turnsElapsed = 0;

    public void Init(BoardGenerator boardGenerator)
    {
        board = boardGenerator;
        isResolved = false;
        turnsElapsed = 0;
    }

    // Вызывается из GameController.EndTurn() после каждого хода.
    // Не через событие — нам важен порядок: сначала ход применён, потом проверка.
    public void CheckAfterTurn()
    {
        if (isResolved) return;
 
        turnsElapsed++;
 
        if (IsVictory())
        {
            isResolved = true;
            Debug.Log($"[Victory] Diagonal filled in {turnsElapsed} turns!");
            OnVictory?.Invoke(new VictoryResult { turnsElapsed = turnsElapsed });
        }
    }
 
    // Победа = стартовая клетка + вся центральная диагональ заняты.
    //
    // Стартовая клетка считается только если фигура на ней сделала круг —
    // иначе только что заспавненная фигура давала бы ложную победу
    // когда центр уже заполнен.
    private bool IsVictory()
    {
        return IsStartOccupiedByReturned() && IsCenterFull();
    }

    private bool IsStartOccupiedByReturned()
    {
        Vector2Int startPos = board.PerimeterPath[board.startIndex];
        TileInstance startTile = board.GetTile(startPos);
 
        if (!startTile.IsOccupied()) return false;
 
        // Фигура должна была покинуть старт и вернуться —
        // иначе только что заспавненная фигура ломает условие.
        Piece piece = startTile.OccupiedPiece;
        return piece.hasLeftStart;
    }

    // Центральная диагональ заполнена = каждая клетка центра занята фигурой.
    // Приватный — снаружи нет смысла спрашивать напрямую, только через CheckAfterTurn.
    private bool IsCenterFull()
    {
        var centerPath = board.CenterPath;
 
        if (centerPath.Count == 0) return false;
 
        foreach (var pos in centerPath)
        {
            if (!board.IsTileOccupied(pos)) 
                return false;
        }
 
        return true;
    }
}