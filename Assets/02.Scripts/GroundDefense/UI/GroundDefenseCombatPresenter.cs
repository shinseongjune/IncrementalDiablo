using UnityEngine;

public class GroundDefenseCombatPresenter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DefenseDirector defense;
    [SerializeField] private GroundDefenseActorRuntime actorRuntime;
    [SerializeField] private GroundDefenseEnemyPool enemyPool;
    [SerializeField] private GroundDefenseBattlefieldView battlefieldView;
    [SerializeField] private bool autoFindDefense = true;
    [SerializeField] private bool autoFindActorRuntime = true;
    [SerializeField] private bool autoFindEnemyPool = true;
    [SerializeField] private bool autoFindBattlefieldView = true;

    [Header("Lane Anchors")]
    [SerializeField] private Transform enemySpawnAnchor;
    [SerializeField] private Transform wallAnchor;
    [SerializeField] private Transform attackOrigin;

    [Header("Pooled Pressure Actors")]
    [SerializeField] private bool usePooledEnemies = true;
    [SerializeField] private bool useProductionBattlefield = true;

    [Header("Legacy Fixed Pressure Actors")]
    [SerializeField] private Transform[] pressureActors;
    [SerializeField] private bool showPressureActors;
    [SerializeField] private int minimumRunningActors = 1;
    [SerializeField] private float pressureActorCyclesPerSecond = 0.16f;
    [SerializeField] private Color pressureActorColor = new Color(0.82f, 0.18f, 0.12f);
    [SerializeField] private Color pressureActorUnderFireColor = new Color(1f, 0.68f, 0.18f);

    [Header("Wall Contact Feedback")]
    [SerializeField] private GameObject wallContactObject;
    [SerializeField] private Renderer wallContactRenderer;
    [SerializeField] private float wallContactFlashSeconds = 0.22f;
    [SerializeField] private float wallContactScaleMultiplier = 1.2f;
    [SerializeField] private Color wallContactColor = new Color(1f, 0.25f, 0.1f);

    [Header("Tower And Defender Feedback")]
    [SerializeField] private Transform[] attackPulses;
    [SerializeField] private bool showAttackPulses = true;
    [SerializeField] private float attackPulseCyclesPerSecond = 0.85f;
    [SerializeField] private float defensePowerPerVisiblePulse = 8f;
    [SerializeField] private Color attackPulseColor = new Color(0.45f, 0.9f, 1f);
    [SerializeField, Min(0.05f)] private float attackBoltLength = 0.8f;
    [SerializeField, Min(0.01f)] private float attackBoltThickness = 0.08f;

    [Header("Refresh")]
    [SerializeField] private bool refreshEveryFrame = true;

    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock propertyBlock;
    private GroundDefenseEnemyView[] pooledActorViews = new GroundDefenseEnemyView[0];
    private Vector3 baseWallContactScale = Vector3.one;
    private float lastWallHealth;
    private float wallContactFlashRemaining;
    private bool hasWallSnapshot;
    private DefenseState lastState = DefenseState.Idle;
    private int lastActorWallContactCount;
    private GroundDefenseActorRuntime subscribedActorRuntime;

    public int ActivePressureActorCount { get; private set; }
    public int ActiveAttackPulseCount { get; private set; }
    public int WallContactEventCount { get; private set; }
    public string LastCombatMessage { get; private set; } = "Ground combat visuals: not refreshed";

    private void Reset()
    {
        ResolveReferences(true);
    }

    private void Awake()
    {
        ResolveReferences();
        CaptureBaseScales();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SyncActorRuntimeSubscription();

        if (defense != null)
        {
            defense.Changed += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        SyncActorRuntimeSubscription(true);

        if (defense != null)
        {
            defense.Changed -= Refresh;
        }

        ReleasePooledViews();
    }

    private void OnValidate()
    {
        minimumRunningActors = Mathf.Max(0, minimumRunningActors);
        pressureActorCyclesPerSecond = Mathf.Max(0f, pressureActorCyclesPerSecond);
        wallContactFlashSeconds = Mathf.Max(0.01f, wallContactFlashSeconds);
        wallContactScaleMultiplier = Mathf.Max(1f, wallContactScaleMultiplier);
        attackPulseCyclesPerSecond = Mathf.Max(0f, attackPulseCyclesPerSecond);
        defensePowerPerVisiblePulse = Mathf.Max(0.01f, defensePowerPerVisiblePulse);
        attackBoltLength = Mathf.Max(0.05f, attackBoltLength);
        attackBoltThickness = Mathf.Max(0.01f, attackBoltThickness);
    }

    private void Update()
    {
        if (refreshEveryFrame)
        {
            Refresh();
        }
    }

    public void Refresh()
    {
        ResolveReferences();
        SyncActorRuntimeSubscription();

        if (defense == null || defense.Runtime == null)
        {
            ActivePressureActorCount = 0;
            ActiveAttackPulseCount = 0;
            LastCombatMessage = "Ground combat visuals: no DefenseDirector";
            ReleasePooledViews();
            SetAllActive(pressureActors, false);
            SetAllActive(attackPulses, false);
            SetWallContactVisible(false);
            return;
        }

        DefenseRuntimeState runtime = defense.Runtime;
        UpdateWallContact(runtime);
        UpdatePressureActors(runtime);
        UpdateAttackPulses(runtime);
        UpdateCombatMessage(runtime);
    }

    private void UpdatePressureActors(DefenseRuntimeState runtime)
    {
        ActivePressureActorCount = 0;

        if (enemySpawnAnchor == null || wallAnchor == null)
        {
            ReleasePooledViews();
            SetAllActive(pressureActors, false);
            return;
        }

        if (usePooledEnemies)
        {
            SetAllActive(pressureActors, false);
            UpdatePooledPressureActors();
            return;
        }

        ReleasePooledViews();
        UpdateLegacyPressureActors(runtime);
    }

    private void UpdatePooledPressureActors()
    {
        if (UseProductionBattlefield && !battlefieldView.UsesRuntimeEnemies)
        {
            ReleasePooledViews();
            ActivePressureActorCount = battlefieldView.VisibleEnemyCount;
            return;
        }

        if (actorRuntime == null || !actorRuntime.IsReady || enemyPool == null || !enemyPool.IsReady)
        {
            ReleasePooledViews();
            return;
        }

        EnsurePooledViewStorage();
        for (int i = 0; i < pooledActorViews.Length; i++)
        {
            if (!actorRuntime.IsActorVisible(i))
            {
                ReturnPooledView(i);
                continue;
            }

            GroundDefenseEnemyArchetype archetype = actorRuntime.GetActorArchetype(i);
            GroundDefenseEnemyView view = pooledActorViews[i];
            if (view != null && view.Archetype != archetype)
            {
                ReturnPooledView(i);
                view = null;
            }

            if (view == null)
            {
                view = enemyPool.Rent(archetype);
                pooledActorViews[i] = view;
            }

            if (view == null)
            {
                continue;
            }

            float travelPercent = actorRuntime.GetActorTravelPercent(i);
            Vector3 position = UseProductionBattlefield
                ? battlefieldView.GetEnemyWorldPosition(i, travelPercent)
                : Vector3.Lerp(enemySpawnAnchor.position, wallAnchor.position, travelPercent);
            view.Apply(
                actorRuntime.GetActorVisualState(i),
                position,
                actorRuntime.GetActorHealthPercent(i),
                actorRuntime.IsActorUnderFire(i),
                actorRuntime.GetActorFeedbackPercent(i));
            ActivePressureActorCount += 1;
        }
    }

    private void UpdateLegacyPressureActors(DefenseRuntimeState runtime)
    {
        if (!showPressureActors || pressureActors == null || pressureActors.Length == 0)
        {
            SetAllActive(pressureActors, false);
            return;
        }

        int validActorCount = CountAssigned(pressureActors);
        bool shouldShowActors = runtime.IsRunning || runtime.State == DefenseState.Breached;
        if (!shouldShowActors || validActorCount == 0)
        {
            SetAllActive(pressureActors, false);
            return;
        }

        int minimumActors = runtime.State == DefenseState.Breached
            ? validActorCount
            : Mathf.Min(minimumRunningActors, validActorCount);
        int activeTarget = Mathf.Clamp(
            Mathf.CeilToInt(Mathf.Lerp(minimumActors, validActorCount, runtime.PressurePercent)),
            0,
            validActorCount);

        float flowPhase = runtime.TotalElapsed * pressureActorCyclesPerSecond;
        int validActorIndex = 0;
        int activeActorIndex = 0;

        for (int i = 0; i < pressureActors.Length; i++)
        {
            Transform actor = pressureActors[i];
            if (actor == null)
            {
                continue;
            }

            bool active = validActorIndex < activeTarget;
            SetActive(actor.gameObject, active);

            if (active)
            {
                float spacing = activeTarget <= 1 ? 0f : activeActorIndex / (float)activeTarget;
                float travelPercent = runtime.State == DefenseState.Breached
                    ? 1f
                    : Mathf.Repeat(flowPhase + spacing, 1f);
                actor.position = Vector3.Lerp(enemySpawnAnchor.position, wallAnchor.position, travelPercent);
                SetRendererColor(actor.GetComponentInChildren<Renderer>(), GetPressureActorColor(runtime, travelPercent));
                ActivePressureActorCount += 1;
                activeActorIndex += 1;
            }

            validActorIndex += 1;
        }
    }

    private void UpdateWallContact(DefenseRuntimeState runtime)
    {
        if (!hasWallSnapshot)
        {
            lastWallHealth = runtime.WallHealth;
            lastState = runtime.State;
            lastActorWallContactCount = actorRuntime == null ? 0 : actorRuntime.TotalWallContactCount;
            hasWallSnapshot = true;
        }

        bool tookWallDamage = runtime.WallHealth < lastWallHealth - 0.001f;
        bool becameBreached = lastState != DefenseState.Breached && runtime.State == DefenseState.Breached;
        bool actorReachedWall = actorRuntime != null &&
                                actorRuntime.TotalWallContactCount > lastActorWallContactCount;
        if (tookWallDamage || becameBreached || actorReachedWall)
        {
            wallContactFlashRemaining = wallContactFlashSeconds;
            WallContactEventCount += 1;
        }

        if (UseProductionBattlefield)
        {
            battlefieldView.ApplyWallState(
                runtime.WallHealthPercent,
                tookWallDamage || actorReachedWall,
                runtime.State == DefenseState.Breached);
            wallContactFlashRemaining = 0f;
            SetWallContactVisible(false);
            lastWallHealth = runtime.WallHealth;
            lastState = runtime.State;
            lastActorWallContactCount = actorRuntime == null ? 0 : actorRuntime.TotalWallContactCount;
            return;
        }

        if (Application.isPlaying)
        {
            wallContactFlashRemaining = Mathf.Max(0f, wallContactFlashRemaining - Time.deltaTime);
        }

        bool showContact = wallContactFlashRemaining > 0f || runtime.State == DefenseState.Breached;
        SetWallContactVisible(showContact);

        if (showContact)
        {
            UpdateWallContactTransform();
            SetRendererColor(wallContactRenderer, wallContactColor);
        }

        lastWallHealth = runtime.WallHealth;
        lastState = runtime.State;
        lastActorWallContactCount = actorRuntime == null ? 0 : actorRuntime.TotalWallContactCount;
    }

    private void UpdateWallContactTransform()
    {
        Transform contactTransform = null;
        if (wallContactObject != null)
        {
            contactTransform = wallContactObject.transform;
        }
        else if (wallContactRenderer != null)
        {
            contactTransform = wallContactRenderer.transform;
        }

        if (contactTransform == null)
        {
            return;
        }

        if (wallAnchor != null)
        {
            contactTransform.position = wallAnchor.position;
        }

        float pulsePercent = wallContactFlashSeconds <= 0f
            ? 0f
            : Mathf.Clamp01(wallContactFlashRemaining / wallContactFlashSeconds);
        float pulseScale = Mathf.Lerp(1f, wallContactScaleMultiplier, pulsePercent);
        contactTransform.localScale = baseWallContactScale * pulseScale;
    }

    private void UpdateAttackPulses(DefenseRuntimeState runtime)
    {
        ActiveAttackPulseCount = 0;

        if (UseProductionBattlefield)
        {
            SetAllActive(attackPulses, false);
            ActiveAttackPulseCount = battlefieldView.ActiveProjectileCount;
            return;
        }

        if (!showAttackPulses || attackPulses == null || attackPulses.Length == 0)
        {
            return;
        }

        if (!runtime.IsRunning || attackOrigin == null)
        {
            SetAllActive(attackPulses, false);
            return;
        }

        int validPulseCount = CountAssigned(attackPulses);
        if (validPulseCount == 0)
        {
            return;
        }

        float defensePower = defense.Upgrades == null ? 0f : defense.Upgrades.TotalDefensePower;
        float visibleDefenseRate = Mathf.Max(defensePower, runtime.LastPressureClearedPerSecond);
        int activeTarget = Mathf.Clamp(
            Mathf.CeilToInt(visibleDefenseRate / defensePowerPerVisiblePulse),
            1,
            validPulseCount);
        Vector3 targetPosition = GetAttackTargetPosition();
        float flowPhase = runtime.TotalElapsed * attackPulseCyclesPerSecond;
        int validPulseIndex = 0;
        int activePulseIndex = 0;

        for (int i = 0; i < attackPulses.Length; i++)
        {
            Transform pulse = attackPulses[i];
            if (pulse == null)
            {
                continue;
            }

            bool active = validPulseIndex < activeTarget;
            SetActive(pulse.gameObject, active);

            if (active)
            {
                float spacing = activeTarget <= 1 ? 0f : activePulseIndex / (float)activeTarget;
                float travelPercent = Mathf.Repeat(flowPhase + spacing, 1f);
                pulse.position = Vector3.Lerp(attackOrigin.position, targetPosition, travelPercent);
                OrientAttackBolt(pulse, targetPosition - attackOrigin.position);
                SetRendererColor(pulse.GetComponentInChildren<Renderer>(), attackPulseColor);
                ActiveAttackPulseCount += 1;
                activePulseIndex += 1;
            }

            validPulseIndex += 1;
        }
    }

    private void OrientAttackBolt(Transform pulse, Vector3 attackDirection)
    {
        if (pulse == null)
        {
            return;
        }

        if (attackDirection.sqrMagnitude > 0.0001f)
        {
            pulse.rotation = Quaternion.FromToRotation(Vector3.up, attackDirection.normalized);
        }

        pulse.localScale = new Vector3(
            attackBoltThickness,
            attackBoltLength * 0.5f,
            attackBoltThickness);
    }

    private Vector3 GetAttackTargetPosition()
    {
        Transform leadingActor = null;
        float bestDistanceToWall = float.MaxValue;

        if (usePooledEnemies && pooledActorViews != null && wallAnchor != null)
        {
            for (int i = 0; i < pooledActorViews.Length; i++)
            {
                GroundDefenseEnemyView view = pooledActorViews[i];
                if (view == null ||
                    !view.gameObject.activeSelf ||
                    actorRuntime == null ||
                    !actorRuntime.IsActorActive(i))
                {
                    continue;
                }

                float distance = Vector3.SqrMagnitude(view.transform.position - wallAnchor.position);
                if (distance < bestDistanceToWall)
                {
                    bestDistanceToWall = distance;
                    leadingActor = view.transform;
                }
            }
        }
        else if (pressureActors != null && wallAnchor != null)
        {
            for (int i = 0; i < pressureActors.Length; i++)
            {
                Transform actor = pressureActors[i];
                if (actor == null || !actor.gameObject.activeSelf)
                {
                    continue;
                }

                float distance = Vector3.SqrMagnitude(actor.position - wallAnchor.position);
                if (distance < bestDistanceToWall)
                {
                    bestDistanceToWall = distance;
                    leadingActor = actor;
                }
            }
        }

        if (leadingActor != null)
        {
            return leadingActor.position;
        }

        return wallAnchor != null ? wallAnchor.position : attackOrigin.position;
    }

    private void UpdateCombatMessage(DefenseRuntimeState runtime)
    {
        bool actorsReady = usePooledEnemies
            ? actorRuntime != null && actorRuntime.IsReady && enemyPool != null && enemyPool.IsReady
            : CountAssigned(pressureActors) > 0;
        bool wallReady = UseProductionBattlefield ||
                         wallContactObject != null ||
                         wallContactRenderer != null;
        bool attacksReady = UseProductionBattlefield || CountAssigned(attackPulses) > 0;

        if (enemySpawnAnchor == null || wallAnchor == null)
        {
            LastCombatMessage = "Ground combat visuals: missing lane anchors";
            return;
        }

        if (!actorsReady && !wallReady && !attacksReady)
        {
            LastCombatMessage = "Ground combat visuals: anchors only";
            return;
        }

        string actorRuntimeText = actorRuntime == null
            ? "actor runtime missing"
            : $"lead HP {Mathf.RoundToInt(actorRuntime.LeadingActorHealthPercent * 100f)}% / hits {actorRuntime.TotalHitCount} / defeats {actorRuntime.TotalDefeatCount} / contacts {actorRuntime.TotalWallContactCount}";
        string actorCapacityText = UseProductionBattlefield && !battlefieldView.UsesRuntimeEnemies
            ? $"{ActivePressureActorCount} static grammar proof"
            : usePooledEnemies && actorRuntime != null
                ? $"{ActivePressureActorCount}/{actorRuntime.ActorCapacity} pooled ({enemyPool?.ActiveCount ?? 0}/{enemyPool?.CreatedCount ?? 0})"
                : $"{ActivePressureActorCount}/{CountAssigned(pressureActors)} fixed";
        string attackText = UseProductionBattlefield
            ? $"{battlefieldView.PresentationStage} / defenders {battlefieldView.ActiveDefenderCount} / projectiles {battlefieldView.ActiveProjectileCount}"
            : $"legacy attacks {ActiveAttackPulseCount}/{CountAssigned(attackPulses)}";
        LastCombatMessage = $"Ground combat: {runtime.State} / actors {actorCapacityText} / {attackText} / {actorRuntimeText} / pressure +{runtime.LastIncomingPressurePerSecond:0.#}/-{runtime.LastPressureClearedPerSecond:0.#}/s / wall {runtime.LastWallDamagePerSecond:0.##}/s";
    }

    private void EnsurePooledViewStorage()
    {
        int capacity = actorRuntime == null ? 0 : actorRuntime.ActorCapacity;
        if (pooledActorViews != null && pooledActorViews.Length == capacity)
        {
            return;
        }

        ReleasePooledViews();
        pooledActorViews = new GroundDefenseEnemyView[capacity];
    }

    private void ReturnPooledView(int index)
    {
        if (pooledActorViews == null || index < 0 || index >= pooledActorViews.Length)
        {
            return;
        }

        if (pooledActorViews[index] != null)
        {
            enemyPool?.Return(pooledActorViews[index]);
            pooledActorViews[index] = null;
        }
    }

    private void ReleasePooledViews()
    {
        if (pooledActorViews == null)
        {
            return;
        }

        for (int i = 0; i < pooledActorViews.Length; i++)
        {
            ReturnPooledView(i);
        }
    }

    private Color GetPressureActorColor(DefenseRuntimeState runtime, float travelPercent)
    {
        if (runtime.State == DefenseState.Breached || runtime.LastWallDamagePerSecond > 0f || travelPercent >= 0.92f)
        {
            return wallContactColor;
        }

        return runtime.LastPressureClearedPerSecond > 0f ? pressureActorUnderFireColor : pressureActorColor;
    }

    private void CaptureBaseScales()
    {
        if (wallContactObject != null)
        {
            baseWallContactScale = wallContactObject.transform.localScale;
            return;
        }

        if (wallContactRenderer != null)
        {
            baseWallContactScale = wallContactRenderer.transform.localScale;
        }
    }

    private void ResolveReferences(bool force = false)
    {
        if ((autoFindDefense || force) && (defense == null || force))
        {
            defense = FindAnyObjectByType<DefenseDirector>();
        }

        if ((autoFindActorRuntime || force) && (actorRuntime == null || force))
        {
            actorRuntime = GetComponent<GroundDefenseActorRuntime>();
            if (actorRuntime == null)
            {
                actorRuntime = FindAnyObjectByType<GroundDefenseActorRuntime>();
            }
        }

        if ((autoFindEnemyPool || force) && (enemyPool == null || force))
        {
            enemyPool = GetComponent<GroundDefenseEnemyPool>();
            if (enemyPool == null)
            {
                enemyPool = FindAnyObjectByType<GroundDefenseEnemyPool>();
            }
        }

        if ((autoFindBattlefieldView || force) && (battlefieldView == null || force))
        {
            battlefieldView = GetComponent<GroundDefenseBattlefieldView>();
            if (battlefieldView == null)
            {
                battlefieldView = FindAnyObjectByType<GroundDefenseBattlefieldView>();
            }
        }
    }

    private bool UseProductionBattlefield =>
        useProductionBattlefield &&
        battlefieldView != null &&
        battlefieldView.IsReady;

    private void SyncActorRuntimeSubscription(bool clear = false)
    {
        GroundDefenseActorRuntime target = clear ? null : actorRuntime;
        if (subscribedActorRuntime == target)
        {
            return;
        }

        if (subscribedActorRuntime != null)
        {
            subscribedActorRuntime.ActorHit -= HandleActorHit;
        }

        subscribedActorRuntime = target;
        if (subscribedActorRuntime != null)
        {
            subscribedActorRuntime.ActorHit += HandleActorHit;
        }
    }

    private void HandleActorHit(int actorIndex)
    {
        if (!UseProductionBattlefield || actorRuntime == null)
        {
            return;
        }

        GroundDefenseEnemyArchetype archetype = actorRuntime.GetActorArchetype(actorIndex);
        Vector3 targetPosition = battlefieldView.GetEnemyWorldPosition(
            actorIndex,
            actorRuntime.GetActorTravelPercent(actorIndex));
        targetPosition += Vector3.up * (archetype == null ? 1f : archetype.VisualHeightOffset);
        battlefieldView.PlayDefenseHit(actorIndex, targetPosition);
    }

    private void SetWallContactVisible(bool active)
    {
        if (wallContactObject != null)
        {
            SetActive(wallContactObject, active);
            return;
        }

        if (wallContactRenderer != null)
        {
            SetActive(wallContactRenderer.gameObject, active);
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

    private static int CountAssigned(Transform[] targets)
    {
        if (targets == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                count += 1;
            }
        }

        return count;
    }

    private static void SetAllActive(Transform[] targets, bool active)
    {
        if (targets == null)
        {
            return;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            Transform target = targets[i];
            if (target != null)
            {
                SetActive(target.gameObject, active);
            }
        }
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }
}
