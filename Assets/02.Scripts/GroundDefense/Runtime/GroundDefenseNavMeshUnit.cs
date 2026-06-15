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
    private bool deathReported;

    public GroundDefenseNavMeshUnitSide Side => side;
    public CharacterActor Actor => actor;
    public Health Health => actor == null ? null : actor.Health;
    public Vector3 HomePosition => homePosition;
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
    }

    public void PlayHitFeedback()
    {
        hitFeedbackRemaining = 0.16f;
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
            attackFeedbackRemaining = 0.14f;
            target.PlayHitFeedback();
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
            attackFeedbackRemaining = 0.14f;
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
                : Mathf.Lerp(1f, 1.14f, attackFeedbackRemaining / 0.14f);
            visualRoot.localScale = baseVisualScale * pulse;
        }

        if (!deathReported)
        {
            SetVisualColor(hitFeedbackRemaining > 0f
                ? new Color(1f, 0.55f, 0.32f, 1f)
                : Color.white);
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
