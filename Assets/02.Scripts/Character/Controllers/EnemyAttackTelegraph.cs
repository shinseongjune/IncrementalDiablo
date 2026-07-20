using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyAIController))]
[RequireComponent(typeof(CombatDriver))]
public sealed class EnemyAttackTelegraph : MonoBehaviour
{
    [Header("Ring")]
    [SerializeField] private LineRenderer ring;
    [SerializeField] private bool createRuntimeRingIfMissing = true;
    [SerializeField, Min(0f)] private float radiusPadding = 0.08f;
    [SerializeField, Min(0f)] private float ringHeight = 0.04f;
    [SerializeField, Min(0.01f)] private float ringWidth = 0.08f;
    [SerializeField, Range(12, 96)] private int segmentCount = 40;
    [SerializeField] private Color ringColor = new Color(1f, 0.2f, 0.05f, 0.9f);
    [SerializeField, Min(0f)] private float pulseSpeed = 5f;
    [SerializeField, Range(0f, 1f)] private float pulseAlphaFloor = 0.45f;

    private EnemyAIController enemyAi;
    private CombatDriver combat;
    private Material runtimeRingMaterial;
    private bool isVisible;

    private void Awake()
    {
        enemyAi = GetComponent<EnemyAIController>();
        combat = GetComponent<CombatDriver>();

        if (ring == null && createRuntimeRingIfMissing)
        {
            ring = CreateRuntimeRing();
        }

        ApplyRingGeometry();
        SetVisible(false);
    }

    private void OnEnable()
    {
        if (enemyAi == null)
        {
            enemyAi = GetComponent<EnemyAIController>();
        }

        if (enemyAi != null)
        {
            enemyAi.AttackWindupStarted += HandleWindupStarted;
            enemyAi.AttackWindupCompleted += HandleWindupFinished;
            enemyAi.AttackWindupCanceled += HandleWindupFinished;

            if (enemyAi.IsWindingUp)
            {
                HandleWindupStarted();
            }
        }
    }

    private void OnDisable()
    {
        if (enemyAi != null)
        {
            enemyAi.AttackWindupStarted -= HandleWindupStarted;
            enemyAi.AttackWindupCompleted -= HandleWindupFinished;
            enemyAi.AttackWindupCanceled -= HandleWindupFinished;
        }

        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (runtimeRingMaterial != null)
        {
            Destroy(runtimeRingMaterial);
        }
    }

    private void OnValidate()
    {
        radiusPadding = Mathf.Max(0f, radiusPadding);
        ringHeight = Mathf.Max(0f, ringHeight);
        ringWidth = Mathf.Max(0.01f, ringWidth);
        segmentCount = Mathf.Clamp(segmentCount, 12, 96);
        pulseSpeed = Mathf.Max(0f, pulseSpeed);
        pulseAlphaFloor = Mathf.Clamp01(pulseAlphaFloor);
        ApplyRingGeometry();
    }

    private void Update()
    {
        if (!isVisible || ring == null)
        {
            return;
        }

        float pulse = Mathf.Lerp(pulseAlphaFloor, 1f, Mathf.PingPong(Time.time * pulseSpeed, 1f));
        Color color = ringColor;
        color.a *= pulse;
        ring.startColor = color;
        ring.endColor = color;
    }

    private void HandleWindupStarted()
    {
        ApplyRingGeometry();
        SetVisible(true);
    }

    private void HandleWindupFinished()
    {
        SetVisible(false);
    }

    private LineRenderer CreateRuntimeRing()
    {
        GameObject ringObject = new GameObject("AttackTelegraph");
        ringObject.transform.SetParent(transform, false);

        LineRenderer createdRing = ringObject.AddComponent<LineRenderer>();
        createdRing.useWorldSpace = false;
        createdRing.loop = false;
        createdRing.alignment = LineAlignment.View;
        createdRing.textureMode = LineTextureMode.Stretch;

        Shader ringShader = Shader.Find("Sprites/Default");
        if (ringShader == null)
        {
            ringShader = Shader.Find("Unlit/Color");
        }

        if (ringShader != null)
        {
            runtimeRingMaterial = new Material(ringShader);
            createdRing.sharedMaterial = runtimeRingMaterial;
        }

        return createdRing;
    }

    private void ApplyRingGeometry()
    {
        if (ring == null)
        {
            return;
        }

        float attackRange = combat == null ? 0f : combat.AttackRange;
        float radius = Mathf.Max(0.1f, attackRange + radiusPadding);
        int pointCount = segmentCount + 1;
        ring.useWorldSpace = false;
        ring.loop = false;
        ring.positionCount = pointCount;
        ring.startWidth = ringWidth;
        ring.endWidth = ringWidth;

        for (int pointIndex = 0; pointIndex < pointCount; pointIndex++)
        {
            float angle = pointIndex / (float)segmentCount * Mathf.PI * 2f;
            ring.SetPosition(pointIndex, new Vector3(Mathf.Cos(angle) * radius, ringHeight, Mathf.Sin(angle) * radius));
        }
    }

    private void SetVisible(bool visible)
    {
        isVisible = visible;
        if (ring == null)
        {
            return;
        }

        ring.enabled = visible;
        if (visible)
        {
            Color color = ringColor;
            ring.startColor = color;
            ring.endColor = color;
        }
    }
}
