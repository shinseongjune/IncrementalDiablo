using UnityEngine;

public class GroundDefenseCombatPresenter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DefenseDirector defense;
    [SerializeField] private bool autoFindDefense = true;

    [Header("Lane Anchors")]
    [SerializeField] private Transform enemySpawnAnchor;
    [SerializeField] private Transform wallAnchor;
    [SerializeField] private Transform attackOrigin;

    [Header("Pressure Actors")]
    [SerializeField] private Transform[] pressureActors;
    [SerializeField] private bool showPressureActors = true;
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

    [Header("Refresh")]
    [SerializeField] private bool refreshEveryFrame = true;

    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock propertyBlock;
    private Vector3 baseWallContactScale = Vector3.one;
    private float lastWallHealth;
    private float wallContactFlashRemaining;
    private bool hasWallSnapshot;
    private DefenseState lastState = DefenseState.Idle;

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

        if (defense != null)
        {
            defense.Changed += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (defense != null)
        {
            defense.Changed -= Refresh;
        }
    }

    private void OnValidate()
    {
        minimumRunningActors = Mathf.Max(0, minimumRunningActors);
        pressureActorCyclesPerSecond = Mathf.Max(0f, pressureActorCyclesPerSecond);
        wallContactFlashSeconds = Mathf.Max(0.01f, wallContactFlashSeconds);
        wallContactScaleMultiplier = Mathf.Max(1f, wallContactScaleMultiplier);
        attackPulseCyclesPerSecond = Mathf.Max(0f, attackPulseCyclesPerSecond);
        defensePowerPerVisiblePulse = Mathf.Max(0.01f, defensePowerPerVisiblePulse);
    }

    private void Update()
    {
        if (!refreshEveryFrame)
        {
            return;
        }

        Refresh();
    }

    public void Refresh()
    {
        ResolveReferences();

        if (defense == null || defense.Runtime == null)
        {
            ActivePressureActorCount = 0;
            ActiveAttackPulseCount = 0;
            LastCombatMessage = "Ground combat visuals: no DefenseDirector";
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

        if (!showPressureActors || pressureActors == null || pressureActors.Length == 0)
        {
            return;
        }

        if (enemySpawnAnchor == null || wallAnchor == null)
        {
            SetAllActive(pressureActors, false);
            return;
        }

        int validActorCount = CountAssigned(pressureActors);
        if (validActorCount == 0)
        {
            return;
        }

        bool shouldShowActors = runtime.IsRunning || runtime.State == DefenseState.Breached;
        if (!shouldShowActors)
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
            hasWallSnapshot = true;
        }

        bool tookWallDamage = runtime.WallHealth < lastWallHealth - 0.001f;
        bool becameBreached = lastState != DefenseState.Breached && runtime.State == DefenseState.Breached;
        if (tookWallDamage || becameBreached)
        {
            wallContactFlashRemaining = wallContactFlashSeconds;
            WallContactEventCount += 1;
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
                SetRendererColor(pulse.GetComponentInChildren<Renderer>(), attackPulseColor);
                ActiveAttackPulseCount += 1;
                activePulseIndex += 1;
            }

            validPulseIndex += 1;
        }
    }

    private Vector3 GetAttackTargetPosition()
    {
        Transform leadingActor = null;
        float bestDistanceToWall = float.MaxValue;

        if (pressureActors != null && wallAnchor != null)
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
        bool actorsReady = CountAssigned(pressureActors) > 0;
        bool wallReady = wallContactObject != null || wallContactRenderer != null;
        bool attacksReady = CountAssigned(attackPulses) > 0;

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

        LastCombatMessage = $"Ground combat visuals: {runtime.State} / actors {ActivePressureActorCount}/{CountAssigned(pressureActors)} / attacks {ActiveAttackPulseCount}/{CountAssigned(attackPulses)} / wall hits {WallContactEventCount} / pressure +{runtime.LastIncomingPressurePerSecond:0.#}/-{runtime.LastPressureClearedPerSecond:0.#}/s / wall {runtime.LastWallDamagePerSecond:0.##}/s";
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
