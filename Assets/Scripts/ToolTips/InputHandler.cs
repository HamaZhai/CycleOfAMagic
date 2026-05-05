using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class InputHandler : MonoBehaviour
{
    public Camera cam;
    public LayerMask interactableMask;

    [SerializeField] private float clickAssistRadius = 0.18f;

    private InputAction clickAction;
    private bool clickRequested;

    private void Awake()
    {
        clickAction = new InputAction(binding: "<Mouse>/leftButton");
        clickAction.performed += ctx => clickRequested = true;
        clickAction.Enable();
    }

    private void OnDestroy()
    {
        clickAction?.Disable();
        clickAction?.Dispose();
    }

    private void Update()
    {
        if (!clickRequested) return;
        clickRequested = false;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);
        RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(ray, Mathf.Infinity, interactableMask);

        if (TryHandlePieceHit(hits)) return;
        if (TryHandleOccupiedTileHit(hits)) return;
        if (TryHandleAssistedPieceHit(mousePos)) return;
        if (TryHandleTileHit(hits)) return;
    }

    private bool TryHandlePieceHit(RaycastHit2D[] hits)
    {
        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;

            Piece piece = hit.collider.GetComponent<Piece>();
            if (piece == null) continue;

            piece.HandleClick();
            return true;
        }

        return false;
    }

    private bool TryHandleOccupiedTileHit(RaycastHit2D[] hits)
    {
        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;

            TileInstance tile = hit.collider.GetComponent<TileInstance>();
            if (tile == null || tile.OccupiedPiece == null) continue;

            tile.OccupiedPiece.HandleClick();
            return true;
        }

        return false;
    }

    private bool TryHandleAssistedPieceHit(Vector2 mousePos)
    {
        Vector3 world = cam.ScreenToWorldPoint(mousePos);
        Vector2 point = new Vector2(world.x, world.y);
        Collider2D[] colliders = Physics2D.OverlapCircleAll(point, clickAssistRadius, interactableMask);

        foreach (var collider in colliders)
        {
            Piece piece = collider.GetComponent<Piece>();
            if (piece == null) continue;

            piece.HandleClick();
            return true;
        }

        foreach (var collider in colliders)
        {
            TileInstance tile = collider.GetComponent<TileInstance>();
            if (tile == null || tile.OccupiedPiece == null) continue;

            tile.OccupiedPiece.HandleClick();
            return true;
        }

        return false;
    }

    private bool TryHandleTileHit(RaycastHit2D[] hits)
    {
        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;

            TileInstance tile = hit.collider.GetComponent<TileInstance>();
            if (tile == null) continue;

            tile.HandleClick();
            return true;
        }

        return false;
    }
}
