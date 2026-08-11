using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps one authored additive room Scene loaded for the active expedition plan and places the player
/// at that room's entrance. It deliberately does not own combat, enemy spawning, portals, rewards, or saves.
/// </summary>
[DisallowMultipleComponent]
public sealed class DungeonRoomLoader : MonoBehaviour
{
    [Header("Runtime Links")]
    [SerializeField] private ExpeditionDirector expedition;
    [SerializeField] private Transform player;
    [SerializeField] private Transform returnToHubPoint;
    [SerializeField] private bool autoFindRuntimeLinks = true;

    [Header("Additive Room Catalog")]
    [SerializeField] private DungeonRoomCatalogEntry[] roomCatalog = Array.Empty<DungeonRoomCatalogEntry>();
    [SerializeField] private float entranceNavMeshSampleRadius = 2f;

    [Header("Runtime")]
    [SerializeField, TextArea] private string lastLoadMessage;

    private ExpeditionDirector subscribedExpedition;
    private Coroutine transition;
    private DungeonRoomCatalogEntry pendingEntry;
    private bool unloadRequested;
    private Scene currentRoomScene;
    private DungeonRoomTemplate currentTemplate;
    private int loadedRoomSeed = -1;

    public DungeonRoomTemplate CurrentTemplate => currentTemplate;
    public string CurrentTemplateId => currentTemplate == null ? string.Empty : currentTemplate.TemplateId;
    public string LastLoadMessage => lastLoadMessage;
    public bool HasLoadedRoom => currentRoomScene.IsValid() && currentRoomScene.isLoaded && currentTemplate != null;
    public bool IsTransitioning => transition != null || pendingEntry != null || unloadRequested;
    public bool HasLoadedActiveRoom => HasLoadedRoom && expedition != null && expedition.IsSnapshotReady && expedition.RunPlan != null &&
                                       loadedRoomSeed == expedition.RunPlan.currentRoomSeed &&
                                       string.Equals(CurrentTemplateId, expedition.CurrentRoomTemplateId, StringComparison.Ordinal);
    public bool CanUseCurrentRoomExits => HasLoadedActiveRoom && expedition != null && expedition.IsAwaitingRoomExit;

    private void Awake()
    {
        ResolveRuntimeLinks();
    }

    private void OnEnable()
    {
        ResolveRuntimeLinks();
        SubscribeToExpedition();
        RefreshActiveRoom();
    }

    private void OnDisable()
    {
        UnsubscribeFromExpedition();
    }

    private void OnValidate()
    {
        roomCatalog ??= Array.Empty<DungeonRoomCatalogEntry>();
        entranceNavMeshSampleRadius = Mathf.Max(0f, entranceNavMeshSampleRadius);

        for (int i = 0; i < roomCatalog.Length; i++)
        {
            roomCatalog[i]?.Normalize();
        }
    }

    public void RefreshActiveRoom()
    {
        ResolveRuntimeLinks();

        if (expedition == null || !expedition.IsSnapshotReady || !ShouldKeepRoomLoaded())
        {
            RequestUnload();
            return;
        }

        DungeonRunPlan runPlan = expedition.RunPlan;
        if (runPlan == null)
        {
            SetLastLoadMessage("DungeonRoomLoader blocked: the running expedition has no DungeonRunPlan.");
            return;
        }

        if (!TryResolveCatalogEntry(runPlan, out DungeonRoomCatalogEntry entry, out string blocker))
        {
            SetLastLoadMessage(blocker);
            Debug.LogWarning(blocker, this);
            return;
        }

        if (HasLoadedActiveRoom && string.Equals(CurrentTemplateId, entry.TemplateId, StringComparison.Ordinal))
        {
            RefreshRoomExits();
            return;
        }

        RequestLoad(entry);
    }

    public bool TryValidateCatalog(out string message)
    {
        int configuredCount = 0;

        for (int i = 0; i < roomCatalog.Length; i++)
        {
            DungeonRoomCatalogEntry entry = roomCatalog[i];
            if (entry == null || !entry.IsConfigured)
            {
                continue;
            }

            configuredCount++;
            for (int j = i + 1; j < roomCatalog.Length; j++)
            {
                if (roomCatalog[j] != null &&
                    string.Equals(entry.TemplateId, roomCatalog[j].TemplateId, StringComparison.Ordinal))
                {
                    message = $"DungeonRoomLoader has duplicate catalog id '{entry.TemplateId}'.";
                    return false;
                }
            }
        }

        if (configuredCount == 0)
        {
            message = "DungeonRoomLoader needs at least one catalog entry with a Template Id and additive Scene Path.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    /// <summary>
    /// Executes the return-portal choice, then puts the player at the persistent hub point before
    /// the director change unloads the current additive room.
    /// </summary>
    public bool TryReturnToHub()
    {
        ResolveRuntimeLinks();

        if (expedition == null || !expedition.IsAwaitingRoomExit || !CanUseCurrentRoomExits)
        {
            SetLastLoadMessage("Return portal blocked: clear the loaded current room before returning.");
            return false;
        }

        if (returnToHubPoint == null)
        {
            SetLastLoadMessage("Return portal blocked: assign DungeonRoomLoader Return To Hub Point in the persistent Gameplay scene.");
            return false;
        }

        if (!expedition.TryReturnToHub())
        {
            SetLastLoadMessage("Return portal blocked: the pending expedition reward could not be banked.");
            return false;
        }

        WarpPlayerTo(returnToHubPoint);
        SetLastLoadMessage("Return portal banked the reward and returned the hero to the hub.");
        return true;
    }

    /// <summary>
    /// Executes the deeper-exit choice. The director advances the saved plan; RefreshActiveRoom then
    /// replaces this additive scene even when the deterministic template id repeats.
    /// </summary>
    public bool TryEnterDeeperRoom()
    {
        ResolveRuntimeLinks();

        if (expedition == null || !expedition.IsAwaitingRoomExit || !CanUseCurrentRoomExits)
        {
            SetLastLoadMessage("Deeper exit blocked: clear the loaded current room before descending.");
            return false;
        }

        if (!expedition.TryEnterDeeperRoom())
        {
            SetLastLoadMessage("Deeper exit blocked: the expedition could not advance its room plan.");
            return false;
        }

        SetLastLoadMessage("Deeper exit selected; loading the next seeded room.");
        return true;
    }

    private void HandleExpeditionChanged()
    {
        RefreshActiveRoom();
        RefreshRoomExits();
    }

    private bool ShouldKeepRoomLoaded()
    {
        return expedition != null && expedition.IsSnapshotReady && expedition.HasActiveExpedition;
    }

    private bool TryResolveCatalogEntry(
        DungeonRunPlan runPlan,
        out DungeonRoomCatalogEntry entry,
        out string blocker)
    {
        entry = null;
        blocker = string.Empty;

        if (!TryValidateCatalog(out blocker))
        {
            return false;
        }

        if (runPlan.hasAssignedRoomTemplate)
        {
            for (int i = 0; i < roomCatalog.Length; i++)
            {
                DungeonRoomCatalogEntry candidate = roomCatalog[i];
                if (candidate != null && candidate.IsConfigured &&
                    string.Equals(candidate.TemplateId, runPlan.currentRoomTemplateId, StringComparison.Ordinal))
                {
                    entry = candidate;
                    return true;
                }
            }

            blocker = $"DungeonRoomLoader cannot restore saved template '{runPlan.currentRoomTemplateId}' because it is absent from the catalog.";
            return false;
        }

        int configuredIndex = runPlan.currentRoomSeed;
        for (int i = 0; i < roomCatalog.Length; i++)
        {
            if (roomCatalog[i] != null && roomCatalog[i].IsConfigured)
            {
                if (configuredIndex == 0)
                {
                    entry = roomCatalog[i];
                    break;
                }

                configuredIndex--;
            }
        }

        if (entry == null)
        {
            int configuredCount = 0;
            for (int i = 0; i < roomCatalog.Length; i++)
            {
                if (roomCatalog[i] != null && roomCatalog[i].IsConfigured)
                {
                    configuredCount++;
                }
            }

            int selectedIndex = configuredCount == 0 ? 0 : runPlan.currentRoomSeed % configuredCount;
            for (int i = 0; i < roomCatalog.Length; i++)
            {
                if (roomCatalog[i] == null || !roomCatalog[i].IsConfigured)
                {
                    continue;
                }

                if (selectedIndex == 0)
                {
                    entry = roomCatalog[i];
                    break;
                }

                selectedIndex--;
            }
        }

        if (entry == null)
        {
            blocker = "DungeonRoomLoader could not select a configured room catalog entry.";
            return false;
        }

        expedition.TryAssignCurrentRoomTemplate(entry.TemplateId);
        return true;
    }

    private void RequestLoad(DungeonRoomCatalogEntry entry)
    {
        pendingEntry = entry;
        unloadRequested = false;

        if (transition == null)
        {
            transition = StartCoroutine(ProcessRequests());
        }
    }

    private void RequestUnload()
    {
        pendingEntry = null;
        unloadRequested = true;

        if (transition == null && currentRoomScene.IsValid() && currentRoomScene.isLoaded)
        {
            transition = StartCoroutine(ProcessRequests());
        }
    }

    private IEnumerator ProcessRequests()
    {
        while (unloadRequested || pendingEntry != null)
        {
            DungeonRoomCatalogEntry requestedEntry = pendingEntry;
            bool shouldUnload = unloadRequested;
            pendingEntry = null;
            unloadRequested = false;

            if (shouldUnload)
            {
                yield return UnloadCurrentRoom();
                continue;
            }

            if (requestedEntry != null &&
                (!HasLoadedActiveRoom || !string.Equals(CurrentTemplateId, requestedEntry.TemplateId, StringComparison.Ordinal)))
            {
                yield return LoadRoom(requestedEntry);
            }
        }

        transition = null;
    }

    private IEnumerator LoadRoom(DungeonRoomCatalogEntry entry)
    {
        yield return UnloadCurrentRoom();

        AsyncOperation operation;
        try
        {
            operation = SceneManager.LoadSceneAsync(entry.ScenePath, LoadSceneMode.Additive);
        }
        catch (Exception exception)
        {
            SetLastLoadMessage($"DungeonRoomLoader could not load '{entry.ScenePath}': {exception.Message}");
            yield break;
        }

        if (operation == null)
        {
            SetLastLoadMessage($"DungeonRoomLoader could not start additive load for '{entry.ScenePath}'. Add the scene to Build Settings.");
            yield break;
        }

        yield return operation;

        Scene loadedScene = SceneManager.GetSceneByPath(entry.ScenePath);
        if (!loadedScene.IsValid() || !loadedScene.isLoaded)
        {
            SetLastLoadMessage($"DungeonRoomLoader loaded '{entry.ScenePath}' but could not resolve its Scene handle.");
            yield break;
        }

        DungeonRoomTemplate template = FindTemplate(loadedScene, entry.TemplateId);
        if (template == null)
        {
            currentRoomScene = loadedScene;
            SetLastLoadMessage($"DungeonRoomLoader requires one DungeonRoomTemplate with id '{entry.TemplateId}' in '{entry.ScenePath}'.");
            Debug.LogWarning(lastLoadMessage, this);
            yield return UnloadCurrentRoom();
            yield break;
        }

        if (!template.TryValidate(out string templateBlocker))
        {
            currentRoomScene = loadedScene;
            SetLastLoadMessage(templateBlocker);
            Debug.LogWarning(templateBlocker, template);
            yield return UnloadCurrentRoom();
            yield break;
        }

        currentRoomScene = loadedScene;
        currentTemplate = template;
        loadedRoomSeed = expedition == null || expedition.RunPlan == null ? -1 : expedition.RunPlan.currentRoomSeed;
        WarpPlayerToEntrance(template.EntrancePoint);
        RefreshRoomExits();
        SetLastLoadMessage($"Loaded room template '{entry.TemplateId}' for depth {expedition.Depth}, room {expedition.CurrentRoomIndex + 1}.");
    }

    private IEnumerator UnloadCurrentRoom()
    {
        Scene sceneToUnload = currentRoomScene;
        if (currentTemplate != null)
        {
            currentTemplate.ReturnPortal?.SetExitAvailable(false);
            currentTemplate.DeeperExit?.SetExitAvailable(false);
        }
        currentTemplate = null;
        currentRoomScene = default;
        loadedRoomSeed = -1;

        if (!sceneToUnload.IsValid() || !sceneToUnload.isLoaded)
        {
            yield break;
        }

        AsyncOperation operation = SceneManager.UnloadSceneAsync(sceneToUnload);
        if (operation != null)
        {
            yield return operation;
        }
    }

    private DungeonRoomTemplate FindTemplate(Scene scene, string templateId)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        DungeonRoomTemplate matchedTemplate = null;

        for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
        {
            DungeonRoomTemplate[] templates = roots[rootIndex].GetComponentsInChildren<DungeonRoomTemplate>(includeInactive: true);
            for (int templateIndex = 0; templateIndex < templates.Length; templateIndex++)
            {
                DungeonRoomTemplate candidate = templates[templateIndex];
                if (candidate == null || !string.Equals(candidate.TemplateId, templateId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (matchedTemplate != null)
                {
                    SetLastLoadMessage($"DungeonRoomLoader found multiple DungeonRoomTemplate roots with id '{templateId}'.");
                    return null;
                }

                matchedTemplate = candidate;
            }
        }

        return matchedTemplate;
    }

    private void WarpPlayerToEntrance(Transform entrancePoint)
    {
        WarpPlayerTo(entrancePoint);
    }

    private void WarpPlayerTo(Transform destination)
    {
        if (player == null || destination == null)
        {
            SetLastLoadMessage("DungeonRoomLoader needs a Player and destination anchor to place the hero.");
            return;
        }

        CharacterMotor motor = player.GetComponent<CharacterMotor>();
        motor?.Stop();

        NavMeshAgent agent = player.GetComponent<NavMeshAgent>();
        Vector3 targetPosition = destination.position;
        if (agent != null && agent.enabled &&
            NavMesh.SamplePosition(destination.position, out NavMeshHit hit, entranceNavMeshSampleRadius, agent.areaMask))
        {
            targetPosition = hit.position;
            if (agent.isOnNavMesh)
            {
                agent.Warp(targetPosition);
            }
            else
            {
                agent.enabled = false;
                player.SetPositionAndRotation(targetPosition, destination.rotation);
                agent.enabled = true;
            }
        }
        else
        {
            player.position = targetPosition;
        }

        player.rotation = destination.rotation;
    }

    private void RefreshRoomExits()
    {
        if (currentTemplate == null)
        {
            return;
        }

        bool exitsAvailable = CanUseCurrentRoomExits;
        currentTemplate.ReturnPortal?.SetExitAvailable(exitsAvailable);
        currentTemplate.DeeperExit?.SetExitAvailable(exitsAvailable);
    }

    private void ResolveRuntimeLinks()
    {
        if (expedition == null && autoFindRuntimeLinks)
        {
            expedition = FindAnyObjectByType<ExpeditionDirector>();
        }

        if (player == null && autoFindRuntimeLinks)
        {
            PlayerController playerController = FindAnyObjectByType<PlayerController>();
            player = playerController == null ? null : playerController.transform;
        }
    }

    private void SubscribeToExpedition()
    {
        if (subscribedExpedition == expedition)
        {
            return;
        }

        UnsubscribeFromExpedition();
        if (expedition != null)
        {
            expedition.Changed += HandleExpeditionChanged;
            subscribedExpedition = expedition;
        }
    }

    private void UnsubscribeFromExpedition()
    {
        if (subscribedExpedition != null)
        {
            subscribedExpedition.Changed -= HandleExpeditionChanged;
            subscribedExpedition = null;
        }
    }

    private void SetLastLoadMessage(string message)
    {
        lastLoadMessage = message;
    }
}

[Serializable]
public sealed class DungeonRoomCatalogEntry
{
    [SerializeField] private string templateId;
    [SerializeField] private string scenePath;

    public string TemplateId => templateId;
    public string ScenePath => scenePath;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(templateId) && !string.IsNullOrWhiteSpace(scenePath);

    public void Normalize()
    {
        templateId = string.IsNullOrWhiteSpace(templateId) ? string.Empty : templateId.Trim();
        scenePath = string.IsNullOrWhiteSpace(scenePath) ? string.Empty : scenePath.Trim();
    }
}
