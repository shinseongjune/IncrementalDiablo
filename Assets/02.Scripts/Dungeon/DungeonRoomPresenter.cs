using UnityEngine;
using UnityEngine.Serialization;

public class DungeonRoomPresenter : MonoBehaviour
{
    [Header("Room Link")]
    [SerializeField] private CombatRoom combatRoom;
    [SerializeField] private bool autoFindCombatRoom = true;

    [Header("Prototype Room Shell Fallback")]
    [FormerlySerializedAs("autoBuildRuntimeVisuals")]
    [SerializeField] private bool autoBuildPrototypeFallbackVisuals = true;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Vector3 roomCenterWorldPosition = Vector3.zero;
    [SerializeField] private Vector2 roomSize = new Vector2(16f, 12f);
    [SerializeField] private float floorThickness = 0.05f;
    [SerializeField] private float wallThickness = 0.35f;
    [SerializeField] private float wallHeight = 0.45f;
    [SerializeField] private float floorYOffset = 0.02f;

    [Header("Prototype Debug Tint")]
    [SerializeField] private bool applyPrototypeStateTint;
    [FormerlySerializedAs("idleColor")]
    [SerializeField] private Color prototypeIdleTint = new Color(0.18f, 0.2f, 0.24f, 1f);
    [FormerlySerializedAs("startingColor")]
    [SerializeField] private Color prototypeStartingTint = new Color(0.72f, 0.48f, 0.16f, 1f);
    [FormerlySerializedAs("runningColor")]
    [SerializeField] private Color prototypeRunningTint = new Color(0.58f, 0.16f, 0.12f, 1f);
    [FormerlySerializedAs("clearedColor")]
    [SerializeField] private Color prototypeClearedTint = new Color(0.16f, 0.42f, 0.22f, 1f);
    [FormerlySerializedAs("failedColor")]
    [SerializeField] private Color prototypeFailedTint = new Color(0.34f, 0.1f, 0.12f, 1f);

    [Header("Optional Authored Renderers")]
    [SerializeField] private Renderer floorRenderer;
    [SerializeField] private Renderer[] boundaryRenderers = new Renderer[0];

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private CombatRoom subscribedRoom;
    private MaterialPropertyBlock propertyBlock;
    private Material runtimeMaterial;
    private bool usingPrototypeFallbackVisuals;

    private void Awake()
    {
        ResolveCombatRoom();
        EnsureRuntimeVisuals();
        RefreshVisuals();
    }

    private void OnEnable()
    {
        ResolveCombatRoom();
        EnsureRuntimeVisuals();
        SubscribeToRoom();
        RefreshVisuals();
    }

    private void OnDisable()
    {
        UnsubscribeFromRoom();
    }

    private void OnValidate()
    {
        roomSize.x = Mathf.Max(2f, roomSize.x);
        roomSize.y = Mathf.Max(2f, roomSize.y);
        floorThickness = Mathf.Max(0.01f, floorThickness);
        wallThickness = Mathf.Max(0.05f, wallThickness);
        wallHeight = Mathf.Max(0.05f, wallHeight);
        floorYOffset = Mathf.Max(0f, floorYOffset);
        boundaryRenderers ??= new Renderer[0];
    }

    private void ResolveCombatRoom()
    {
        if (combatRoom == null && autoFindCombatRoom)
        {
            combatRoom = GetComponent<CombatRoom>();
        }

        if (combatRoom == null && autoFindCombatRoom)
        {
            combatRoom = FindAnyObjectByType<CombatRoom>();
        }
    }

    private void EnsureRuntimeVisuals()
    {
        if (HasAuthoredVisuals() || !autoBuildPrototypeFallbackVisuals)
        {
            return;
        }

        if (visualRoot == null)
        {
            GameObject root = new GameObject("DungeonRoomVisuals_Runtime");
            root.transform.position = roomCenterWorldPosition;
            visualRoot = root.transform;
        }

        usingPrototypeFallbackVisuals = true;

        floorRenderer = CreateVisualPart(
            "FloorMarker",
            new Vector3(0f, floorYOffset, 0f),
            new Vector3(roomSize.x, floorThickness, roomSize.y));

        boundaryRenderers = new[]
        {
            CreateVisualPart(
                "Wall_North",
                new Vector3(0f, wallHeight * 0.5f, roomSize.y * 0.5f),
                new Vector3(roomSize.x + wallThickness, wallHeight, wallThickness)),
            CreateVisualPart(
                "Wall_South",
                new Vector3(0f, wallHeight * 0.5f, roomSize.y * -0.5f),
                new Vector3(roomSize.x + wallThickness, wallHeight, wallThickness)),
            CreateVisualPart(
                "Wall_East",
                new Vector3(roomSize.x * 0.5f, wallHeight * 0.5f, 0f),
                new Vector3(wallThickness, wallHeight, roomSize.y + wallThickness)),
            CreateVisualPart(
                "Wall_West",
                new Vector3(roomSize.x * -0.5f, wallHeight * 0.5f, 0f),
                new Vector3(wallThickness, wallHeight, roomSize.y + wallThickness))
        };
    }

    private Renderer CreateVisualPart(string name, Vector3 localPosition, Vector3 localScale)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(visualRoot, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;

        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = part.GetComponent<Renderer>();
        Material material = GetRuntimeMaterial();
        if (material != null)
        {
            renderer.sharedMaterial = material;
        }

        return renderer;
    }

    private bool HasAuthoredVisuals()
    {
        if (floorRenderer != null)
        {
            return true;
        }

        for (int i = 0; i < boundaryRenderers.Length; i++)
        {
            if (boundaryRenderers[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private void SubscribeToRoom()
    {
        if (subscribedRoom == combatRoom)
        {
            return;
        }

        UnsubscribeFromRoom();

        if (combatRoom == null)
        {
            return;
        }

        combatRoom.Changed += RefreshVisuals;
        subscribedRoom = combatRoom;
    }

    private void UnsubscribeFromRoom()
    {
        if (subscribedRoom == null)
        {
            return;
        }

        subscribedRoom.Changed -= RefreshVisuals;
        subscribedRoom = null;
    }

    private void RefreshVisuals()
    {
        if (!ShouldApplyPrototypeStateTint())
        {
            ClearPrototypeTint(floorRenderer);

            for (int i = 0; i < boundaryRenderers.Length; i++)
            {
                ClearPrototypeTint(boundaryRenderers[i]);
            }

            return;
        }

        Color color = ResolvePrototypeRoomTint();
        ApplyPrototypeTint(floorRenderer, color);

        for (int i = 0; i < boundaryRenderers.Length; i++)
        {
            ApplyPrototypeTint(boundaryRenderers[i], color);
        }
    }

    private bool ShouldApplyPrototypeStateTint()
    {
        return applyPrototypeStateTint || usingPrototypeFallbackVisuals;
    }

    private Color ResolvePrototypeRoomTint()
    {
        if (combatRoom == null)
        {
            return prototypeIdleTint;
        }

        return combatRoom.State switch
        {
            CombatRoomState.Starting => prototypeStartingTint,
            CombatRoomState.Running => prototypeRunningTint,
            CombatRoomState.Cleared => prototypeClearedTint,
            CombatRoomState.Failed => prototypeFailedTint,
            _ => prototypeIdleTint
        };
    }

    private void ApplyPrototypeTint(Renderer targetRenderer, Color color)
    {
        if (targetRenderer == null)
        {
            return;
        }

        propertyBlock ??= new MaterialPropertyBlock();
        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(BaseColorId, color);
        propertyBlock.SetColor(ColorId, color);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    private void ClearPrototypeTint(Renderer targetRenderer)
    {
        if (targetRenderer == null)
        {
            return;
        }

        targetRenderer.SetPropertyBlock(null);
    }
    private Material GetRuntimeMaterial()
    {
        if (runtimeMaterial != null)
        {
            return runtimeMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader == null)
        {
            return null;
        }

        runtimeMaterial = new Material(shader)
        {
            name = "DungeonRoomPresenter_PrototypeFallbackMaterial",
            hideFlags = HideFlags.DontSave
        };
        return runtimeMaterial;
    }
}
