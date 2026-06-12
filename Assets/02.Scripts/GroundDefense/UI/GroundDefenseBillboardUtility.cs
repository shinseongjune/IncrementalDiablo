using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class GroundDefenseBillboardHandle
{
    public GroundDefenseBillboardHandle(GameObject root, MeshRenderer renderer)
    {
        Root = root;
        Renderer = renderer;
    }

    public GameObject Root { get; }
    public MeshRenderer Renderer { get; }
}

public static class GroundDefenseBillboardUtility
{
    public static GroundDefenseBillboardHandle CreateBillboard(
        string name,
        Transform parent,
        Camera facingCamera,
        Texture texture,
        Rect uvRect,
        Vector2 size,
        Color color,
        int sortingOrder)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);

        GroundDefenseBillboardFacing facing = root.AddComponent<GroundDefenseBillboardFacing>();
        facing.Configure(facingCamera);

        MeshRenderer renderer = CreateQuad(
            "Visual",
            root.transform,
            texture,
            uvRect,
            size,
            color,
            sortingOrder);
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
