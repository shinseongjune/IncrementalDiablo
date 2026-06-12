using UnityEngine;

public sealed class GroundDefenseEnemyView : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Renderer[] renderers = new Renderer[0];
    [SerializeField, Min(1f)] private float wallContactScaleMultiplier = 1.2f;
    [SerializeField] private bool showHealthBar = true;
    [SerializeField] private Color healthyColor = new Color(0.24f, 0.82f, 0.3f);
    [SerializeField] private Color woundedColor = new Color(0.95f, 0.22f, 0.12f);

    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock propertyBlock;
    private Vector3 baseScale = Vector3.one;
    private GroundDefenseBillboardHandle readableVisual;
    private GroundDefenseBillboardHandle healthBar;
    private Transform healthFill;
    private Renderer healthFillRenderer;
    private float healthBarWidth;

    public GroundDefenseEnemyArchetype Archetype { get; private set; }

    private void Awake()
    {
        ResolveVisualReferences();
        CaptureBaseScale();
    }

    private void OnValidate()
    {
        wallContactScaleMultiplier = Mathf.Max(1f, wallContactScaleMultiplier);
        ResolveVisualReferences();
        CaptureBaseScale();
    }

    public void Initialize(GroundDefenseEnemyArchetype archetype)
    {
        Archetype = archetype;
        ResolveVisualReferences();
        CaptureBaseScale();
        BuildReadableVisual();
    }

    public void Apply(
        GroundDefenseActorVisualState visualState,
        Vector3 position,
        float healthPercent,
        bool underFire,
        float feedbackPercent)
    {
        if (Archetype == null)
        {
            Release();
            return;
        }

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        transform.position = position;
        Transform targetRoot = visualRoot == null ? transform : visualRoot;
        targetRoot.localScale = GetScale(visualState, feedbackPercent);
        SetColor(GetColor(visualState, healthPercent, underFire));
        UpdateHealthBar(healthPercent, visualState);
    }

    public void Release()
    {
        Transform targetRoot = visualRoot == null ? transform : visualRoot;
        targetRoot.localScale = baseScale;
        UpdateHealthBar(1f, GroundDefenseActorVisualState.Inactive);
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    private Color GetColor(
        GroundDefenseActorVisualState visualState,
        float healthPercent,
        bool underFire)
    {
        if (readableVisual != null)
        {
            return visualState switch
            {
                GroundDefenseActorVisualState.Defeated => new Color(0.42f, 0.12f, 0.1f, 0.9f),
                GroundDefenseActorVisualState.WallContact => new Color(1f, 0.45f, 0.2f),
                _ when underFire => new Color(1f, 0.72f, 0.42f),
                _ => Color.white
            };
        }

        return visualState switch
        {
            GroundDefenseActorVisualState.Defeated => Archetype.DefeatColor,
            GroundDefenseActorVisualState.WallContact => Archetype.WallContactColor,
            _ when underFire => Archetype.UnderFireColor,
            _ => Color.Lerp(
                Archetype.UnderFireColor,
                Archetype.BaseColor,
                Mathf.Clamp01(healthPercent))
        };
    }

    private Vector3 GetScale(GroundDefenseActorVisualState visualState, float feedbackPercent)
    {
        return visualState switch
        {
            GroundDefenseActorVisualState.Defeated =>
                Vector3.Lerp(baseScale * 0.25f, baseScale, Mathf.Clamp01(feedbackPercent)),
            GroundDefenseActorVisualState.WallContact =>
                baseScale * Mathf.Lerp(1f, wallContactScaleMultiplier, Mathf.Clamp01(feedbackPercent)),
            _ => baseScale
        };
    }

    private void SetColor(Color color)
    {
        if (readableVisual != null)
        {
            SetRendererColor(readableVisual.Renderer, color);
            return;
        }

        if (renderers == null)
        {
            return;
        }

        propertyBlock ??= new MaterialPropertyBlock();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer target = renderers[i];
            if (target == null)
            {
                continue;
            }

            SetRendererColor(target, color);
        }
    }

    private void BuildReadableVisual()
    {
        if (readableVisual != null)
        {
            GroundDefenseBillboardUtility.DestroyVisual(readableVisual.Root);
            readableVisual = null;
            healthBar = null;
            healthFill = null;
            healthFillRenderer = null;
        }

        bool hasReadableTexture = Archetype != null && Archetype.VisualTexture != null;
        SetFallbackVisible(!hasReadableTexture);
        if (!hasReadableTexture || !Application.isPlaying)
        {
            return;
        }

        Camera defenseCamera = GroundDefenseBillboardUtility.FindDefenseCamera();
        readableVisual = GroundDefenseBillboardUtility.CreateBillboard(
            "EnemyRoleVisual",
            transform,
            defenseCamera,
            Archetype.VisualTexture,
            Archetype.VisualUvRect,
            Archetype.VisualSize,
            Color.white,
            10);
        readableVisual.Root.transform.localPosition =
            Vector3.up * Archetype.VisualHeightOffset;

        if (!showHealthBar)
        {
            return;
        }

        healthBarWidth = Mathf.Max(0.7f, Archetype.VisualSize.x * 0.72f);
        healthBar = GroundDefenseBillboardUtility.CreateBillboard(
            "EnemyHealthBar",
            transform,
            defenseCamera,
            Texture2D.whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(healthBarWidth, 0.1f),
            new Color(0.08f, 0.08f, 0.08f, 0.9f),
            20);
        healthBar.Root.transform.localPosition =
            Vector3.up * (Archetype.VisualHeightOffset + Archetype.VisualSize.y * 0.56f);
        healthFillRenderer = GroundDefenseBillboardUtility.CreateQuad(
            "Fill",
            healthBar.Root.transform,
            Texture2D.whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(healthBarWidth, 0.065f),
            healthyColor,
            21);
        healthFill = healthFillRenderer.transform;
    }

    private void UpdateHealthBar(float healthPercent, GroundDefenseActorVisualState visualState)
    {
        if (healthBar == null || healthFill == null)
        {
            return;
        }

        bool visible = visualState == GroundDefenseActorVisualState.Advancing;
        if (healthBar.Root.activeSelf != visible)
        {
            healthBar.Root.SetActive(visible);
        }

        float safeHealth = Mathf.Clamp01(healthPercent);
        healthFill.localScale = new Vector3(safeHealth, 1f, 1f);
        healthFill.localPosition = new Vector3(
            -healthBarWidth * (1f - safeHealth) * 0.5f,
            0f,
            -0.01f);
        SetRendererColor(
            healthFillRenderer,
            Color.Lerp(woundedColor, healthyColor, safeHealth));
    }

    private void SetFallbackVisible(bool visible)
    {
        if (renderers != null)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = visible;
                }
            }
        }

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = visible;
        }
    }

    private void SetRendererColor(Renderer target, Color color)
    {
        if (target == null)
        {
            return;
        }

        propertyBlock ??= new MaterialPropertyBlock();
        target.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(BaseColorProperty, color);
        propertyBlock.SetColor(ColorProperty, color);
        target.SetPropertyBlock(propertyBlock);
    }

    private void ResolveVisualReferences()
    {
        if (visualRoot == null)
        {
            visualRoot = transform;
        }

        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>(true);
        }
    }

    private void CaptureBaseScale()
    {
        Transform targetRoot = visualRoot == null ? transform : visualRoot;
        baseScale = targetRoot.localScale;
    }
}
