using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class CurrencyWallet : MonoBehaviour
{
    [SerializeField] private ResourceAmount[] startingAmounts = new ResourceAmount[0];

    private readonly Dictionary<ResourceId, int> amounts = new Dictionary<ResourceId, int>();
    private bool initialized;

    public event Action Changed;
    public event Action<ResourceId, int> ResourceChanged;

    private void Awake()
    {
        Initialize();
    }

    public int GetAmount(ResourceId resource)
    {
        EnsureInitialized();
        return amounts.TryGetValue(resource, out int amount) ? amount : 0;
    }

    public void SetAmount(ResourceId resource, int amount)
    {
        EnsureInitialized();

        int nextAmount = Mathf.Max(0, amount);
        if (GetAmount(resource) == nextAmount)
        {
            return;
        }

        amounts[resource] = nextAmount;
        ResourceChanged?.Invoke(resource, nextAmount);
        Changed?.Invoke();
    }

    public void Add(ResourceId resource, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        long nextAmount = (long)GetAmount(resource) + amount;
        SetAmount(resource, nextAmount > int.MaxValue ? int.MaxValue : (int)nextAmount);
    }

    public void Add(ResourceAmount[] rewards)
    {
        if (rewards == null)
        {
            return;
        }

        for (int i = 0; i < rewards.Length; i++)
        {
            Add(rewards[i].Resource, rewards[i].Amount);
        }
    }

    public bool CanSpend(ResourceId resource, int amount)
    {
        return amount <= 0 || GetAmount(resource) >= amount;
    }

    public bool CanSpend(ResourceAmount[] costs)
    {
        if (costs == null)
        {
            return true;
        }

        Dictionary<ResourceId, int> totalCosts = new Dictionary<ResourceId, int>();

        for (int i = 0; i < costs.Length; i++)
        {
            ResourceId resource = costs[i].Resource;
            int amount = costs[i].Amount;

            if (amount <= 0)
            {
                continue;
            }

            totalCosts[resource] = totalCosts.TryGetValue(resource, out int currentTotal)
                ? currentTotal + amount
                : amount;
        }

        foreach (KeyValuePair<ResourceId, int> totalCost in totalCosts)
        {
            if (!CanSpend(totalCost.Key, totalCost.Value))
            {
                return false;
            }
        }

        return true;
    }

    public bool TrySpend(ResourceId resource, int amount)
    {
        if (!CanSpend(resource, amount))
        {
            return false;
        }

        if (amount > 0)
        {
            SetAmount(resource, GetAmount(resource) - amount);
        }

        return true;
    }

    public bool TrySpend(ResourceAmount[] costs)
    {
        if (!CanSpend(costs))
        {
            return false;
        }

        if (costs == null)
        {
            return true;
        }

        for (int i = 0; i < costs.Length; i++)
        {
            TrySpend(costs[i].Resource, costs[i].Amount);
        }

        return true;
    }

    public string FormatAll()
    {
        EnsureInitialized();

        ResourceId[] resourceIds = (ResourceId[])Enum.GetValues(typeof(ResourceId));
        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < resourceIds.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(" / ");
            }

            ResourceId resource = resourceIds[i];
            builder.Append(resource);
            builder.Append(": ");
            builder.Append(GetAmount(resource));
        }

        return builder.ToString();
    }

    public ResourceAmount[] ExportAmounts()
    {
        EnsureInitialized();

        ResourceId[] resourceIds = (ResourceId[])Enum.GetValues(typeof(ResourceId));
        ResourceAmount[] exportedAmounts = new ResourceAmount[resourceIds.Length];

        for (int i = 0; i < resourceIds.Length; i++)
        {
            ResourceId resource = resourceIds[i];
            exportedAmounts[i] = new ResourceAmount(resource, GetAmount(resource));
        }

        return exportedAmounts;
    }

    public void ImportAmounts(ResourceAmount[] savedAmounts)
    {
        amounts.Clear();

        if (savedAmounts != null)
        {
            for (int i = 0; i < savedAmounts.Length; i++)
            {
                ResourceAmount savedAmount = savedAmounts[i];
                ResourceId resource = savedAmount.Resource;
                long nextAmount = (long)GetAmountWithoutInitializing(resource) + savedAmount.Amount;
                amounts[resource] = nextAmount > int.MaxValue ? int.MaxValue : (int)nextAmount;
            }
        }

        initialized = true;

        ResourceId[] resourceIds = (ResourceId[])Enum.GetValues(typeof(ResourceId));
        for (int i = 0; i < resourceIds.Length; i++)
        {
            ResourceId resource = resourceIds[i];
            ResourceChanged?.Invoke(resource, GetAmountWithoutInitializing(resource));
        }

        Changed?.Invoke();
    }

    private void EnsureInitialized()
    {
        if (!initialized)
        {
            Initialize();
        }
    }

    private void Initialize()
    {
        amounts.Clear();

        if (startingAmounts != null)
        {
            for (int i = 0; i < startingAmounts.Length; i++)
            {
                ResourceAmount amount = startingAmounts[i];
                amounts[amount.Resource] = GetAmountWithoutInitializing(amount.Resource) + amount.Amount;
            }
        }

        initialized = true;
        Changed?.Invoke();
    }

    private int GetAmountWithoutInitializing(ResourceId resource)
    {
        return amounts.TryGetValue(resource, out int amount) ? amount : 0;
    }
}
