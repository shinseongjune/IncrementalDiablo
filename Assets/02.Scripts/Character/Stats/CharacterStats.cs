using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float moveSpeed = 4f;

    [Header("Combat")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackCooldown = 1f;

    public float GetValue(StatId statId)
    {
        switch (statId)
        {
            case StatId.MaxHealth:
                return Mathf.Max(1f, maxHealth);
            case StatId.AttackDamage:
                return Mathf.Max(0f, attackDamage);
            case StatId.AttackRange:
                return Mathf.Max(0f, attackRange);
            case StatId.AttackCooldown:
                return Mathf.Max(0.05f, attackCooldown);
            case StatId.MoveSpeed:
                return Mathf.Max(0f, moveSpeed);
            default:
                Debug.LogWarning($"Unhandled stat id: {statId}", this);
                return 0f;
        }
    }
}
