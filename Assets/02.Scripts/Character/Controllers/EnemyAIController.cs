using UnityEngine;

[RequireComponent(typeof(CharacterActor))]
public class EnemyAIController : MonoBehaviour
{
    [SerializeField] private CharacterActor target;
    [SerializeField] private bool autoFindPlayer = true;
    [SerializeField] private float chaseRefreshInterval = 0.15f;
    [SerializeField] private float retargetInterval = 0.5f;
    [SerializeField, Min(0.1f)] private float attackWindupDuration = 0.65f;

    private CharacterActor actor;
    private CharacterActor windupTarget;
    private float nextChaseRefreshTime;
    private float nextRetargetTime;
    private float windupEndTime;

    public event System.Action AttackWindupStarted;
    public event System.Action AttackWindupCompleted;
    public event System.Action AttackWindupCanceled;

    public bool IsWindingUp => windupTarget != null;

    private void Awake()
    {
        actor = GetComponent<CharacterActor>();
        ResolveTarget(true);
    }

    private void OnValidate()
    {
        chaseRefreshInterval = Mathf.Max(0.05f, chaseRefreshInterval);
        retargetInterval = Mathf.Max(0.05f, retargetInterval);
        attackWindupDuration = Mathf.Max(0.1f, attackWindupDuration);
    }

    private void Update()
    {
        ResolveTarget();

        if (!CanFightTarget())
        {
            CancelAttackWindup();
            actor?.Motor?.Stop();
            return;
        }

        if (IsWindingUp)
        {
            UpdateAttackWindup();
            return;
        }

        if (actor.Combat.IsInRange(target.transform))
        {
            actor.Motor.Stop();
            actor.Motor.FaceToward(target.transform.position);
            StartAttackWindup();
            return;
        }

        if (Time.time < nextChaseRefreshTime)
        {
            return;
        }

        nextChaseRefreshTime = Time.time + chaseRefreshInterval;
        actor.Motor.TryMoveTo(target.transform.position);
    }

    private void OnDisable()
    {
        CancelAttackWindup();
    }

    private void StartAttackWindup()
    {
        if (actor.Combat.IsCoolingDown || target == null || target.Health == null || !target.Health.IsAlive)
        {
            return;
        }

        windupTarget = target;
        windupEndTime = Time.time + attackWindupDuration;
        AttackWindupStarted?.Invoke();
    }

    private void UpdateAttackWindup()
    {
        actor.Motor.Stop();

        if (windupTarget == null || windupTarget.Health == null || !windupTarget.Health.IsAlive)
        {
            CancelAttackWindup();
            return;
        }

        actor.Motor.FaceToward(windupTarget.transform.position);

        if (Time.time < windupEndTime)
        {
            return;
        }

        CharacterActor resolvedTarget = windupTarget;
        windupTarget = null;

        // Damage authority remains in CombatDriver. Rechecking range here makes leaving the ring a true dodge.
        if (actor.Combat.TryBasicAttack(resolvedTarget.Health))
        {
            AttackWindupCompleted?.Invoke();
            return;
        }

        AttackWindupCanceled?.Invoke();
    }

    private void CancelAttackWindup()
    {
        if (!IsWindingUp)
        {
            return;
        }

        windupTarget = null;
        AttackWindupCanceled?.Invoke();
    }

    private void ResolveTarget(bool force = false)
    {
        if (!autoFindPlayer && !force)
        {
            return;
        }

        if (!force && target != null && target.Health != null && target.Health.IsAlive)
        {
            return;
        }

        if (!force && Time.time < nextRetargetTime)
        {
            return;
        }

        nextRetargetTime = Time.time + retargetInterval;

        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null && player.TryGetComponent(out CharacterActor playerActor))
        {
            target = playerActor;
            return;
        }

        CharacterActor[] actors = FindObjectsByType<CharacterActor>(FindObjectsInactive.Exclude);
        for (int i = 0; i < actors.Length; i++)
        {
            CharacterActor candidate = actors[i];
            if (candidate != null && candidate.Team == CharacterTeam.Player)
            {
                target = candidate;
                return;
            }
        }
    }

    private bool CanFightTarget()
    {
        return actor != null &&
               actor.Health != null &&
               actor.Health.IsAlive &&
               actor.Motor != null &&
               actor.Combat != null &&
               target != null &&
               target.Health != null &&
               target.Health.IsAlive &&
               target.Team != actor.Team;
    }
}
