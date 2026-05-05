using System;
using UnityEngine;

public class ItemSalvageService : MonoBehaviour
{
    [SerializeField] private CurrencyWallet wallet;

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
    }
}
