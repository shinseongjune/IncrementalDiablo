using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterActor))]
public sealed class CombatAnimationDriver : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private bool autoFindAnimator = true;

    [Header("Required Parameters")]
    [SerializeField] private string moveSpeedParameter = "MoveSpeed";
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private string hitTrigger = "Hit";
    [SerializeField] private string deathTrigger = "Death";

    private CharacterMotor motor;
    private CombatDriver combat;
    private Health health;
    private int moveSpeedHash;
    private int attackHash;
    private int hitHash;
    private int deathHash;
    private bool hasMoveSpeedParameter;
    private bool hasAttackTrigger;
    private bool hasHitTrigger;
    private bool hasDeathTrigger;
    private bool isDead;

    public Animator Animator => animator;
    public bool HasAnimator => animator != null;
    public bool IsAnimatorContractReady =>
        animator != null &&
        hasMoveSpeedParameter &&
        hasAttackTrigger &&
        hasHitTrigger &&
        hasDeathTrigger;

    private void Awake()
    {
        ResolveDependencies();
        ResolveAnimator();
        CacheParameterContract();
    }

    private void OnEnable()
    {
        ResolveDependencies();
        ResolveAnimator();
        CacheParameterContract();
        Subscribe();

        if (health != null)
        {
            if (!health.IsAlive)
            {
                HandleDeath();
            }
            // CombatRoom can refill a tracked enemy while it is inactive between rooms.
            // Reconcile the cached state on reactivation so its Animator leaves Death.
            else if (isDead)
            {
                HandleRefilled();
            }
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnValidate()
    {
        CacheParameterContract();
    }

    private void Update()
    {
        if (animator == null && autoFindAnimator)
        {
            ResolveAnimator();
            CacheParameterContract();
        }

        if (animator == null || isDead || !hasMoveSpeedParameter)
        {
            return;
        }

        animator.SetFloat(moveSpeedHash, motor == null ? 0f : motor.CurrentSpeed);
    }

    [ContextMenu("Validate Animator Contract")]
    private void ValidateAnimatorContract()
    {
        ResolveAnimator();
        CacheParameterContract();

        if (animator == null)
        {
            Debug.LogWarning($"{name} has no Animator yet. Assign the Hero or enemy rig Animator to CombatAnimationDriver.", this);
            return;
        }

        if (IsAnimatorContractReady)
        {
            Debug.Log($"{name} Animator contract is ready: MoveSpeed, Attack, Hit, Death.", this);
            return;
        }

        Debug.LogWarning(
            $"{name} Animator is missing one or more required parameters: MoveSpeed (float), Attack/Hit/Death (triggers).",
            this);
    }

    private void ResolveDependencies()
    {
        motor ??= GetComponent<CharacterMotor>();
        combat ??= GetComponent<CombatDriver>();
        health ??= GetComponent<Health>();
    }

    private void ResolveAnimator()
    {
        if (animator == null && autoFindAnimator)
        {
            animator = GetComponentInChildren<Animator>(includeInactive: true);
        }
    }

    private void CacheParameterContract()
    {
        moveSpeedHash = Animator.StringToHash(moveSpeedParameter);
        attackHash = Animator.StringToHash(attackTrigger);
        hitHash = Animator.StringToHash(hitTrigger);
        deathHash = Animator.StringToHash(deathTrigger);

        hasMoveSpeedParameter = HasParameter(moveSpeedHash, AnimatorControllerParameterType.Float);
        hasAttackTrigger = HasParameter(attackHash, AnimatorControllerParameterType.Trigger);
        hasHitTrigger = HasParameter(hitHash, AnimatorControllerParameterType.Trigger);
        hasDeathTrigger = HasParameter(deathHash, AnimatorControllerParameterType.Trigger);
    }

    private bool HasParameter(int parameterHash, AnimatorControllerParameterType type)
    {
        if (animator == null)
        {
            return false;
        }

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].nameHash == parameterHash && parameters[i].type == type)
            {
                return true;
            }
        }

        return false;
    }

    private void Subscribe()
    {
        if (combat != null)
        {
            combat.BasicAttackPerformed += HandleAttack;
        }

        if (health != null)
        {
            health.Damaged += HandleDamaged;
            health.Died += HandleDeath;
            health.Refilled += HandleRefilled;
        }
    }

    private void Unsubscribe()
    {
        if (combat != null)
        {
            combat.BasicAttackPerformed -= HandleAttack;
        }

        if (health != null)
        {
            health.Damaged -= HandleDamaged;
            health.Died -= HandleDeath;
            health.Refilled -= HandleRefilled;
        }
    }

    private void HandleAttack()
    {
        if (!isDead && animator != null && hasAttackTrigger)
        {
            animator.SetTrigger(attackHash);
        }
    }

    private void HandleDamaged(float damage, float currentHealth)
    {
        if (!isDead && currentHealth > 0f && animator != null && hasHitTrigger)
        {
            animator.SetTrigger(hitHash);
        }
    }

    private void HandleDeath()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        if (animator != null && hasDeathTrigger)
        {
            animator.SetTrigger(deathHash);
        }
    }

    private void HandleRefilled()
    {
        if (!isDead)
        {
            return;
        }

        isDead = false;

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }
}
