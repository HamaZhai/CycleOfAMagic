using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class InputHandler : MonoBehaviour
{
    public Camera cam;
    public LayerMask interactableMask;

    private InputAction clickAction;

    private bool clickRequested = false;

    private void Awake()
    {   
        clickAction = new InputAction(binding: "<Mouse>/leftButton");
        clickAction.performed += ctx => clickRequested = true;
        clickAction.Enable();
    }

    private void Update()
    {
        if (!clickRequested) return;
        clickRequested = false;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);

        RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, interactableMask);

        if (hit.collider != null)
        {
            var tile = hit.collider.GetComponent<TileInstance>();
            if (tile != null)
            {
                tile.HandleClick();
                return;
            }

            var piece = hit.collider.GetComponent<Piece>();
            if (piece != null)
            {
                piece.HandleClick();
                return;
            }
        }
    }
}