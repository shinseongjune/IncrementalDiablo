using UnityEngine;

[RequireComponent(typeof(CharacterStats))]
public class CombatDriver : MonoBehaviour
{
    private CharacterStats stats;
    private float nextAttackTime;

    public event System.Action BasicAttackPerformed;

    public bool IsCoolingDown => Time.time < nextAttackTime;
    public float AttackRange => stats == null ? 0f : stats.GetValue(StatId.AttackRange);

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
    }

    public bool TryBasicAttack(Health target)
    {
        if (!CanAttack(target))
        {
            return false;
        }

        target.TakeDamage(stats.GetValue(StatId.AttackDamage));
        nextAttackTime = Time.time + stats.GetValue(StatId.AttackCooldown);
        BasicAttackPerformed?.Invoke();
        return true;
    }

    public bool TryPlayBasicAttackInPlace()
    {
        if (IsCoolingDown)
        {
            return false;
        }

        nextAttackTime = Time.time + stats.GetValue(StatId.AttackCooldown);
        BasicAttackPerformed?.Invoke();
        return true;
    }

    public bool CanAttack(Health target)
    {
        if (target == null || !target.IsAlive || IsCoolingDown)
        {
            return false;
        }

        return IsInRange(target.transform);
    }

    public bool IsInRange(Transform target)
    {
        if (target == null)
        {
            return false;
        }

        return Vector3.Distance(transform.position, target.position) <= AttackRange;
    }
}
