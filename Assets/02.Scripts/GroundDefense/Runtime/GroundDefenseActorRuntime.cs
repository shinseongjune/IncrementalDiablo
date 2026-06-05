using System;
using UnityEngine;

public class GroundDefenseActorRuntime : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DefenseDirector defense;
    [SerializeField] private bool autoFindDefense = true;

    [Header("Actor Model")]
    [SerializeField, Min(1)] private int actorCapacity = 3;
    [SerializeField, Min(0.01f)] private float actorMaxHealth = 12f;
    [SerializeField, Min(0.01f)] private float pressurePerSpawn = 8f;
    [SerializeField, Min(0.01f)] private float damagePerHit = 3f;
    [SerializeField, Min(0)] private int minimumRunningActors = 1;
    [SerializeField, Min(0f)] private float baseAdvancePerSecond = 0.1f;
    [SerializeField, Min(0f)] private float pressureAdvancePerSecond = 0.12f;
    [SerializeField, Min(0.01f)] private float hitFeedbackSeconds = 0.16f;

    [Header("Diagnostics")]
    [SerializeField] private int activeActorCount;
    [SerializeField] private int totalHitCount;
    [SerializeField] private int totalDefeatCount;
    [SerializeField] private int totalWallContactCount;
    [SerializeField] private int recentHitCount;
    [SerializeField] private int recentDefeatCount;
    [SerializeField] private int recentWallContactCount;
    [SerializeField] private string lastRuntimeMessage = "Defense actors: not initialized";

    private ActorState[] actors = Array.Empty<ActorState>();
    private float spawnBudget;
    private float defenseDamageBudget;
    private float lastObservedElapsed = -1f;
    private DefenseState lastObservedState = DefenseState.Idle;

    public int ActorCapacity => actors.Length;
    public int ActiveActorCount => activeActorCount;
    public int TotalHitCount => totalHitCount;
    public int TotalDefeatCount => totalDefeatCount;
    public int TotalWallContactCount => totalWallContactCount;
    public int RecentHitCount => recentHitCount;
    public int RecentDefeatCount => recentDefeatCount;
    public int RecentWallContactCount => recentWallContactCount;
    public string LastRuntimeMessage => lastRuntimeMessage;
    public bool IsReady => defense != null && actors.Length > 0;
    public float LeadingActorHealthPercent
    {
        get
        {
            int leadingIndex = GetLeadingActorIndex();
            return leadingIndex < 0 ? 0f : GetActorHealthPercent(leadingIndex);
        }
    }

    private void Reset()
    {
        ResolveReferences(true);
        EnsureActorStorage(true);
    }

    private void Awake()
    {
        ResolveReferences();
        EnsureActorStorage(true);
    }

    private void OnEnable()
    {
        ResolveReferences();
        EnsureActorStorage(false);
        ResetActorStates();
    }

    private void OnValidate()
    {
        actorCapacity = Mathf.Max(1, actorCapacity);
        actorMaxHealth = Mathf.Max(0.01f, actorMaxHealth);
        pressurePerSpawn = Mathf.Max(0.01f, pressurePerSpawn);
        damagePerHit = Mathf.Max(0.01f, damagePerHit);
        minimumRunningActors = Mathf.Clamp(minimumRunningActors, 0, actorCapacity);
        baseAdvancePerSecond = Mathf.Max(0f, baseAdvancePerSecond);
        pressureAdvancePerSecond = Mathf.Max(0f, pressureAdvancePerSecond);
        hitFeedbackSeconds = Mathf.Max(0.01f, hitFeedbackSeconds);

        if (!Application.isPlaying)
        {
            EnsureActorStorage(false);
        }
    }

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    public bool IsActorActive(int index)
    {
        return IsValidIndex(index) && actors[index].active;
    }

    public float GetActorHealthPercent(int index)
    {
        if (!IsValidIndex(index) || !actors[index].active)
        {
            return 0f;
        }

        return Mathf.Clamp01(actors[index].health / actorMaxHealth);
    }

    public float GetActorTravelPercent(int index)
    {
        return IsValidIndex(index) ? Mathf.Clamp01(actors[index].travelPercent) : 0f;
    }

    public bool IsActorUnderFire(int index)
    {
        return IsValidIndex(index) && actors[index].active && actors[index].hitFeedbackRemaining > 0f;
    }

    public int GetLeadingActorIndex()
    {
        int leadingIndex = -1;
        float leadingTravel = float.MinValue;

        for (int i = 0; i < actors.Length; i++)
        {
            if (!actors[i].active || actors[i].travelPercent <= leadingTravel)
            {
                continue;
            }

            leadingTravel = actors[i].travelPercent;
            leadingIndex = i;
        }

        return leadingIndex;
    }

    private void Tick(float deltaTime)
    {
        ResolveReferences();
        EnsureActorStorage(false);
        ClearRecentEvents();

        if (defense == null || defense.Runtime == null)
        {
            activeActorCount = 0;
            lastRuntimeMessage = "Defense actors: no DefenseDirector";
            return;
        }

        DefenseRuntimeState runtime = defense.Runtime;
        if (lastObservedElapsed >= 0f && runtime.TotalElapsed + 0.001f < lastObservedElapsed)
        {
            ResetActorStates();
        }

        float safeDeltaTime = Mathf.Max(0f, deltaTime);
        TickHitFeedback(safeDeltaTime);

        if (runtime.State == DefenseState.Breached)
        {
            FillBreachedActors();
        }
        else if (runtime.IsRunning)
        {
            TickRunningActors(runtime, safeDeltaTime);
        }
        else
        {
            DeactivateAllActors();
        }

        activeActorCount = CountActiveActors();
        lastObservedElapsed = runtime.TotalElapsed;
        lastObservedState = runtime.State;
        string leadingHealthText = activeActorCount <= 0
            ? "none"
            : $"{Mathf.RoundToInt(LeadingActorHealthPercent * 100f)}%";
        lastRuntimeMessage =
            $"Defense actors: {runtime.State} / active {activeActorCount}/{actors.Length} / lead HP {leadingHealthText} / hits {totalHitCount} / defeats {totalDefeatCount} / contacts {totalWallContactCount}";
    }

    private void TickRunningActors(DefenseRuntimeState runtime, float deltaTime)
    {
        if (lastObservedState != DefenseState.Holding && lastObservedState != DefenseState.Pushing)
        {
            spawnBudget = 0f;
        }

        int requiredMinimum = Mathf.Min(minimumRunningActors, actors.Length);
        while (CountActiveActors() < requiredMinimum && TrySpawnActor())
        {
        }

        spawnBudget = Mathf.Min(
            spawnBudget + runtime.LastIncomingPressurePerSecond * deltaTime / pressurePerSpawn,
            actors.Length * 2f);
        while (spawnBudget >= 1f && TrySpawnActor())
        {
            spawnBudget -= 1f;
        }

        TickDefenseHits(runtime.LastPressureClearedPerSecond * deltaTime);
        AdvanceActors(runtime, deltaTime);
    }

    private void TickDefenseHits(float damage)
    {
        defenseDamageBudget = Mathf.Min(
            defenseDamageBudget + Mathf.Max(0f, damage),
            actorMaxHealth * Mathf.Max(1, actors.Length));

        while (defenseDamageBudget >= damagePerHit)
        {
            int targetIndex = GetLeadingActorIndex();
            if (targetIndex < 0)
            {
                return;
            }

            ActorState actor = actors[targetIndex];
            float appliedDamage = Mathf.Min(actor.health, damagePerHit);
            actor.health -= appliedDamage;
            actor.hitFeedbackRemaining = hitFeedbackSeconds;
            defenseDamageBudget -= damagePerHit;
            recentHitCount += 1;
            totalHitCount += 1;

            if (actor.health <= 0.0001f)
            {
                actor.active = false;
                actor.health = 0f;
                actor.travelPercent = 0f;
                actor.hitFeedbackRemaining = 0f;
                recentDefeatCount += 1;
                totalDefeatCount += 1;
            }

            actors[targetIndex] = actor;
        }
    }

    private void AdvanceActors(DefenseRuntimeState runtime, float deltaTime)
    {
        float advancePerSecond = baseAdvancePerSecond + runtime.PressurePercent * pressureAdvancePerSecond;
        bool canReachWall = runtime.LastWallDamagePerSecond > 0.0001f;
        float travelLimit = canReachWall ? 1f : 0.9f;

        for (int i = 0; i < actors.Length; i++)
        {
            ActorState actor = actors[i];
            if (!actor.active)
            {
                continue;
            }

            actor.travelPercent = Mathf.Min(travelLimit, actor.travelPercent + advancePerSecond * deltaTime);
            if (canReachWall && actor.travelPercent >= 0.999f)
            {
                actor.active = false;
                actor.health = 0f;
                actor.travelPercent = 0f;
                actor.hitFeedbackRemaining = 0f;
                recentWallContactCount += 1;
                totalWallContactCount += 1;
            }

            actors[i] = actor;
        }
    }

    private bool TrySpawnActor()
    {
        for (int i = 0; i < actors.Length; i++)
        {
            if (actors[i].active)
            {
                continue;
            }

            actors[i] = new ActorState
            {
                active = true,
                health = actorMaxHealth,
                travelPercent = 0f,
                hitFeedbackRemaining = 0f
            };
            return true;
        }

        return false;
    }

    private void FillBreachedActors()
    {
        for (int i = 0; i < actors.Length; i++)
        {
            ActorState actor = actors[i];
            actor.active = true;
            actor.health = Mathf.Max(actor.health, actorMaxHealth);
            actor.travelPercent = 1f;
            actor.hitFeedbackRemaining = 0f;
            actors[i] = actor;
        }
    }

    private void TickHitFeedback(float deltaTime)
    {
        for (int i = 0; i < actors.Length; i++)
        {
            ActorState actor = actors[i];
            actor.hitFeedbackRemaining = Mathf.Max(0f, actor.hitFeedbackRemaining - deltaTime);
            actors[i] = actor;
        }
    }

    private void ClearRecentEvents()
    {
        recentHitCount = 0;
        recentDefeatCount = 0;
        recentWallContactCount = 0;
    }

    private void ResetActorStates()
    {
        EnsureActorStorage(false);
        DeactivateAllActors();
        spawnBudget = 0f;
        defenseDamageBudget = 0f;
        lastObservedElapsed = -1f;
        lastObservedState = DefenseState.Idle;
        activeActorCount = 0;
        totalHitCount = 0;
        totalDefeatCount = 0;
        totalWallContactCount = 0;
        recentHitCount = 0;
        recentDefeatCount = 0;
        recentWallContactCount = 0;
        lastRuntimeMessage = "Defense actors: reset";
    }

    private void DeactivateAllActors()
    {
        for (int i = 0; i < actors.Length; i++)
        {
            actors[i] = default;
        }
    }

    private void EnsureActorStorage(bool forceReset)
    {
        int safeCapacity = Mathf.Max(1, actorCapacity);
        if (!forceReset && actors != null && actors.Length == safeCapacity)
        {
            return;
        }

        actors = new ActorState[safeCapacity];
    }

    private int CountActiveActors()
    {
        int count = 0;
        for (int i = 0; i < actors.Length; i++)
        {
            if (actors[i].active)
            {
                count += 1;
            }
        }

        return count;
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < actors.Length;
    }

    private void ResolveReferences(bool force = false)
    {
        if ((autoFindDefense || force) && (defense == null || force))
        {
            defense = GetComponent<DefenseDirector>();
            if (defense == null)
            {
                defense = FindAnyObjectByType<DefenseDirector>();
            }
        }
    }

    private struct ActorState
    {
        public bool active;
        public float health;
        public float travelPercent;
        public float hitFeedbackRemaining;
    }
}
