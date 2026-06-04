using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(RawImage))]
public class DungeonViewportInputRouter : MonoBehaviour, IPointerDownHandler
{
    [Header("References")]
    [SerializeField] private RawImage viewportImage;
    [SerializeField] private Camera viewportCamera;
    [SerializeField] private PlayerController player;
    [SerializeField] private PlayableScreenLayoutController screenLayout;
    [SerializeField] private bool autoFindReferences = true;

    [Header("Input Rules")]
    [SerializeField] private bool requireDungeonFocus = true;

    [Header("Diagnostics")]
    [SerializeField] private string lastInputMessage = "Ready";

    public string LastInputMessage => lastInputMessage;

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        if (viewportImage == null)
        {
            viewportImage = GetComponent<RawImage>();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        ResolveReferences();

        if (requireDungeonFocus && screenLayout != null && screenLayout.CurrentFocus != PlayableScreenFocus.DungeonFocus)
        {
            lastInputMessage = $"Dungeon viewport click ignored during {screenLayout.CurrentFocus}.";
            return;
        }

        if (viewportImage == null)
        {
            lastInputMessage = "Dungeon viewport click ignored: RawImage is missing.";
            return;
        }

        if (viewportCamera == null)
        {
            lastInputMessage = "Dungeon viewport click ignored: camera is missing.";
            return;
        }

        if (player == null)
        {
            lastInputMessage = "Dungeon viewport click ignored: PlayerController is missing.";
            return;
        }

        RectTransform imageRect = viewportImage.rectTransform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                imageRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
        {
            lastInputMessage = "Dungeon viewport click ignored: pointer is outside the image rect.";
            return;
        }

        Rect rect = imageRect.rect;
        if (!rect.Contains(localPoint))
        {
            lastInputMessage = "Dungeon viewport click ignored: pointer is outside the image rect.";
            return;
        }

        Vector2 normalizedPoint = Rect.PointToNormalized(rect, localPoint);
        Rect uvRect = viewportImage.uvRect;
        Vector3 viewportPoint = new Vector3(
            uvRect.x + normalizedPoint.x * uvRect.width,
            uvRect.y + normalizedPoint.y * uvRect.height,
            0f);

        Ray ray = viewportCamera.ViewportPointToRay(viewportPoint);
        bool handled = player.HandlePrimaryClickRay(ray, IsStationaryAttackHeld());
        lastInputMessage = handled
            ? $"Dungeon viewport click routed at {viewportPoint.x:0.00}, {viewportPoint.y:0.00}."
            : $"Dungeon viewport click ignored: {player.LastClickMessage}";
    }

    private void ResolveReferences()
    {
        if (viewportImage == null)
        {
            viewportImage = GetComponent<RawImage>();
        }

        if (!autoFindReferences)
        {
            return;
        }

        if (player == null)
        {
            player = FindAnyObjectByType<PlayerController>();
        }

        if (screenLayout == null)
        {
            screenLayout = FindAnyObjectByType<PlayableScreenLayoutController>();
        }
    }

    private static bool IsStationaryAttackHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null
            && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
#else
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
    }
}
