using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class GroundDefenseBillboardHandle
{
    public GroundDefenseBillboardHandle(GameObject root, Renderer renderer)
    {
        Root = root;
        Renderer = renderer;
    }

    public GameObject Root { get; }
    public Renderer Renderer { get; }
}

public static class GroundDefenseBillboardUtility
{
    public static GroundDefenseBillboardHandle CreateBillboard(
        string name,
        Transform parent,
        Camera facingCamera,
        Texture2D texture,
        Rect uvRect,
        Vector2 size,
        Color color,
        int sortingOrder,
        bool flipX = false)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);

        GroundDefenseBillboardFacing facing = root.AddComponent<GroundDefenseBillboardFacing>();
        facing.Configure(facingCamera);

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);

        Sprite sprite = CreateSprite(name, texture, uvRect, out Texture2D generatedTexture);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.flipX = flipX;
        renderer.sortingOrder = sortingOrder;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

        Vector2 spriteSize = sprite.bounds.size;
        visual.transform.localScale = new Vector3(
            size.x / Mathf.Max(0.001f, spriteSize.x),
            size.y / Mathf.Max(0.001f, spriteSize.y),
            1f);

        GroundDefenseGeneratedVisual resources =
            root.GetComponent<GroundDefenseGeneratedVisual>() ??
            root.AddComponent<GroundDefenseGeneratedVisual>();
        resources.Track(sprite);
        resources.Track(generatedTexture);
        return new GroundDefenseBillboardHandle(root, renderer);
    }

    public static MeshRenderer CreateQuad(
        string name,
        Transform parent,
        Texture texture,
        Rect uvRect,
        Vector2 size,
        Color color,
        int sortingOrder)
    {
        GameObject quad = new GameObject(name);
        quad.transform.SetParent(parent, false);

        Mesh mesh = new Mesh
        {
            name = $"{name}_RuntimeMesh",
            vertices = new[]
            {
                new Vector3(-size.x * 0.5f, -size.y * 0.5f, 0f),
                new Vector3(size.x * 0.5f, -size.y * 0.5f, 0f),
                new Vector3(-size.x * 0.5f, size.y * 0.5f, 0f),
                new Vector3(size.x * 0.5f, size.y * 0.5f, 0f)
            },
            uv = new[]
            {
                new Vector2(uvRect.xMin, uvRect.yMin),
                new Vector2(uvRect.xMax, uvRect.yMin),
                new Vector2(uvRect.xMin, uvRect.yMax),
                new Vector2(uvRect.xMax, uvRect.yMax)
            },
            triangles = new[] { 0, 2, 1, 2, 3, 1 }
        };
        mesh.RecalculateBounds();

        MeshFilter filter = quad.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;

        MeshRenderer renderer = quad.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = CreateMaterial(name, texture, color);
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.sortingOrder = sortingOrder;

        GroundDefenseGeneratedVisual resources =
            parent.GetComponent<GroundDefenseGeneratedVisual>() ??
            parent.gameObject.AddComponent<GroundDefenseGeneratedVisual>();
        resources.Track(mesh);
        resources.Track(renderer.sharedMaterial);
        return renderer;
    }

    public static Camera FindDefenseCamera(Camera preferred = null)
    {
        if (preferred != null)
        {
            return preferred;
        }

        Camera[] cameras = Camera.allCameras;
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera candidate = cameras[i];
            if (candidate != null && candidate.name.Contains("DefensePanel"))
            {
                return candidate;
            }
        }

        return Camera.main;
    }

    private static Sprite CreateSprite(
        string name,
        Texture2D texture,
        Rect uvRect,
        out Texture2D generatedTexture)
    {
        generatedTexture = null;
        Texture2D safeTexture = texture == null ? Texture2D.whiteTexture : texture;
        Rect safeUv = ClampUvRect(uvRect);
        if (TryCreateCutoutTexture(name, safeTexture, safeUv, out Texture2D cutoutTexture))
        {
            generatedTexture = cutoutTexture;
            Sprite cutoutSprite = Sprite.Create(
                cutoutTexture,
                new Rect(0f, 0f, cutoutTexture.width, cutoutTexture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            cutoutSprite.name = $"{name}_CutoutRuntimeSprite";
            return cutoutSprite;
        }

        Rect pixelRect = new Rect(
            safeUv.xMin * safeTexture.width,
            safeUv.yMin * safeTexture.height,
            safeUv.width * safeTexture.width,
            safeUv.height * safeTexture.height);
        Sprite sprite = Sprite.Create(
            safeTexture,
            pixelRect,
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect);
        sprite.name = $"{name}_RuntimeSprite";
        return sprite;
    }

    private static bool TryCreateCutoutTexture(
        string name,
        Texture2D source,
        Rect uvRect,
        out Texture2D cutoutTexture)
    {
        cutoutTexture = null;
        if (source == null)
        {
            return false;
        }

        int x = Mathf.Clamp(
            Mathf.FloorToInt(uvRect.xMin * source.width),
            0,
            source.width - 1);
        int y = Mathf.Clamp(
            Mathf.FloorToInt(uvRect.yMin * source.height),
            0,
            source.height - 1);
        int width = Mathf.Clamp(
            Mathf.CeilToInt(uvRect.width * source.width),
            1,
            source.width - x);
        int height = Mathf.Clamp(
            Mathf.CeilToInt(uvRect.height * source.height),
            1,
            source.height - y);

        Color[] pixels;
        try
        {
            pixels = source.GetPixels(x, y, width, height);
        }
        catch (UnityException)
        {
            return false;
        }

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = ApplyDarkMatteCutout(pixels[i]);
        }

        cutoutTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = $"{name}_CutoutRuntimeTexture",
            filterMode = source.filterMode,
            wrapMode = TextureWrapMode.Clamp
        };
        cutoutTexture.SetPixels(pixels);
        cutoutTexture.Apply(false, true);
        return true;
    }

    private static Color ApplyDarkMatteCutout(Color pixel)
    {
        if (pixel.a <= 0.01f)
        {
            return Color.clear;
        }

        float brightness = Mathf.Max(pixel.r, pixel.g, pixel.b);
        if (brightness <= 0.045f)
        {
            pixel.a = 0f;
            return pixel;
        }

        if (brightness < 0.095f)
        {
            pixel.a *= Mathf.InverseLerp(0.045f, 0.095f, brightness);
        }

        return pixel;
    }

    private static Rect ClampUvRect(Rect uvRect)
    {
        float xMin = Mathf.Clamp01(Mathf.Min(uvRect.xMin, uvRect.xMax));
        float xMax = Mathf.Clamp01(Mathf.Max(uvRect.xMin, uvRect.xMax));
        float yMin = Mathf.Clamp01(Mathf.Min(uvRect.yMin, uvRect.yMax));
        float yMax = Mathf.Clamp01(Mathf.Max(uvRect.yMin, uvRect.yMax));
        if (xMax - xMin < 0.0001f)
        {
            xMin = Mathf.Min(xMin, 0.9999f);
            xMax = xMin + 0.0001f;
        }

        if (yMax - yMin < 0.0001f)
        {
            yMin = Mathf.Min(yMin, 0.9999f);
            yMax = yMin + 0.0001f;
        }

        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    public static void DestroyVisual(GameObject visual)
    {
        if (visual == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(visual);
        }
        else
        {
            Object.DestroyImmediate(visual);
        }
    }

    private static Material CreateMaterial(string name, Texture texture, Color color)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        Material material = new Material(shader)
        {
            name = $"{name}_RuntimeMaterial",
            mainTexture = texture,
            color = color,
            renderQueue = (int)RenderQueue.Transparent
        };

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        return material;
    }
}

internal sealed class GroundDefenseBillboardFacing : MonoBehaviour
{
    private Camera facingCamera;

    public void Configure(Camera camera)
    {
        facingCamera = GroundDefenseBillboardUtility.FindDefenseCamera(camera);
        FaceCamera();
    }

    private void LateUpdate()
    {
        FaceCamera();
    }

    private void FaceCamera()
    {
        facingCamera = GroundDefenseBillboardUtility.FindDefenseCamera(facingCamera);
        if (facingCamera != null)
        {
            transform.rotation = Quaternion.LookRotation(
                -facingCamera.transform.forward,
                facingCamera.transform.up);
        }
    }
}

internal sealed class GroundDefenseGeneratedVisual : MonoBehaviour
{
    private readonly List<Object> resources = new List<Object>();

    public void Track(Object resource)
    {
        if (resource != null)
        {
            resources.Add(resource);
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < resources.Count; i++)
        {
            if (resources[i] != null)
            {
                Destroy(resources[i]);
            }
        }
    }
}
