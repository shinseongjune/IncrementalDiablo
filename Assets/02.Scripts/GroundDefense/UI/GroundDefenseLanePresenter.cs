using TMPro;
using UnityEngine;

public class GroundDefenseLanePresenter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DefenseDirector defense;
    [SerializeField] private bool autoFindDefense = true;

    [Header("Lane Anchors")]
    [SerializeField] private Transform enemySpawnAnchor;
    [SerializeField] private Transform wallAnchor;
    [SerializeField] private Transform enemyPressureMarker;
    [SerializeField] private Transform pushProgressMarker;

    [Header("Fill Transforms")]
    [SerializeField] private Transform wallHealthFill;
    [SerializeField] private Transform pressureFill;

    [Header("State Objects")]
    [SerializeField] private GameObject holdStateObject;
    [SerializeField] private GameObject pushStateObject;
    [SerializeField] private GameObject breachedStateObject;

    [Header("Renderers")]
    [SerializeField] private Renderer wallRenderer;
    [SerializeField] private Renderer pressureRenderer;
    [SerializeField] private Renderer progressRenderer;
    [SerializeField] private bool autoResolveMarkerRenderers = true;
    [SerializeField] private Color idleColor = new Color(0.6f, 0.65f, 0.7f);
    [SerializeField] private Color holdColor = new Color(0.25f, 0.7f, 0.45f);
    [SerializeField] private Color pushColor = new Color(0.95f, 0.7f, 0.2f);
    [SerializeField] private Color warningColor = new Color(1f, 0.45f, 0.2f);
    [SerializeField] private Color breachedColor = new Color(0.9f, 0.15f, 0.12f);

    [Header("Enemy Flow Markers")]
    [SerializeField] private Transform[] enemyFlowMarkers;
    [SerializeField] private bool showEnemyFlowMarkers = true;
    [SerializeField] private int minimumRunningEnemyMarkers = 1;
    [SerializeField] private float enemyFlowCyclesPerSecond = 0.18f;

    [Header("Labels")]
    [SerializeField] private TMP_Text stateLabel;
    [SerializeField] private TMP_Text pressureLabel;
    [SerializeField] private TMP_Text progressLabel;
    [SerializeField] private TMP_Text wallLabel;

    [Header("Refresh")]
    [SerializeField] private bool refreshEveryFrame = true;
    [SerializeField] private float lowWallWarningThreshold = 0.35f;

    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock propertyBlock;
    private Vector3 baseWallHealthFillScale = Vector3.one;
    private Vector3 basePressureFillScale = Vector3.one;

    public int ActiveEnemyFlowMarkerCount { get; private set; }
    public string LastPresentationMessage { get; private set; } = "Frontline visuals: not refreshed";

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
        lowWallWarningThreshold = Mathf.Clamp01(lowWallWarningThreshold);
        minimumRunningEnemyMarkers = Mathf.Max(0, minimumRunningEnemyMarkers);
        enemyFlowCyclesPerSecond = Mathf.Max(0f, enemyFlowCyclesPerSecond);
    }

    private void Update()
    {
        if (!refreshEveryFrame)
        {
            return;
        }

        if (defense != null && defense.Runtime != null && defense.Runtime.IsRunning)
        {
            Refresh();
        }
    }

    public void Refresh()
    {
        ResolveReferences();

        if (defense == null || defense.Runtime == null)
        {
            LastPresentationMessage = "Frontline visuals: no DefenseDirector";
            UpdateEnemyFlowMarkers(null);
            SetText(stateLabel, "Frontline visuals: no DefenseDirector");
            return;
        }

        DefenseRuntimeState runtime = defense.Runtime;
        UpdateMarkers(runtime);
        UpdateEnemyFlowMarkers(runtime);
        UpdateFillScales(runtime);
        UpdateStateObjects(runtime);
        UpdateRendererColors(runtime);
        UpdateLabels(runtime);
        UpdatePresentationMessage(runtime);
    }

    private void UpdateMarkers(DefenseRuntimeState runtime)
    {
        if (enemySpawnAnchor != null && wallAnchor != null && enemyPressureMarker != null)
        {
            enemyPressureMarker.position = Vector3.Lerp(
                enemySpawnAnchor.position,
                wallAnchor.position,
                runtime.PressurePercent);
        }

        if (enemySpawnAnchor != null && wallAnchor != null && pushProgressMarker != null)
        {
            pushProgressMarker.position = Vector3.Lerp(
                wallAnchor.position,
                enemySpawnAnchor.position,
                runtime.FrontlineProgressPercent);
        }
    }

    private void UpdateFillScales(DefenseRuntimeState runtime)
    {
        if (wallHealthFill != null)
        {
            Vector3 nextScale = baseWallHealthFillScale;
            nextScale.x *= runtime.WallHealthPercent;
            wallHealthFill.localScale = nextScale;
        }

        if (pressureFill != null)
        {
            Vector3 nextScale = basePressureFillScale;
            nextScale.x *= runtime.PressurePercent;
            pressureFill.localScale = nextScale;
        }
    }

    private void UpdateEnemyFlowMarkers(DefenseRuntimeState runtime)
    {
        ActiveEnemyFlowMarkerCount = 0;

        if (!showEnemyFlowMarkers || enemyFlowMarkers == null || enemyFlowMarkers.Length == 0)
        {
            return;
        }

        if (runtime == null || enemySpawnAnchor == null || wallAnchor == null)
        {
            SetAllEnemyFlowMarkersActive(false);
            return;
        }

        int validMarkerCount = CountAssignedEnemyFlowMarkers();
        if (validMarkerCount == 0)
        {
            return;
        }

        bool shouldShowFlow = runtime.IsRunning || runtime.State == DefenseState.Breached;
        if (!shouldShowFlow)
        {
            SetAllEnemyFlowMarkersActive(false);
            return;
        }

        int minimumMarkers = runtime.State == DefenseState.Breached
            ? validMarkerCount
            : Mathf.Min(minimumRunningEnemyMarkers, validMarkerCount);
        int activeTarget = Mathf.Clamp(
            Mathf.CeilToInt(Mathf.Lerp(minimumMarkers, validMarkerCount, runtime.PressurePercent)),
            0,
            validMarkerCount);

        float flowPhase = runtime.TotalElapsed * enemyFlowCyclesPerSecond;
        int validMarkerIndex = 0;
        int activeMarkerIndex = 0;

        for (int i = 0; i < enemyFlowMarkers.Length; i++)
        {
            Transform marker = enemyFlowMarkers[i];
            if (marker == null)
            {
                continue;
            }

            bool active = validMarkerIndex < activeTarget;
            SetActive(marker.gameObject, active);

            if (active)
            {
                float spacing = activeTarget <= 1 ? 0f : activeMarkerIndex / (float)activeTarget;
                float travelPercent = Mathf.Repeat(flowPhase + spacing, 1f);
                marker.position = Vector3.Lerp(enemySpawnAnchor.position, wallAnchor.position, travelPercent);
                ActiveEnemyFlowMarkerCount += 1;
                activeMarkerIndex += 1;
            }

            validMarkerIndex += 1;
        }
    }

    private void UpdateStateObjects(DefenseRuntimeState runtime)
    {
        SetActive(holdStateObject, runtime.State == DefenseState.Holding || runtime.State == DefenseState.WaitingForRepairOrUpgrade || runtime.State == DefenseState.Idle);
        SetActive(pushStateObject, runtime.State == DefenseState.Pushing);
        SetActive(breachedStateObject, runtime.State == DefenseState.Breached);
    }

    private void UpdateRendererColors(DefenseRuntimeState runtime)
    {
        Color stateColor = GetStateColor(runtime);
        Color wallColor = runtime.State == DefenseState.Breached
            ? breachedColor
            : runtime.WallHealthPercent <= lowWallWarningThreshold
                ? warningColor
                : stateColor;

        SetRendererColor(wallRenderer, wallColor);
        SetRendererColor(pressureRenderer, runtime.State == DefenseState.Breached || runtime.PressurePercent >= 0.8f ? warningColor : stateColor);
        SetRendererColor(progressRenderer, stateColor);
    }

    private void UpdateLabels(DefenseRuntimeState runtime)
    {
        SetText(stateLabel, $"Frontline Lv.{runtime.FrontlineLevel} / {runtime.State} / {runtime.Mode}");
        SetText(pressureLabel, $"Pressure {Mathf.RoundToInt(runtime.PressurePercent * 100f)}%");
        SetText(progressLabel, runtime.Mode == FrontlineMode.Push
            ? $"Push {Mathf.RoundToInt(runtime.FrontlineProgressPercent * 100f)}%"
            : "Push paused");
        SetText(wallLabel, $"Wall {Mathf.CeilToInt(runtime.WallHealth)}/{Mathf.CeilToInt(runtime.WallMaxHealth)}");
    }

    private void UpdatePresentationMessage(DefenseRuntimeState runtime)
    {
        bool laneAnchorsReady = enemySpawnAnchor != null && wallAnchor != null;
        bool pressureReady = enemyPressureMarker != null || pressureFill != null || pressureRenderer != null;
        bool progressReady = pushProgressMarker != null || progressRenderer != null;

        if (!laneAnchorsReady)
        {
            LastPresentationMessage = "Frontline visuals: missing lane anchors";
            return;
        }

        if (!pressureReady && !progressReady && CountAssignedEnemyFlowMarkers() == 0)
        {
            LastPresentationMessage = "Frontline visuals: anchors only";
            return;
        }

        string flowText = CountAssignedEnemyFlowMarkers() > 0
            ? $"flow {ActiveEnemyFlowMarkerCount}/{CountAssignedEnemyFlowMarkers()}"
            : "flow markers unassigned";
        LastPresentationMessage = $"Frontline visuals: {runtime.State} / pressure {Mathf.RoundToInt(runtime.PressurePercent * 100f)}% / {flowText}";
    }

    private Color GetStateColor(DefenseRuntimeState runtime)
    {
        return runtime.State switch
        {
            DefenseState.Holding => holdColor,
            DefenseState.Pushing => pushColor,
            DefenseState.Breached => breachedColor,
            DefenseState.WaitingForRepairOrUpgrade => warningColor,
            _ => idleColor
        };
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

    private void CaptureBaseScales()
    {
        if (wallHealthFill != null)
        {
            baseWallHealthFillScale = wallHealthFill.localScale;
        }

        if (pressureFill != null)
        {
            basePressureFillScale = pressureFill.localScale;
        }
    }

    private void ResolveReferences(bool force = false)
    {
        if ((autoFindDefense || force) && (defense == null || force))
        {
            defense = FindAnyObjectByType<DefenseDirector>();
        }

        ResolveMarkerRenderers(force);
    }

    private void ResolveMarkerRenderers(bool force)
    {
        if (!autoResolveMarkerRenderers && !force)
        {
            return;
        }

        if ((pressureRenderer == null || force) && enemyPressureMarker != null)
        {
            pressureRenderer = enemyPressureMarker.GetComponentInChildren<Renderer>();
        }

        if ((progressRenderer == null || force) && pushProgressMarker != null)
        {
            progressRenderer = pushProgressMarker.GetComponentInChildren<Renderer>();
        }
    }

    private int CountAssignedEnemyFlowMarkers()
    {
        if (enemyFlowMarkers == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < enemyFlowMarkers.Length; i++)
        {
            if (enemyFlowMarkers[i] != null)
            {
                count += 1;
            }
        }

        return count;
    }

    private void SetAllEnemyFlowMarkersActive(bool active)
    {
        if (enemyFlowMarkers == null)
        {
            return;
        }

        for (int i = 0; i < enemyFlowMarkers.Length; i++)
        {
            Transform marker = enemyFlowMarkers[i];
            if (marker != null)
            {
                SetActive(marker.gameObject, active);
            }
        }
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
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
