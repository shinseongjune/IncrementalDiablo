using System;
using UnityEngine;

public class ItemSalvageService : MonoBehaviour
{
    [SerializeField] private CurrencyWallet wallet;
    [SerializeField] private SimpleInventory inventory;

    public event Action<ItemDefinition, ResourceAmount[]> Salvaged;

    public bool TrySalvage(ItemDefinition item)
    {
        return TrySalvage(item, out _);
    }

    public bool TrySalvage(ItemDefinition item, out ResourceAmount[] rewards)
    {
        rewards = ItemEconomyModel.GetSalvageRewards(item);

        if (item == null)
        {
            return false;
        }

        ResolveWallet();
        if (wallet == null)
        {
            Debug.LogWarning("ItemSalvageService needs a CurrencyWallet before it can salvage items.", this);
            return false;
        }

        wallet.Add(rewards);
        Salvaged?.Invoke(item, rewards);
        return true;
    }

    public bool TrySalvage(ItemInstance item)
    {
        return TrySalvage(item, out _);
    }

    public bool TrySalvage(ItemInstance item, out ResourceAmount[] rewards)
    {
        rewards = new ResourceAmount[0];

        if (item == null)
        {
            return false;
        }

        ItemDefinition definition = item.Definition;
        if (definition == null)
        {
            Debug.LogWarning("ItemSalvageService cannot salvage an item instance without a loaded ItemDefinition.", this);
            return false;
        }

        rewards = ItemEconomyModel.GetSalvageRewards(definition);

        ResolveWallet();
        if (wallet == null)
        {
            Debug.LogWarning("ItemSalvageService needs a CurrencyWallet before it can salvage items.", this);
            return false;
        }

        if (inventory != null && !inventory.Remove(item.InstanceId))
        {
            Debug.LogWarning($"ItemSalvageService could not remove item instance {item.InstanceId} from the inventory.", this);
            return false;
        }

        wallet.Add(rewards);
        Salvaged?.Invoke(definition, rewards);
        return true;
    }

    private void Reset()
    {
        ResolveWallet();
    }

    private void ResolveWallet()
    {
        if (wallet == null)
        {
            wallet = GetComponent<CurrencyWallet>();
        }

        if (wallet == null)
        {
            wallet = FindAnyObjectByType<CurrencyWallet>();
        }

        if (inventory == null)
        {
            inventory = GetComponent<SimpleInventory>();
        }

        if (inventory == null)
        {
            inventory = FindAnyObjectByType<SimpleInventory>();
        }
    }
}
