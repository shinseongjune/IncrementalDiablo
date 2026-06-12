using System.Collections.Generic;
using UnityEngine;

public sealed class GroundDefenseEnemyPool : MonoBehaviour
{
    [SerializeField] private Transform poolRoot;
    [SerializeField] private GroundDefenseEnemyArchetype[] prewarmArchetypes = new GroundDefenseEnemyArchetype[0];
    [SerializeField, Min(0)] private int prewarmPerArchetype = 4;
    [SerializeField, Min(1)] private int maxInstances = 16;

    private readonly List<GroundDefenseEnemyView> views = new List<GroundDefenseEnemyView>();
    private bool prewarmed;

    public int CreatedCount => views.Count;
    public int ActiveCount
    {
        get
        {
            int activeCount = 0;
            for (int i = 0; i < views.Count; i++)
            {
                if (views[i] != null && views[i].gameObject.activeSelf)
                {
                    activeCount += 1;
                }
            }

            return activeCount;
        }
    }

    public bool IsReady
    {
        get
        {
            if (prewarmArchetypes == null)
            {
                return false;
            }

            for (int i = 0; i < prewarmArchetypes.Length; i++)
            {
                if (prewarmArchetypes[i] != null && prewarmArchetypes[i].ViewPrefab != null)
                {
                    return true;
                }
            }

            return false;
        }
    }

    private void Awake()
    {
        EnsurePrewarmed();
    }

    private void OnValidate()
    {
        prewarmPerArchetype = Mathf.Max(0, prewarmPerArchetype);
        maxInstances = Mathf.Max(1, maxInstances);
    }

    public GroundDefenseEnemyView Rent(GroundDefenseEnemyArchetype archetype)
    {
        if (archetype == null || archetype.ViewPrefab == null)
        {
            return null;
        }

        EnsurePrewarmed();
        for (int i = 0; i < views.Count; i++)
        {
            GroundDefenseEnemyView view = views[i];
            if (view != null && view.Archetype == archetype && !view.gameObject.activeSelf)
            {
                return view;
            }
        }

        return CreateView(archetype);
    }

    public void Return(GroundDefenseEnemyView view)
    {
        if (view != null)
        {
            view.Release();
        }
    }

    public void ReturnAll()
    {
        for (int i = 0; i < views.Count; i++)
        {
            if (views[i] != null)
            {
                views[i].Release();
            }
        }
    }

    private void EnsurePrewarmed()
    {
        if (prewarmed || !Application.isPlaying)
        {
            return;
        }

        prewarmed = true;
        if (prewarmArchetypes == null)
        {
            return;
        }

        for (int i = 0; i < prewarmArchetypes.Length; i++)
        {
            GroundDefenseEnemyArchetype archetype = prewarmArchetypes[i];
            if (archetype == null || archetype.ViewPrefab == null)
            {
                continue;
            }

            for (int count = 0; count < prewarmPerArchetype && views.Count < maxInstances; count++)
            {
                CreateView(archetype);
            }
        }
    }

    private GroundDefenseEnemyView CreateView(GroundDefenseEnemyArchetype archetype)
    {
        if (views.Count >= maxInstances)
        {
            return null;
        }

        Transform parent = poolRoot == null ? transform : poolRoot;
        GroundDefenseEnemyView view = Instantiate(archetype.ViewPrefab, parent);
        view.name = $"{archetype.DisplayName}_Pooled_{views.Count + 1:00}";
        view.Initialize(archetype);
        view.Release();
        views.Add(view);
        return view;
    }
}
