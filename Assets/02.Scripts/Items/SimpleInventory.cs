using System;
using System.Collections.Generic;
using UnityEngine;

public class SimpleInventory : MonoBehaviour
{
    [SerializeField] private int capacity = 60;
    [SerializeField] private ItemInstance[] startingItems = new ItemInstance[0];
    [SerializeField] private ItemDefinition[] knownDefinitions = new ItemDefinition[0];
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

    public void RegisterDefinition(ItemDefinition definition)
    {
        if (definition == null)
        {
            return;
        }

        knownDefinitions ??= new ItemDefinition[0];
        for (int i = 0; i < knownDefinitions.Length; i++)
        {
            ItemDefinition knownDefinition = knownDefinitions[i];
            if (knownDefinition == null)
            {
                knownDefinitions[i] = definition;
                return;
            }

            if (knownDefinition.Id == definition.Id)
            {
                return;
            }
        }

        Array.Resize(ref knownDefinitions, knownDefinitions.Length + 1);
        knownDefinitions[knownDefinitions.Length - 1] = definition;
    }

    public void RegisterDefinitions(ItemDefinition[] definitions)
    {
        if (definitions == null)
        {
            return;
        }

        for (int i = 0; i < definitions.Length; i++)
        {
            RegisterDefinition(definitions[i]);
        }
    }

    public bool TryAdd(ItemInstance item)
    {
        EnsureInitialized();

        if (item == null || items.Count >= Capacity)
        {
            return false;
        }

        RegisterDefinition(item.Definition);
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

    public void NotifyItemsChanged()
    {
        EnsureInitialized();
        Changed?.Invoke();
    }

    public bool TryEquip(long instanceId, EquipmentSlots equipmentSlots, out string failureReason)
    {
        failureReason = string.Empty;

        if (!TryGet(instanceId, out ItemInstance item))
        {
            failureReason = "item is not in inventory";
            return false;
        }

        if (equipmentSlots == null)
        {
            failureReason = "EquipmentSlots is missing";
            return false;
        }

        if (!equipmentSlots.TryEquip(item))
        {
            failureReason = "EquipmentSlots rejected the item snapshot";
            return false;
        }

        MarkOnlyEquippedInSlot(item);
        Changed?.Invoke();
        return true;
    }

    public void UnequipAll(EquipmentSlots equipmentSlots = null)
    {
        EnsureInitialized();

        for (int i = 0; i < items.Count; i++)
        {
            items[i].SetEquipped(false);
        }

        equipmentSlots?.UnequipAll();
        Changed?.Invoke();
    }

    public long[] GetEquippedItemInstanceIds()
    {
        EnsureInitialized();

        List<long> results = new List<long>(3);
        for (int i = 0; i < items.Count; i++)
        {
            ItemInstance item = items[i];
            if (item != null && item.Equipped && item.InstanceId > 0)
            {
                results.Add(item.InstanceId);
            }
        }

        return results.ToArray();
    }

    public int RestoreEquipment(EquipmentSlots equipmentSlots, long[] preferredEquippedItemInstanceIds, out int restoredCount)
    {
        EnsureInitialized();
        restoredCount = 0;

        if (equipmentSlots == null)
        {
            return 0;
        }

        List<ItemInstance> candidates = CollectEquipmentRestoreCandidates(preferredEquippedItemInstanceIds);
        ClearEquippedFlags();
        equipmentSlots.UnequipAll();

        List<ItemSlot> restoredSlots = new List<ItemSlot>(3);
        int snapshotDefinitionCount = 0;
        for (int i = 0; i < candidates.Count; i++)
        {
            ItemInstance item = candidates[i];
            if (item == null || ContainsSlot(restoredSlots, item.Slot))
            {
                continue;
            }

            if (equipmentSlots.TryEquip(item))
            {
                MarkOnlyEquippedInSlot(item);
                restoredSlots.Add(item.Slot);
                restoredCount++;

                if (item.Definition == null)
                {
                    snapshotDefinitionCount++;
                }
            }
        }

        Changed?.Invoke();
        return snapshotDefinitionCount;
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

                item.TrySetDefinition(FindKnownDefinition(item.DefinitionId));
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

    private ItemDefinition FindKnownDefinition(string definitionId)
    {
        if (string.IsNullOrWhiteSpace(definitionId) || knownDefinitions == null)
        {
            return null;
        }

        for (int i = 0; i < knownDefinitions.Length; i++)
        {
            ItemDefinition definition = knownDefinitions[i];
            if (definition != null && definition.Id == definitionId)
            {
                return definition;
            }
        }

        return null;
    }

    private void MarkOnlyEquippedInSlot(ItemInstance equippedItem)
    {
        if (equippedItem == null)
        {
            return;
        }

        for (int i = 0; i < items.Count; i++)
        {
            ItemInstance item = items[i];
            if (item != null && item.Slot == equippedItem.Slot)
            {
                item.SetEquipped(item.InstanceId == equippedItem.InstanceId);
            }
        }
    }

    private void ClearEquippedFlags()
    {
        for (int i = 0; i < items.Count; i++)
        {
            items[i].SetEquipped(false);
        }
    }

    private List<ItemInstance> CollectEquipmentRestoreCandidates(long[] preferredEquippedItemInstanceIds)
    {
        List<ItemInstance> candidates = new List<ItemInstance>(3);

        if (preferredEquippedItemInstanceIds != null && preferredEquippedItemInstanceIds.Length > 0)
        {
            for (int i = 0; i < preferredEquippedItemInstanceIds.Length; i++)
            {
                long instanceId = preferredEquippedItemInstanceIds[i];
                if (instanceId <= 0 || ContainsCandidate(candidates, instanceId))
                {
                    continue;
                }

                if (TryGet(instanceId, out ItemInstance item))
                {
                    candidates.Add(item);
                }
            }
        }

        if (candidates.Count > 0)
        {
            return candidates;
        }

        for (int i = 0; i < items.Count; i++)
        {
            ItemInstance item = items[i];
            if (item != null && item.Equipped)
            {
                candidates.Add(item);
            }
        }

        return candidates;
    }

    private static bool ContainsCandidate(List<ItemInstance> candidates, long instanceId)
    {
        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i] != null && candidates[i].InstanceId == instanceId)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsSlot(List<ItemSlot> slots, ItemSlot slot)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == slot)
            {
                return true;
            }
        }

        return false;
    }
}
