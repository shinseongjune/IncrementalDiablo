using UnityEngine;
using UnityEngine.AI;

public enum GroundDefenseNavMeshUnitSide
{
    Defender,
    Enemy
}

[RequireComponent(typeof(CharacterActor))]
public sealed class GroundDefenseNavMeshUnit : MonoBehaviour
{
    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");
    private const float AttackFeedbackSeconds = 0.2f;
    private const float HitFeedbackSeconds = 0.18f;

    private GroundDefenseNavMeshBattlefield battlefield;
    private GroundDefenseNavMeshUnitSide side;
    private CharacterActor actor;
    private NavMeshAgent agent;
    private Collider bodyCollider;
    private Transform visualRoot;
    private Renderer visualRenderer;
    private Vector3 homePosition;
    private Vector3 baseVisualScale = Vector3.one;
    private MaterialPropertyBlock propertyBlock;
    private float attackFeedbackRemaining;
    private float hitFeedbackRemaining;
    private LineRenderer attackOwnershipLine;
    private Material attackLineMaterial;
    private Vector3 attackLineEnd;
    private bool deathReported;

    public GroundDefenseNavMeshUnitSide Side => side;
    public CharacterActor Actor => actor;
    public Health Health => actor == null ? null : actor.Health;
    public Vector3 HomePosition => homePosition;
    public Vector3 VisualHitPosition => visualRoot == null
        ? transform.position + Vector3.up * 1.1f
        : visualRoot.position;
    public bool IsAlive => Health != null && Health.IsAlive;

    private void Awake()
    {
        actor = GetComponent<CharacterActor>();
        agent = GetComponent<NavMeshAgent>();
        bodyCollider = GetComponent<Collider>();
    }

    private void Update()
    {
        TickVisualFeedback();
        TickAttackOwnershipLine();

        if (battlefield == null || actor == null || actor.Health == null)
        {
            return;
        }

        if (!actor.Health.IsAlive)
        {
            HandleDeath();
            return;
        }

        if (actor.Motor == null || actor.Combat == null || !actor.Motor.IsOnNavMesh)
        {
            return;
        }

        if (!battlefield.UnitsCanAct)
        {
            ReturnHome();
            return;
        }

        if (side == GroundDefenseNavMeshUnitSide.Defender)
        {
            UpdateDefender();
        }
        else
        {
            UpdateEnemy();
        }
    }

    public void Configure(
        GroundDefenseNavMeshBattlefield owner,
        GroundDefenseNavMeshUnitSide unitSide,
        Vector3 spawnPosition,
        Transform nextVisualRoot,
        Renderer nextVisualRenderer)
    {
        battlefield = owner;
        side = unitSide;
        homePosition = spawnPosition;
        visualRoot = nextVisualRoot;
        visualRenderer = nextVisualRenderer;
        baseVisualScale = visualRoot == null ? Vector3.one : visualRoot.localScale;
        CreateAttackOwnershipLine(unitSide);
    }

    public void PlayHitFeedback()
    {
        hitFeedbackRemaining = HitFeedbackSeconds;
    }

    private void UpdateDefender()
    {
        GroundDefenseNavMeshUnit target = battlefield.FindNearestEnemy(
            transform.position,
            homePosition);
        if (target == null)
        {
            ReturnHome();
            return;
        }

        FightUnit(target);
    }

    private void UpdateEnemy()
    {
        GroundDefenseNavMeshUnit defender = battlefield.FindNearestDefender(transform.position);
        if (defender != null)
        {
            FightUnit(defender);
            return;
        }

        FightWall();
    }

    private void FightUnit(GroundDefenseNavMeshUnit target)
    {
        if (target == null || !target.IsAlive)
        {
            return;
        }

        if (!actor.Combat.IsInRange(target.transform))
        {
            actor.Motor.TryMoveTo(target.transform.position);
            return;
        }

        actor.Motor.Stop();
        actor.Motor.FaceToward(target.transform.position);
        if (actor.Combat.TryBasicAttack(target.Health))
        {
            attackFeedbackRemaining = AttackFeedbackSeconds;
            target.PlayHitFeedback();
            ShowAttackOwnership(target.VisualHitPosition);
        }
    }

    private void FightWall()
    {
        Vector3 wallApproach = battlefield.WallApproachPosition;
        if (Vector3.Distance(transform.position, wallApproach) >
            actor.Stats.GetValue(StatId.AttackRange))
        {
            actor.Motor.TryMoveTo(wallApproach);
            return;
        }

        actor.Motor.Stop();
        actor.Motor.FaceToward(battlefield.WallPosition);
        if (actor.Combat.TryPlayBasicAttackInPlace())
        {
            attackFeedbackRemaining = AttackFeedbackSeconds;
            ShowAttackOwnership(battlefield.WallPosition + Vector3.up * 1.25f);
            battlefield.ApplyEnemyWallHit(actor.Stats.GetValue(StatId.AttackDamage));
        }
    }

    private void ReturnHome()
    {
        if (Vector3.Distance(transform.position, homePosition) <= 0.3f)
        {
            actor.Motor.Stop();
            return;
        }

        actor.Motor.TryMoveTo(homePosition);
    }

    private void HandleDeath()
    {
        if (deathReported)
        {
            return;
        }

        deathReported = true;
        actor.Motor?.Stop();
        if (agent != null)
        {
            agent.enabled = false;
        }

        if (bodyCollider != null)
        {
            bodyCollider.enabled = false;
        }

        SetVisualColor(new Color(0.28f, 0.08f, 0.06f, 0.7f));
        if (visualRoot != null)
        {
            visualRoot.localScale = baseVisualScale * 0.72f;
        }

        battlefield.NotifyUnitDefeated(this);
    }

    private void TickVisualFeedback()
    {
        attackFeedbackRemaining = Mathf.Max(0f, attackFeedbackRemaining - Time.deltaTime);
        hitFeedbackRemaining = Mathf.Max(0f, hitFeedbackRemaining - Time.deltaTime);

        if (visualRoot != null && !deathReported)
        {
            float pulse = attackFeedbackRemaining <= 0f
                ? 1f
                : Mathf.Lerp(1f, 1.14f, attackFeedbackRemaining / AttackFeedbackSeconds);
            float hitRecoil = hitFeedbackRemaining <= 0f
                ? 1f
                : Mathf.Lerp(1f, 0.88f, hitFeedbackRemaining / HitFeedbackSeconds);
            visualRoot.localScale = baseVisualScale * pulse * hitRecoil;
        }

        if (!deathReported)
        {
            SetVisualColor(hitFeedbackRemaining > 0f
                ? new Color(1f, 0.55f, 0.32f, 1f)
                : Color.white);
        }
    }

    private void CreateAttackOwnershipLine(GroundDefenseNavMeshUnitSide unitSide)
    {
        if (attackOwnershipLine != null)
        {
            return;
        }

        GameObject lineObject = new GameObject("AttackOwnershipLine");
        lineObject.transform.SetParent(transform, false);

        attackOwnershipLine = lineObject.AddComponent<LineRenderer>();
        attackOwnershipLine.positionCount = 2;
        attackOwnershipLine.useWorldSpace = true;
        attackOwnershipLine.widthMultiplier = 0.12f;
        attackOwnershipLine.numCapVertices = 2;
        attackOwnershipLine.alignment = LineAlignment.View;
        attackOwnershipLine.sortingOrder = unitSide == GroundDefenseNavMeshUnitSide.Defender
            ? 32
            : 31;

        Shader shader = Shader.Find("Sprites/Default") ??
                        Shader.Find("Universal Render Pipeline/Unlit") ??
                        Shader.Find("Standard");
        attackLineMaterial = new Material(shader)
        {
            name = "GroundDefenseAttackOwnershipLine_RuntimeMaterial"
        };
        attackOwnershipLine.sharedMaterial = attackLineMaterial;
        attackOwnershipLine.enabled = false;
    }

    private void ShowAttackOwnership(Vector3 targetPosition)
    {
        attackLineEnd = targetPosition;
        if (attackOwnershipLine != null)
        {
            attackOwnershipLine.enabled = true;
        }
    }

    private void TickAttackOwnershipLine()
    {
        if (attackOwnershipLine == null)
        {
            return;
        }

        bool visible = attackFeedbackRemaining > 0f && !deathReported;
        attackOwnershipLine.enabled = visible;
        if (!visible)
        {
            return;
        }

        Color baseColor = side == GroundDefenseNavMeshUnitSide.Defender
            ? new Color(0.35f, 0.7f, 1f, 1f)
            : new Color(1f, 0.28f, 0.12f, 1f);
        float alpha = Mathf.Clamp01(attackFeedbackRemaining / AttackFeedbackSeconds);
        Color startColor = baseColor;
        Color endColor = Color.white;
        startColor.a = alpha;
        endColor.a = alpha;

        attackOwnershipLine.startColor = startColor;
        attackOwnershipLine.endColor = endColor;
        attackOwnershipLine.SetPosition(0, VisualHitPosition);
        attackOwnershipLine.SetPosition(1, attackLineEnd);
    }

    private void OnDestroy()
    {
        if (attackLineMaterial != null)
        {
            Destroy(attackLineMaterial);
        }
    }

    private void SetVisualColor(Color color)
    {
        if (visualRenderer == null)
        {
            return;
        }

        if (visualRenderer is SpriteRenderer spriteRenderer)
        {
            spriteRenderer.color = color;
            return;
        }

        propertyBlock ??= new MaterialPropertyBlock();
        visualRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(BaseColorProperty, color);
        propertyBlock.SetColor(ColorProperty, color);
        visualRenderer.SetPropertyBlock(propertyBlock);
    }
}
