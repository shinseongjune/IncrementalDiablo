using UnityEngine;

[RequireComponent(typeof(CharacterStats))]
public class Health : MonoBehaviour
{
    private CharacterStats stats;
    private float current;

    public float Current => current;
    public float Max => stats.GetValue(StatId.MaxHealth);
    public bool IsAlive => current > 0f;

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        current = Max;
    }

    public void TakeDamage(float amount)
    {
        if (!IsAlive)
        {
            return;
        }

        current = Mathf.Max(0f, current - amount);
    }

    public void Heal(float amount)
    {
        if (!IsAlive)
        {
            return;
        }

        current = Mathf.Min(Max, current + amount);
    }

    public void Refill()
    {
        current = Max;
    }
}
