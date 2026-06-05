using UnityEngine;
using UnityEngine.UI;

public class PanelCameraRenderTarget : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera sourceCamera;
    [SerializeField] private RawImage targetImage;
    [SerializeField] private bool autoFindTargetImageOnThisObject = true;

    [Header("Render Texture")]
    [SerializeField] private RenderTexture explicitRenderTexture;
    [SerializeField] private bool createRuntimeTexture = true;
    [SerializeField] private bool matchImageRect = true;
    [SerializeField] private Vector2Int fallbackSize = new Vector2Int(1280, 720);
    [SerializeField, Range(0.25f, 2f)] private float renderScale = 1f;
    [SerializeField] private int depthBufferBits = 24;
    [SerializeField] private RenderTextureFormat textureFormat = RenderTextureFormat.ARGB32;

    [Header("Lifecycle")]
    [SerializeField] private bool releaseRuntimeTextureOnDisable = true;
    [SerializeField] private bool restoreOriginalTargetsOnDisable = true;

    [Header("Diagnostics")]
    [SerializeField] private string lastBindingMessage = "Ready";

    private RenderTexture runtimeTexture;
    private Vector2Int runtimeTextureSize;
    private Camera originalCamera;
    private RenderTexture originalCameraTarget;
    private RawImage originalImage;
    private Texture originalImageTexture;

    public Camera SourceCamera => sourceCamera;
    public RawImage TargetImage => targetImage;
    public string LastBindingMessage => lastBindingMessage;
    public RenderTexture ActiveTexture => explicitRenderTexture != null ? explicitRenderTexture : runtimeTexture;
    public bool HasRequiredReferences => sourceCamera != null && targetImage != null;
    public bool HasBoundTexture =>
        HasRequiredReferences &&
        ActiveTexture != null &&
        sourceCamera.targetTexture == ActiveTexture &&
        targetImage.texture == ActiveTexture;

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
        ApplyNow();
    }

    private void LateUpdate()
    {
        if (sourceCamera == null || targetImage == null)
        {
            ResolveReferences();
        }

        if (createRuntimeTexture && explicitRenderTexture == null && matchImageRect)
        {
            ApplyNow();
        }
    }

    private void OnDisable()
    {
        if (restoreOriginalTargetsOnDisable)
        {
            RestoreOriginalTargets();
        }

        if (releaseRuntimeTextureOnDisable)
        {
            ReleaseRuntimeTexture();
        }
    }

    private void OnValidate()
    {
        fallbackSize = new Vector2Int(
            Mathf.Max(16, fallbackSize.x),
            Mathf.Max(16, fallbackSize.y));
        renderScale = Mathf.Clamp(renderScale, 0.25f, 2f);
        depthBufferBits = Mathf.Max(0, depthBufferBits);

        if (autoFindTargetImageOnThisObject && targetImage == null)
        {
            targetImage = GetComponent<RawImage>();
        }
    }

    public void ApplyNow()
    {
        ResolveReferences();

        if (sourceCamera == null)
        {
            lastBindingMessage = "Render target not bound: source camera is missing.";
            return;
        }

        if (targetImage == null)
        {
            lastBindingMessage = "Render target not bound: target RawImage is missing.";
            return;
        }

        RenderTexture texture = explicitRenderTexture;
        if (texture == null && createRuntimeTexture)
        {
            texture = EnsureRuntimeTexture(GetTargetSize());
        }

        if (texture == null)
        {
            texture = sourceCamera.targetTexture;
        }

        if (texture == null)
        {
            lastBindingMessage = "Render target not bound: no RenderTexture is available.";
            return;
        }

        CaptureOriginalTargets();
        sourceCamera.targetTexture = texture;
        targetImage.texture = texture;
        lastBindingMessage = $"Bound {sourceCamera.name} to {targetImage.name} ({texture.width}x{texture.height}).";
    }

    public void ReleaseRuntimeTexture()
    {
        if (runtimeTexture == null)
        {
            runtimeTextureSize = Vector2Int.zero;
            return;
        }

        runtimeTexture.Release();

        if (Application.isPlaying)
        {
            Destroy(runtimeTexture);
        }
        else
        {
            DestroyImmediate(runtimeTexture);
        }

        runtimeTexture = null;
        runtimeTextureSize = Vector2Int.zero;
    }

    private void ResolveReferences()
    {
        if (autoFindTargetImageOnThisObject && targetImage == null)
        {
            targetImage = GetComponent<RawImage>();
        }
    }

    private RenderTexture EnsureRuntimeTexture(Vector2Int targetSize)
    {
        if (runtimeTexture != null && runtimeTextureSize == targetSize && runtimeTexture.format == textureFormat)
        {
            return runtimeTexture;
        }

        ReleaseRuntimeTexture();

        runtimeTexture = new RenderTexture(targetSize.x, targetSize.y, depthBufferBits, textureFormat)
        {
            name = $"{gameObject.name}_RuntimeRenderTexture",
            filterMode = FilterMode.Bilinear,
            useMipMap = false,
            autoGenerateMips = false
        };
        runtimeTexture.Create();
        runtimeTextureSize = targetSize;
        return runtimeTexture;
    }

    private Vector2Int GetTargetSize()
    {
        if (!matchImageRect || targetImage == null)
        {
            return fallbackSize;
        }

        RectTransform rectTransform = targetImage.rectTransform;
        Vector2 size = rectTransform == null ? Vector2.zero : rectTransform.rect.size;
        int width = Mathf.CeilToInt(Mathf.Abs(size.x) * renderScale);
        int height = Mathf.CeilToInt(Mathf.Abs(size.y) * renderScale);

        if (width <= 0 || height <= 0)
        {
            return fallbackSize;
        }

        return new Vector2Int(Mathf.Max(16, width), Mathf.Max(16, height));
    }

    private void CaptureOriginalTargets()
    {
        if (originalCamera != sourceCamera)
        {
            originalCamera = sourceCamera;
            originalCameraTarget = sourceCamera.targetTexture;
        }

        if (originalImage != targetImage)
        {
            originalImage = targetImage;
            originalImageTexture = targetImage.texture;
        }
    }

    private void RestoreOriginalTargets()
    {
        if (originalCamera != null)
        {
            originalCamera.targetTexture = originalCameraTarget;
        }

        if (originalImage != null)
        {
            originalImage.texture = originalImageTexture;
        }
    }
}
