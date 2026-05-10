using System;
using System.Collections.Generic;
using UnityEngine;

public class SimpleInventory : MonoBehaviour
{
    [SerializeField] private int capacity = 60;
    [SerializeField] private ItemInstance[] startingItems = new ItemInstance[0];
    [SerializeField] private long nextItemInstanceId = 1;

    private readonly List<ItemInstance> items = new List<ItemInstance>();
    private bool initialized;

    public event Action Changed;

    public IReadOnlyList<ItemInstance> Items
    {
        get
        {
            EnsureInitialized();
            return items;
        }
    }

    public int Capacity => Mathf.Max(0, capacity);
    public int Count
    {
        get
        {
            EnsureInitialized();
            return items.Count;
        }
    }

    public long NextItemInstanceId => Math.Max(1, nextItemInstanceId);

    private void Awake()
    {
        Initialize();
    }

    public bool TryAdd(ItemDefinition definition, out ItemInstance item)
    {
        item = null;
        if (definition == null)
        {
            return false;
        }

        if (Count >= Capacity)
        {
            return false;
        }

        item = ItemInstance.CreateFromDefinition(ConsumeNextItemInstanceId(), definition, definition.RequiredLevel);
        if (item == null || !TryAdd(item))
        {
            item = null;
            return false;
        }

        return true;
    }

    public bool TryAdd(ItemInstance item)
    {
        EnsureInitialized();

        if (item == null || items.Count >= Capacity)
        {
            return false;
        }

        item.EnsureIdentity(NextItemInstanceId);
        if (Contains(item.InstanceId))
        {
            return false;
        }

        items.Add(item);
        nextItemInstanceId = Math.Max(nextItemInstanceId, item.InstanceId + 1);
        Changed?.Invoke();
        return true;
    }

    public bool Remove(long instanceId)
    {
        EnsureInitialized();

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].InstanceId == instanceId)
            {
                items.RemoveAt(i);
                Changed?.Invoke();
                return true;
            }
        }

        return false;
    }

    public bool Contains(long instanceId)
    {
        EnsureInitialized();

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].InstanceId == instanceId)
            {
                return true;
            }
        }

        return false;
    }

    public bool TryGet(long instanceId, out ItemInstance item)
    {
        EnsureInitialized();

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].InstanceId == instanceId)
            {
                item = items[i];
                return true;
            }
        }

        item = null;
        return false;
    }

    public bool TrySetEquipped(long instanceId, bool equipped)
    {
        if (!TryGet(instanceId, out ItemInstance item))
        {
            return false;
        }

        item.SetEquipped(equipped);
        Changed?.Invoke();
        return true;
    }

    public InventorySaveData CreateSaveData()
    {
        EnsureInitialized();

        ItemInstanceSaveData[] savedItems = new ItemInstanceSaveData[items.Count];
        for (int i = 0; i < items.Count; i++)
        {
            savedItems[i] = items[i].ToSaveData();
        }

        return new InventorySaveData
        {
            nextItemInstanceId = NextItemInstanceId,
            itemInstances = savedItems
        };
    }

    public void ApplySaveData(InventorySaveData saveData)
    {
        items.Clear();
        nextItemInstanceId = Math.Max(1, saveData == null ? 1 : saveData.nextItemInstanceId);

        if (saveData?.itemInstances != null)
        {
            for (int i = 0; i < saveData.itemInstances.Length; i++)
            {
                ItemInstance item = ItemInstance.FromSaveData(saveData.itemInstances[i]);
                if (item == null)
                {
                    continue;
                }

                item.EnsureIdentity(nextItemInstanceId);
                if (items.Count < Capacity && !ContainsWithoutInitializing(item.InstanceId))
                {
                    items.Add(item);
                    nextItemInstanceId = Math.Max(nextItemInstanceId, item.InstanceId + 1);
                }
            }
        }

        initialized = true;
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
        items.Clear();
        nextItemInstanceId = Math.Max(1, nextItemInstanceId);

        if (startingItems != null)
        {
            for (int i = 0; i < startingItems.Length; i++)
            {
                ItemInstance item = startingItems[i];
                if (item == null || items.Count >= Capacity)
                {
                    continue;
                }

                item.EnsureIdentity(NextItemInstanceId);
                if (!ContainsWithoutInitializing(item.InstanceId))
                {
                    items.Add(item);
                    nextItemInstanceId = Math.Max(nextItemInstanceId, item.InstanceId + 1);
                }
            }
        }

        initialized = true;
        Changed?.Invoke();
    }

    private long ConsumeNextItemInstanceId()
    {
        long result = Math.Max(1, nextItemInstanceId);
        nextItemInstanceId = result + 1;
        return result;
    }

    private bool ContainsWithoutInitializing(long instanceId)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].InstanceId == instanceId)
            {
                return true;
            }
        }

        return false;
    }
}
