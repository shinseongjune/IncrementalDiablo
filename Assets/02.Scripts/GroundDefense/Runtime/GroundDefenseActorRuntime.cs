using System;
using UnityEngine;

public enum GroundDefenseActorVisualState
{
    Inactive,
    Advancing,
    Defeated,
    WallContact
}

public class GroundDefenseActorRuntime : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DefenseDirector defense;
    [SerializeField] private bool autoFindDefense = true;

    [Header("Actor Model")]
    [SerializeField] private GroundDefenseEnemyArchetype[] actorArchetypes = Array.Empty<GroundDefenseEnemyArchetype>();
    [SerializeField, Min(1)] private int actorCapacity = 8;
    [SerializeField, Min(0)] private int minimumRunningActors = 1;

    [Header("Diagnostics")]
    [SerializeField] private int activeActorCount;
    [SerializeField] private int visibleActorCount;
    [SerializeField] private int totalHitCount;
    [SerializeField] private int totalDefeatCount;
    [SerializeField] private int totalWallContactCount;
    [SerializeField] private int recentHitCount;
    [SerializeField] private int recentDefeatCount;
    [SerializeField] private int recentWallContactCount;
    [SerializeField] private string lastRuntimeMessage = "Defense actors: not initialized";

    private ActorState[] actors = Array.Empty<ActorState>();
    private float spawnPressureBudget;
    private float defenseDamageBudget;
    private float lastObservedElapsed = -1f;
    private DefenseState lastObservedState = DefenseState.Idle;
    private int spawnSequence;

    public int ActorCapacity => actors.Length;
    public int ActiveActorCount => activeActorCount;
    public int VisibleActorCount => visibleActorCount;
    public int TotalHitCount => totalHitCount;
    public int TotalDefeatCount => totalDefeatCount;
    public int TotalWallContactCount => totalWallContactCount;
    public int RecentHitCount => recentHitCount;
    public int RecentDefeatCount => recentDefeatCount;
    public int RecentWallContactCount => recentWallContactCount;
    public string LastRuntimeMessage => lastRuntimeMessage;
    public bool IsReady => defense != null && actors.Length > 0 && HasValidArchetype();
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
        minimumRunningActors = Mathf.Clamp(minimumRunningActors, 0, actorCapacity);

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
        return IsValidIndex(index) && actors[index].visualState == GroundDefenseActorVisualState.Advancing;
    }

    public bool IsActorVisible(int index)
    {
        return IsValidIndex(index) && actors[index].visualState != GroundDefenseActorVisualState.Inactive;
    }

    public GroundDefenseActorVisualState GetActorVisualState(int index)
    {
        return IsValidIndex(index)
            ? actors[index].visualState
            : GroundDefenseActorVisualState.Inactive;
    }

    public GroundDefenseEnemyArchetype GetActorArchetype(int index)
    {
        if (!IsValidIndex(index))
        {
            return null;
        }

        int archetypeIndex = actors[index].archetypeIndex;
        return IsValidArchetypeIndex(archetypeIndex) ? actorArchetypes[archetypeIndex] : null;
    }

    public float GetActorHealthPercent(int index)
    {
        GroundDefenseEnemyArchetype archetype = GetActorArchetype(index);
        if (archetype == null || !IsActorVisible(index))
        {
            return 0f;
        }

        return Mathf.Clamp01(actors[index].health / archetype.MaxHealth);
    }

    public float GetActorTravelPercent(int index)
    {
        return IsValidIndex(index) ? Mathf.Clamp01(actors[index].travelPercent) : 0f;
    }

    public float GetActorFeedbackPercent(int index)
    {
        if (!IsValidIndex(index))
        {
            return 0f;
        }

        ActorState actor = actors[index];
        GroundDefenseEnemyArchetype archetype = GetActorArchetype(index);
        if (archetype == null)
        {
            return 0f;
        }

        float duration = actor.visualState switch
        {
            GroundDefenseActorVisualState.Defeated => archetype.DefeatFeedbackSeconds,
            GroundDefenseActorVisualState.WallContact => archetype.WallContactFeedbackSeconds,
            _ => archetype.HitFeedbackSeconds
        };
        return duration <= 0f ? 0f : Mathf.Clamp01(actor.feedbackRemaining / duration);
    }

    public bool IsActorUnderFire(int index)
    {
        return IsActorActive(index) && actors[index].feedbackRemaining > 0f;
    }

    public int GetLeadingActorIndex()
    {
        int leadingIndex = -1;
        float leadingTravel = float.MinValue;

        for (int i = 0; i < actors.Length; i++)
        {
            if (!IsActorActive(i) || actors[i].travelPercent <= leadingTravel)
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
            visibleActorCount = 0;
            lastRuntimeMessage = "Defense actors: no DefenseDirector";
            return;
        }

        if (!HasValidArchetype())
        {
            DeactivateAllActors();
            activeActorCount = 0;
            visibleActorCount = 0;
            lastRuntimeMessage = "Defense actors: no production archetype";
            return;
        }

        DefenseRuntimeState runtime = defense.Runtime;
        if (lastObservedElapsed >= 0f && runtime.TotalElapsed + 0.001f < lastObservedElapsed)
        {
            ResetActorStates();
        }

        float safeDeltaTime = Mathf.Max(0f, deltaTime);
        TickFeedback(safeDeltaTime);

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

        activeActorCount = CountActors(GroundDefenseActorVisualState.Advancing);
        visibleActorCount = CountVisibleActors();
        lastObservedElapsed = runtime.TotalElapsed;
        lastObservedState = runtime.State;
        string leadingHealthText = activeActorCount <= 0
            ? "none"
            : $"{Mathf.RoundToInt(LeadingActorHealthPercent * 100f)}%";
        lastRuntimeMessage =
            $"Defense actors: {runtime.State} / active {activeActorCount} / visible {visibleActorCount}/{actors.Length} / lead HP {leadingHealthText} / hits {totalHitCount} / defeats {totalDefeatCount} / contacts {totalWallContactCount}";
    }

    private void TickRunningActors(DefenseRuntimeState runtime, float deltaTime)
    {
        if (lastObservedState != DefenseState.Holding && lastObservedState != DefenseState.Pushing)
        {
            spawnPressureBudget = 0f;
        }

        int requiredMinimum = Mathf.Min(minimumRunningActors, actors.Length);
        while (CountActors(GroundDefenseActorVisualState.Advancing) < requiredMinimum &&
               TryGetNextArchetypeIndex(out int minimumArchetypeIndex) &&
               TrySpawnActor(minimumArchetypeIndex))
        {
        }

        float maxBudget = GetMaxPressurePerSpawn() * Mathf.Max(1, actors.Length) * 2f;
        spawnPressureBudget = Mathf.Min(
            spawnPressureBudget + runtime.LastIncomingPressurePerSecond * deltaTime,
            maxBudget);

        while (TryGetNextArchetypeIndex(out int archetypeIndex))
        {
            GroundDefenseEnemyArchetype archetype = actorArchetypes[archetypeIndex];
            if (spawnPressureBudget + 0.0001f < archetype.PressurePerSpawn ||
                !TrySpawnActor(archetypeIndex))
            {
                break;
            }

            spawnPressureBudget -= archetype.PressurePerSpawn;
        }

        TickDefenseHits(runtime.LastPressureClearedPerSecond * deltaTime);
        AdvanceActors(runtime, deltaTime);
    }

    private void TickDefenseHits(float damage)
    {
        defenseDamageBudget = Mathf.Min(
            defenseDamageBudget + Mathf.Max(0f, damage),
            GetMaxActorHealth() * Mathf.Max(1, actors.Length));

        while (true)
        {
            int targetIndex = GetLeadingActorIndex();
            if (targetIndex < 0)
            {
                return;
            }

            GroundDefenseEnemyArchetype archetype = GetActorArchetype(targetIndex);
            if (archetype == null || defenseDamageBudget + 0.0001f < archetype.DamagePerHit)
            {
                return;
            }

            ActorState actor = actors[targetIndex];
            actor.health -= Mathf.Min(actor.health, archetype.DamagePerHit);
            actor.feedbackRemaining = archetype.HitFeedbackSeconds;
            defenseDamageBudget -= archetype.DamagePerHit;
            recentHitCount += 1;
            totalHitCount += 1;

            if (actor.health <= 0.0001f)
            {
                actor.health = 0f;
                actor.visualState = GroundDefenseActorVisualState.Defeated;
                actor.feedbackRemaining = archetype.DefeatFeedbackSeconds;
                recentDefeatCount += 1;
                totalDefeatCount += 1;
            }

            actors[targetIndex] = actor;
        }
    }

    private void AdvanceActors(DefenseRuntimeState runtime, float deltaTime)
    {
        bool canReachWall = runtime.LastWallDamagePerSecond > 0.0001f;
        float travelLimit = canReachWall ? 1f : 0.9f;

        for (int i = 0; i < actors.Length; i++)
        {
            ActorState actor = actors[i];
            if (actor.visualState != GroundDefenseActorVisualState.Advancing)
            {
                continue;
            }

            GroundDefenseEnemyArchetype archetype = GetActorArchetype(i);
            if (archetype == null)
            {
                actors[i] = default;
                continue;
            }

            float advancePerSecond =
                archetype.BaseAdvancePerSecond +
                runtime.PressurePercent * archetype.PressureAdvancePerSecond;
            actor.travelPercent = Mathf.Min(
                travelLimit,
                actor.travelPercent + advancePerSecond * deltaTime);

            if (canReachWall && actor.travelPercent >= 0.999f)
            {
                actor.health = 0f;
                actor.travelPercent = 1f;
                actor.visualState = GroundDefenseActorVisualState.WallContact;
                actor.feedbackRemaining = archetype.WallContactFeedbackSeconds;
                recentWallContactCount += 1;
                totalWallContactCount += 1;
            }

            actors[i] = actor;
        }
    }

    private bool TrySpawnActor(int archetypeIndex)
    {
        if (!IsValidArchetypeIndex(archetypeIndex))
        {
            return false;
        }

        for (int i = 0; i < actors.Length; i++)
        {
            if (actors[i].visualState != GroundDefenseActorVisualState.Inactive)
            {
                continue;
            }

            GroundDefenseEnemyArchetype archetype = actorArchetypes[archetypeIndex];
            actors[i] = new ActorState
            {
                visualState = GroundDefenseActorVisualState.Advancing,
                archetypeIndex = archetypeIndex,
                health = archetype.MaxHealth,
                travelPercent = 0f,
                feedbackRemaining = 0f
            };
            spawnSequence += 1;
            return true;
        }

        return false;
    }

    private void FillBreachedActors()
    {
        for (int i = 0; i < actors.Length; i++)
        {
            ActorState actor = actors[i];
            if (!IsValidArchetypeIndex(actor.archetypeIndex) &&
                !TryGetNextArchetypeIndex(out actor.archetypeIndex))
            {
                actors[i] = default;
                continue;
            }

            GroundDefenseEnemyArchetype archetype = actorArchetypes[actor.archetypeIndex];
            actor.visualState = GroundDefenseActorVisualState.Advancing;
            actor.health = Mathf.Max(actor.health, archetype.MaxHealth);
            actor.travelPercent = 1f;
            actor.feedbackRemaining = 0f;
            actors[i] = actor;
        }
    }

    private void TickFeedback(float deltaTime)
    {
        for (int i = 0; i < actors.Length; i++)
        {
            ActorState actor = actors[i];
            actor.feedbackRemaining = Mathf.Max(0f, actor.feedbackRemaining - deltaTime);

            if ((actor.visualState == GroundDefenseActorVisualState.Defeated ||
                 actor.visualState == GroundDefenseActorVisualState.WallContact) &&
                actor.feedbackRemaining <= 0f)
            {
                actors[i] = default;
                continue;
            }

            actors[i] = actor;
        }
    }

    private bool TryGetNextArchetypeIndex(out int archetypeIndex)
    {
        archetypeIndex = -1;
        int totalWeight = 0;
        for (int i = 0; i < actorArchetypes.Length; i++)
        {
            if (actorArchetypes[i] != null)
            {
                totalWeight += actorArchetypes[i].SpawnWeight;
            }
        }

        if (totalWeight <= 0)
        {
            return false;
        }

        int selection = spawnSequence % totalWeight;
        for (int i = 0; i < actorArchetypes.Length; i++)
        {
            GroundDefenseEnemyArchetype archetype = actorArchetypes[i];
            if (archetype == null)
            {
                continue;
            }

            if (selection < archetype.SpawnWeight)
            {
                archetypeIndex = i;
                return true;
            }

            selection -= archetype.SpawnWeight;
        }

        return false;
    }

    private float GetMaxActorHealth()
    {
        float maxHealth = 1f;
        for (int i = 0; i < actorArchetypes.Length; i++)
        {
            if (actorArchetypes[i] != null)
            {
                maxHealth = Mathf.Max(maxHealth, actorArchetypes[i].MaxHealth);
            }
        }

        return maxHealth;
    }

    private float GetMaxPressurePerSpawn()
    {
        float maxPressure = 1f;
        for (int i = 0; i < actorArchetypes.Length; i++)
        {
            if (actorArchetypes[i] != null)
            {
                maxPressure = Mathf.Max(maxPressure, actorArchetypes[i].PressurePerSpawn);
            }
        }

        return maxPressure;
    }

    private bool HasValidArchetype()
    {
        if (actorArchetypes == null)
        {
            return false;
        }

        for (int i = 0; i < actorArchetypes.Length; i++)
        {
            if (actorArchetypes[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsValidArchetypeIndex(int index)
    {
        return actorArchetypes != null &&
               index >= 0 &&
               index < actorArchetypes.Length &&
               actorArchetypes[index] != null;
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
        spawnPressureBudget = 0f;
        defenseDamageBudget = 0f;
        lastObservedElapsed = -1f;
        lastObservedState = DefenseState.Idle;
        spawnSequence = 0;
        activeActorCount = 0;
        visibleActorCount = 0;
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

    private int CountActors(GroundDefenseActorVisualState visualState)
    {
        int count = 0;
        for (int i = 0; i < actors.Length; i++)
        {
            if (actors[i].visualState == visualState)
            {
                count += 1;
            }
        }

        return count;
    }

    private int CountVisibleActors()
    {
        int count = 0;
        for (int i = 0; i < actors.Length; i++)
        {
            if (actors[i].visualState != GroundDefenseActorVisualState.Inactive)
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
        public GroundDefenseActorVisualState visualState;
        public int archetypeIndex;
        public float health;
        public float travelPercent;
        public float feedbackRemaining;
    }
}
