using UnityEngine;

[RequireComponent(typeof(CharacterActor))]
public class EnemyAIController : MonoBehaviour
{
    [SerializeField] private CharacterActor target;
    [SerializeField] private bool autoFindPlayer = true;
    [SerializeField] private float chaseRefreshInterval = 0.15f;
    [SerializeField] private float retargetInterval = 0.5f;

    private CharacterActor actor;
    private float nextChaseRefreshTime;
    private float nextRetargetTime;

    private void Awake()
    {
        actor = GetComponent<CharacterActor>();
        ResolveTarget(true);
    }

    private void OnValidate()
    {
        chaseRefreshInterval = Mathf.Max(0.05f, chaseRefreshInterval);
        retargetInterval = Mathf.Max(0.05f, retargetInterval);
    }

    private void Update()
    {
        ResolveTarget();

        if (!CanFightTarget())
        {
            actor?.Motor?.Stop();
            return;
        }

        if (actor.Combat.IsInRange(target.transform))
        {
            actor.Motor.Stop();
            actor.Motor.FaceToward(target.transform.position);
            actor.Combat.TryBasicAttack(target.Health);
            return;
        }

        if (Time.time < nextChaseRefreshTime)
        {
            return;
        }

        nextChaseRefreshTime = Time.time + chaseRefreshInterval;
        actor.Motor.TryMoveTo(target.transform.position);
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
