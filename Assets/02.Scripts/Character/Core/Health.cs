using UnityEngine;

[RequireComponent(typeof(CharacterStats))]
public class Health : MonoBehaviour
{
    private CharacterStats stats;
    private float current;
    private bool initialized;

    public event System.Action<float, float> Damaged;
    public event System.Action Died;
    public event System.Action Refilled;

    public float Current
    {
        get
        {
            EnsureInitialized();
            return current;
        }
    }

    public float Max
    {
        get
        {
            ResolveStats();
            return stats == null ? 0f : stats.GetValue(StatId.MaxHealth);
        }
    }

    public bool IsAlive
    {
        get
        {
            EnsureInitialized();
            return current > 0f;
        }
    }

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnEnable()
    {
        ResolveStats();
        EnsureInitialized();

        if (stats != null)
        {
            stats.Changed += HandleStatsChanged;
        }
    }

    private void OnDisable()
    {
        if (stats != null)
        {
            stats.Changed -= HandleStatsChanged;
        }
    }

    public void TakeDamage(float amount)
    {
        EnsureInitialized();

        if (!IsAlive)
        {
            return;
        }

        float previous = current;
        current = Mathf.Max(0f, current - amount);

        if (Mathf.Approximately(previous, current))
        {
            return;
        }

        Damaged?.Invoke(previous - current, current);

        if (current <= 0f)
        {
            Died?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        EnsureInitialized();

        if (!IsAlive)
        {
            return;
        }

        current = Mathf.Min(Max, current + amount);
    }

    public void Refill()
    {
        EnsureInitialized();
        float previous = current;
        current = Max;

        if (!Mathf.Approximately(previous, current))
        {
            Refilled?.Invoke();
        }
    }

    private void HandleStatsChanged()
    {
        EnsureInitialized();
        current = Mathf.Min(current, Max);
    }

    private void ResolveStats()
    {
        if (stats == null)
        {
            stats = GetComponent<CharacterStats>();
        }
    }

    private void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        ResolveStats();
        if (stats == null)
        {
            return;
        }

        current = stats.GetValue(StatId.MaxHealth);
        initialized = true;
    }
}
