using System;
using System.Collections.Generic;
using UnityEngine;

public class CombatRoom : MonoBehaviour
{
    [Header("Expedition Link")]
    [SerializeField] private ExpeditionDirector expedition;
    [SerializeField] private bool autoFindExpedition = true;
    [SerializeField] private bool startWhenExpeditionRuns = true;
    [SerializeField] private float startCountdownSeconds = 1.5f;

    [Header("Additive Room Gate")]
    [SerializeField] private DungeonRoomLoader roomLoader;
    [SerializeField] private bool autoFindRoomLoader = true;
    [SerializeField] private bool requireLoadedTemplateWhenRoomLoaderIsConfigured = true;

    [Header("Actor Tracking")]
    [SerializeField] private Health heroHealth;
    [SerializeField] private Health[] enemyHealths = new Health[0];
    [SerializeField] private bool autoFindTrackedCombatants = true;
    [SerializeField] private bool refillTrackedCombatantsOnBegin = true;
    [SerializeField] private bool manageTrackedEnemyActivity = true;
    [SerializeField] private bool blockPrototypeSimulationWhenEnemySetupBlocked = true;

    [Header("Prototype Simulation")]
    [SerializeField] private bool simulateWhenNoEnemies = true;
    [SerializeField] private float prototypeHeroHealth = 60f;
    [SerializeField] private float prototypeHeroDps = 12f;
    [SerializeField] private float prototypeEnemyHealth = 40f;
    [SerializeField] private float prototypeEnemyDps = 4f;
    [SerializeField] private float maxPrototypeCombatSeconds = 20f;

    [Header("Runtime")]
    [SerializeField] private CombatRoomState state = CombatRoomState.Idle;
    [SerializeField] private int activeRoomIndex = -1;
    [SerializeField] private float countdownRemaining;
    [SerializeField] private float elapsedSeconds;
    [SerializeField] private float currentHeroHealth;
    [SerializeField] private float currentEnemyHealth;
    [SerializeField] private CombatRoomResult lastResult;
    [SerializeField] private string trackedEnemySetupBlocker;
    [SerializeField] private string roomTemplateBlocker;

    private ExpeditionDirector subscribedExpedition;
    private bool resolvingRoom;
    private bool externalStartControl;

    public event Action Changed;
    public event Action<CombatRoomResult> Resolved;

    public CombatRoomState State => state;
    public int ActiveRoomIndex => activeRoomIndex;
    public float CountdownRemaining => Mathf.Max(0f, countdownRemaining);
    public float ElapsedSeconds => Mathf.Max(0f, elapsedSeconds);
    public float CurrentHeroHealth => Mathf.Max(0f, currentHeroHealth);
    public float CurrentEnemyHealth => Mathf.Max(0f, currentEnemyHealth);
    public CombatRoomResult LastResult => lastResult;
    public bool UsesTrackedCombatants => heroHealth != null && HasAnyEnemyReference();
    public bool HasTrackedEnemySetupBlocker => !string.IsNullOrWhiteSpace(trackedEnemySetupBlocker);
    public string TrackedEnemySetupBlocker => trackedEnemySetupBlocker;
    public bool HasRoomTemplateBlocker => !string.IsNullOrWhiteSpace(roomTemplateBlocker);
    public string RoomTemplateBlocker => roomTemplateBlocker;
    public bool IsPrototypeSimulationAvailable => simulateWhenNoEnemies && !HasAnyEnemyReference() && (!HasTrackedEnemySetupBlocker || !blockPrototypeSimulationWhenEnemySetupBlocked);
    public int ActiveDepth => expedition == null ? 1 : expedition.Depth;
    public DungeonEncounterProfile ActiveEncounter => expedition == null
        ? DungeonEncounterModel.GetDefault()
        : expedition.ActiveEncounter;
    public DungeonDepthBalanceProfile DepthBalance => expedition == null
        ? DungeonDepthBalanceModel.Evaluate(ActiveDepth)
        : expedition.GetEffectiveDepthBalance(ActiveDepth);
    public bool IsStableForWorldSnapshot => !GameRuntimeRestoreGate.IsRestoring &&
                                            !resolvingRoom &&
                                            state != CombatRoomState.Starting &&
                                            (state == CombatRoomState.Running || state == CombatRoomState.Cleared || state == CombatRoomState.Failed);

    private void Awake()
    {
        ResolveExpedition();
        ResolveRoomLoader();
        ResolveTrackedCombatants();
    }

    private void OnEnable()
    {
        ResolveExpedition();
        ResolveRoomLoader();
        ResolveTrackedCombatants();
        SubscribeToExpedition();
        SetTrackedEnemiesActive(expedition != null && expedition.IsRunning && state == CombatRoomState.Running);
        TryBeginForRunningExpedition();
    }

    private void OnDisable()
    {
        UnsubscribeFromExpedition();
    }

    private void Update()
    {
        if (GameRuntimeRestoreGate.IsRestoring)
        {
            return;
        }

        ResolveExpedition();
        ResolveRoomLoader();
        SubscribeToExpedition();
        TryBeginForRunningExpedition();

        if (state == CombatRoomState.Starting)
        {
            TickStarting();
            return;
        }

        if (state == CombatRoomState.Running)
        {
            TickRunning();
        }
    }

    private void OnValidate()
    {
        startCountdownSeconds = Mathf.Max(0f, startCountdownSeconds);
        prototypeHeroHealth = Mathf.Max(1f, prototypeHeroHealth);
        prototypeHeroDps = Mathf.Max(0.1f, prototypeHeroDps);
        prototypeEnemyHealth = Mathf.Max(1f, prototypeEnemyHealth);
        prototypeEnemyDps = Mathf.Max(0f, prototypeEnemyDps);
        maxPrototypeCombatSeconds = Mathf.Max(1f, maxPrototypeCombatSeconds);
        enemyHealths ??= new Health[0];
    }

    public bool BeginRoom()
    {
        ResolveExpedition();
        ResolveRoomLoader();
        ResolveTrackedCombatants();

        if (expedition == null || !expedition.IsSnapshotReady || !expedition.IsRunning)
        {
            Debug.LogWarning("CombatRoom cannot begin because no running ExpeditionDirector was found.", this);
            return false;
        }

        if (!TryValidateActiveRoomTemplate(out string roomBlocker))
        {
            SetRoomTemplateBlocker(roomBlocker);
            return false;
        }

        activeRoomIndex = expedition.CurrentRoomIndex;
        ClearTrackedEnemySetupBlocker(false);
        ClearRoomTemplateBlocker(false);
        RefillTrackedCombatants();
        state = CombatRoomState.Starting;
        countdownRemaining = startCountdownSeconds;
        elapsedSeconds = 0f;
        currentHeroHealth = ResolveInitialHeroHealth();
        currentEnemyHealth = ResolveInitialEnemyHealth();
        SetLastResult(CombatRoomResolution.None, $"Room starting / Encounter: {ActiveEncounter.DisplayName}");
        NotifyChanged();

        if (Mathf.Approximately(countdownRemaining, 0f))
        {
            EnterRunning();
        }

        return true;
    }

    /// <summary>
    /// Lets a physical traversal route decide when the player has entered the active room.
    /// Without this claim, legacy one-room expeditions keep their automatic start behavior.
    /// </summary>
    public void SetExternalStartControl(bool enabled)
    {
        externalStartControl = enabled;

        if (!externalStartControl)
        {
            TryBeginForRunningExpedition();
        }
    }

    public bool ForceClearRoom()
    {
        return ResolveRoom(CombatRoomResolution.Cleared, "Room cleared by debug command");
    }

    public bool ForceFailRoom()
    {
        return ResolveRoom(CombatRoomResolution.Failed, "Room failed by debug command");
    }

    public void RegisterTrackedEnemies(IReadOnlyList<Health> trackedEnemies, bool refill = false)
    {
        if (trackedEnemies == null || trackedEnemies.Count == 0)
        {
            enemyHealths = new Health[0];
        }
        else
        {
            List<Health> validEnemies = new List<Health>(trackedEnemies.Count);
            for (int i = 0; i < trackedEnemies.Count; i++)
            {
                Health enemyHealth = trackedEnemies[i];
                if (enemyHealth != null)
                {
                    validEnemies.Add(enemyHealth);
                }
            }

            enemyHealths = validEnemies.ToArray();
        }

        if (refill)
        {
            RefillTrackedCombatants();
        }

        currentEnemyHealth = ResolveInitialEnemyHealth();
        SetTrackedEnemiesActive(state == CombatRoomState.Running);
        ClearTrackedEnemySetupBlocker(false);
        NotifyChanged();
    }

    public bool TryCreateWorldSnapshot(out DungeonCombatWorldSnapshot snapshot, out string error)
    {
        snapshot = null;
        if (!IsStableForWorldSnapshot)
        {
            error = "Dungeon combat is transitioning and cannot be checkpointed.";
            return false;
        }

        snapshot = new DungeonCombatWorldSnapshot
        {
            state = state,
            countdownRemaining = CountdownRemaining,
            elapsedSeconds = ElapsedSeconds,
            currentHeroHealth = CurrentHeroHealth,
            currentEnemyHealth = CurrentEnemyHealth
        };
        error = string.Empty;
        return true;
    }

    public bool TryRestoreWorldSnapshot(
        DungeonCombatWorldSnapshot snapshot,
        Health restoredHero,
        IReadOnlyList<Health> restoredEnemies,
        out string error)
    {
        if (snapshot == null || !Enum.IsDefined(typeof(CombatRoomState), snapshot.state) ||
            snapshot.countdownRemaining < 0f || snapshot.elapsedSeconds < 0f ||
            snapshot.currentHeroHealth < 0f || snapshot.currentEnemyHealth < 0f)
        {
            error = "Dungeon combat restore snapshot is invalid.";
            return false;
        }

        if (snapshot.state == CombatRoomState.Starting)
        {
            error = "Dungeon combat restore cannot resume an in-progress start countdown.";
            return false;
        }

        heroHealth = restoredHero;
        List<Health> validEnemies = new List<Health>();
        if (restoredEnemies != null)
        {
            for (int i = 0; i < restoredEnemies.Count; i++)
            {
                if (restoredEnemies[i] != null)
                {
                    validEnemies.Add(restoredEnemies[i]);
                }
            }
        }

        enemyHealths = validEnemies.ToArray();
        state = snapshot.state;
        activeRoomIndex = expedition == null ? snapshot.state == CombatRoomState.Idle ? -1 : 0 : expedition.CurrentRoomIndex;
        countdownRemaining = snapshot.countdownRemaining;
        elapsedSeconds = snapshot.elapsedSeconds;
        currentHeroHealth = snapshot.currentHeroHealth;
        currentEnemyHealth = snapshot.currentEnemyHealth;
        ClearTrackedEnemySetupBlocker(false);
        ClearRoomTemplateBlocker(false);
        SetLastResult(CombatRoomResolution.None, "Dungeon combat restored from world checkpoint.");
        SetTrackedEnemiesActive(state == CombatRoomState.Running);
        NotifyChanged();
        error = string.Empty;
        return true;
    }

    public void ReportTrackedEnemySetupBlocker(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        trackedEnemySetupBlocker = message.Trim();

        if (state == CombatRoomState.Starting || state == CombatRoomState.Running)
        {
            currentEnemyHealth = 0f;
            SetLastResult(CombatRoomResolution.None, trackedEnemySetupBlocker);
        }

        NotifyChanged();
    }

    public void ClearTrackedEnemySetupBlocker()
    {
        ClearTrackedEnemySetupBlocker(true);
    }

    private void TickStarting()
    {
        if (expedition == null || !expedition.IsRunning)
        {
            return;
        }

        countdownRemaining = Mathf.Max(0f, countdownRemaining - Time.deltaTime);

        if (countdownRemaining <= 0f)
        {
            EnterRunning();
        }
    }

    private void TickRunning()
    {
        if (expedition == null || !expedition.IsRunning)
        {
            return;
        }

        elapsedSeconds += Time.deltaTime;

        if (TryResolveTrackedCombatants())
        {
            return;
        }

        if (HasTrackedEnemySetupBlocker && blockPrototypeSimulationWhenEnemySetupBlocked)
        {
            return;
        }

        if (simulateWhenNoEnemies && !HasAnyEnemyReference())
        {
            TickPrototypeSimulation();
        }
    }

    private void EnterRunning()
    {
        state = CombatRoomState.Running;
        countdownRemaining = 0f;
        SetTrackedEnemiesActive(true);
        SetLastResult(CombatRoomResolution.None, $"Room combat running / Encounter: {ActiveEncounter.DisplayName}");
        NotifyChanged();
    }

    private bool TryResolveTrackedCombatants()
    {
        if (heroHealth != null)
        {
            currentHeroHealth = heroHealth.Current;

            if (!heroHealth.IsAlive)
            {
                return ResolveRoom(CombatRoomResolution.Failed, "Hero defeated");
            }
        }

        bool hasEnemy = false;
        bool hasLivingEnemy = false;
        float trackedEnemyHealth = 0f;

        for (int i = 0; i < enemyHealths.Length; i++)
        {
            Health enemyHealth = enemyHealths[i];
            if (enemyHealth == null)
            {
                continue;
            }

            hasEnemy = true;
            trackedEnemyHealth += Mathf.Max(0f, enemyHealth.Current);

            if (enemyHealth.IsAlive)
            {
                hasLivingEnemy = true;
            }
        }

        if (!hasEnemy)
        {
            return false;
        }

        currentEnemyHealth = trackedEnemyHealth;
        return !hasLivingEnemy && ResolveRoom(CombatRoomResolution.Cleared, "All tracked enemies defeated");
    }

    private void TickPrototypeSimulation()
    {
        DungeonDepthBalanceProfile balance = DepthBalance;
        currentEnemyHealth = Mathf.Max(0f, currentEnemyHealth - prototypeHeroDps * Time.deltaTime);
        currentHeroHealth = Mathf.Max(0f, currentHeroHealth - prototypeEnemyDps * balance.EnemyDamageMultiplier * Time.deltaTime);

        if (currentEnemyHealth <= 0f)
        {
            ResolveRoom(CombatRoomResolution.Cleared, "Prototype room cleared");
            return;
        }

        if (currentHeroHealth <= 0f)
        {
            ResolveRoom(CombatRoomResolution.Failed, "Prototype hero defeated");
            return;
        }

        if (elapsedSeconds >= maxPrototypeCombatSeconds)
        {
            CombatRoomResolution timeoutResult = currentEnemyHealth <= currentHeroHealth
                ? CombatRoomResolution.Cleared
                : CombatRoomResolution.Failed;
            string message = timeoutResult == CombatRoomResolution.Cleared
                ? "Prototype room cleared by timeout score"
                : "Prototype room failed by timeout score";
            ResolveRoom(timeoutResult, message);
        }
    }

    private bool ResolveRoom(CombatRoomResolution resolution, string message)
    {
        if (resolution == CombatRoomResolution.None)
        {
            return false;
        }

        ResolveExpedition();

        if (expedition == null || !expedition.IsRunning)
        {
            Debug.LogWarning("CombatRoom cannot resolve because no running ExpeditionDirector was found.", this);
            return false;
        }

        resolvingRoom = true;
        bool applied = resolution == CombatRoomResolution.Cleared
            ? expedition.CompleteRoom()
            : expedition.FailExpedition();
        resolvingRoom = false;

        if (!applied)
        {
            return false;
        }

        state = resolution == CombatRoomResolution.Cleared ? CombatRoomState.Cleared : CombatRoomState.Failed;
        SetTrackedEnemiesActive(false);
        SetLastResult(resolution, message);
        Resolved?.Invoke(lastResult);
        NotifyChanged();
        return true;
    }

    private void TryBeginForRunningExpedition()
    {
        if (GameRuntimeRestoreGate.IsRestoring || !startWhenExpeditionRuns || externalStartControl || expedition == null ||
            !expedition.IsSnapshotReady || !expedition.IsRunning)
        {
            return;
        }

        if (state == CombatRoomState.Starting || state == CombatRoomState.Running)
        {
            return;
        }

        BeginRoom();
    }

    private void HandleExpeditionChanged()
    {
        if (resolvingRoom)
        {
            return;
        }

        if (expedition == null)
        {
            return;
        }

        if (expedition.State == DungeonRunState.Ready)
        {
            ResetRoomRuntime();
            return;
        }

        TryBeginForRunningExpedition();
    }

    private void ResetRoomRuntime()
    {
        state = CombatRoomState.Idle;
        activeRoomIndex = -1;
        countdownRemaining = 0f;
        elapsedSeconds = 0f;
        currentHeroHealth = 0f;
        currentEnemyHealth = 0f;
        ClearTrackedEnemySetupBlocker(false);
        ClearRoomTemplateBlocker(false);
        SetTrackedEnemiesActive(false);
        SetLastResult(CombatRoomResolution.None, "Room idle");
        NotifyChanged();
    }

    private float ResolveInitialHeroHealth()
    {
        return heroHealth == null ? prototypeHeroHealth : Mathf.Max(1f, heroHealth.Current);
    }

    private float ResolveInitialEnemyHealth()
    {
        float trackedHealth = 0f;

        for (int i = 0; i < enemyHealths.Length; i++)
        {
            Health enemyHealth = enemyHealths[i];
            if (enemyHealth != null)
            {
                trackedHealth += Mathf.Max(0f, enemyHealth.Current);
            }
        }

        if (trackedHealth > 0f)
        {
            return trackedHealth;
        }

        return prototypeEnemyHealth * DepthBalance.EnemyHealthMultiplier;
    }

    private bool HasAnyEnemyReference()
    {
        for (int i = 0; i < enemyHealths.Length; i++)
        {
            if (enemyHealths[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private void ClearTrackedEnemySetupBlocker(bool notifyChanged)
    {
        if (!HasTrackedEnemySetupBlocker)
        {
            return;
        }

        trackedEnemySetupBlocker = string.Empty;

        if (notifyChanged)
        {
            NotifyChanged();
        }
    }

    private void SetRoomTemplateBlocker(string message)
    {
        string normalizedMessage = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();
        if (string.Equals(roomTemplateBlocker, normalizedMessage, StringComparison.Ordinal))
        {
            return;
        }

        roomTemplateBlocker = normalizedMessage;
        SetLastResult(CombatRoomResolution.None, roomTemplateBlocker);
        NotifyChanged();
    }

    private void ClearRoomTemplateBlocker(bool notifyChanged)
    {
        if (!HasRoomTemplateBlocker)
        {
            return;
        }

        roomTemplateBlocker = string.Empty;
        if (notifyChanged)
        {
            NotifyChanged();
        }
    }

    private void SetLastResult(CombatRoomResolution resolution, string message)
    {
        lastResult = new CombatRoomResult
        {
            resolution = resolution,
            roomIndex = Mathf.Max(0, activeRoomIndex),
            elapsedSeconds = Mathf.Max(0f, elapsedSeconds),
            heroHealthRemaining = Mathf.Max(0f, currentHeroHealth),
            enemyHealthRemaining = Mathf.Max(0f, currentEnemyHealth),
            message = message
        };
    }

    private void ResolveExpedition()
    {
        if (expedition == null && autoFindExpedition)
        {
            expedition = FindAnyObjectByType<ExpeditionDirector>();
        }
    }

    private void ResolveRoomLoader()
    {
        if (roomLoader == null && autoFindRoomLoader)
        {
            roomLoader = FindAnyObjectByType<DungeonRoomLoader>();
        }
    }

    private bool TryValidateActiveRoomTemplate(out string blocker)
    {
        blocker = string.Empty;

        if (!requireLoadedTemplateWhenRoomLoaderIsConfigured || roomLoader == null)
        {
            return true;
        }

        if (!roomLoader.HasLoadedActiveRoom || roomLoader.CurrentTemplate == null)
        {
            blocker = "CombatRoom is waiting for DungeonRoomLoader to load the active room plan.";
            return false;
        }

        if (expedition == null || expedition.RunPlan == null ||
            !string.Equals(roomLoader.CurrentTemplateId, expedition.CurrentRoomTemplateId, StringComparison.Ordinal))
        {
            blocker = "CombatRoom blocked: the loaded room template does not match the active DungeonRunPlan.";
            return false;
        }

        Transform[] enemyAnchors = roomLoader.CurrentTemplate.EnemySpawnAnchors;
        for (int i = 0; i < enemyAnchors.Length; i++)
        {
            if (enemyAnchors[i] != null)
            {
                return true;
            }
        }

        blocker = "CombatRoom blocked: the active DungeonRoomTemplate has no enemy spawn anchor.";
        return false;
    }

    private void ResolveTrackedCombatants()
    {
        if (!autoFindTrackedCombatants)
        {
            return;
        }

        if (heroHealth == null)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                heroHealth = player.GetComponent<Health>();
            }
        }

        if (HasAnyEnemyReference())
        {
            return;
        }

        if (roomLoader != null && requireLoadedTemplateWhenRoomLoaderIsConfigured)
        {
            // Additive dungeon encounters are owned by EnemySpawner. Searching every loaded
            // CharacterActor here can accidentally bind frontline enemies to the room before
            // its template-spawned enemies register.
            return;
        }

        CharacterActor[] actors = FindObjectsByType<CharacterActor>(FindObjectsInactive.Include);
        List<Health> trackedEnemies = new List<Health>(actors.Length);

        for (int i = 0; i < actors.Length; i++)
        {
            CharacterActor actor = actors[i];
            if (actor == null || actor.Team != CharacterTeam.Enemy)
            {
                continue;
            }

            if (actor.TryGetComponent(out Health enemyHealth))
            {
                trackedEnemies.Add(enemyHealth);
            }
        }

        if (trackedEnemies.Count > 0)
        {
            enemyHealths = trackedEnemies.ToArray();
        }
    }

    private void RefillTrackedCombatants()
    {
        if (!refillTrackedCombatantsOnBegin)
        {
            return;
        }

        if (heroHealth != null)
        {
            heroHealth.Refill();
        }

        for (int i = 0; i < enemyHealths.Length; i++)
        {
            Health enemyHealth = enemyHealths[i];
            if (enemyHealth != null)
            {
                enemyHealth.Refill();
            }
        }
    }

    private void SetTrackedEnemiesActive(bool active)
    {
        if (!manageTrackedEnemyActivity)
        {
            return;
        }

        for (int i = 0; i < enemyHealths.Length; i++)
        {
            Health enemyHealth = enemyHealths[i];
            if (enemyHealth == null)
            {
                continue;
            }

            GameObject enemyObject = enemyHealth.gameObject;
            if (enemyObject.activeSelf != active)
            {
                enemyObject.SetActive(active);
            }
        }
    }

    private void SubscribeToExpedition()
    {
        if (subscribedExpedition == expedition)
        {
            return;
        }

        UnsubscribeFromExpedition();

        if (expedition == null)
        {
            return;
        }

        expedition.Changed += HandleExpeditionChanged;
        subscribedExpedition = expedition;
    }

    private void UnsubscribeFromExpedition()
    {
        if (subscribedExpedition == null)
        {
            return;
        }

        subscribedExpedition.Changed -= HandleExpeditionChanged;
        subscribedExpedition = null;
    }

    private void NotifyChanged()
    {
        Changed?.Invoke();
    }
}

public enum CombatRoomState
{
    Idle,
    Starting,
    Running,
    Cleared,
    Failed
}

public enum CombatRoomResolution
{
    None,
    Cleared,
    Failed
}

[Serializable]
public struct CombatRoomResult
{
    public CombatRoomResolution resolution;
    public int roomIndex;
    public float elapsedSeconds;
    public float heroHealthRemaining;
    public float enemyHealthRemaining;
    public string message;
}
