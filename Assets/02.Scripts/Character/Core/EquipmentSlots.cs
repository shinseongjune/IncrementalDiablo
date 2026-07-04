using System;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentSlots : MonoBehaviour
{
    [SerializeField] private ItemDefinition weapon;
    [SerializeField] private ItemDefinition armor;
    [SerializeField] private ItemDefinition ring;

    private ItemInstance weaponItem;
    private ItemInstance armorItem;
    private ItemInstance ringItem;

    public event Action Changed;

    public ItemDefinition Weapon => weapon;
    public ItemDefinition Armor => armor;
    public ItemDefinition Ring => ring;
    public ItemInstance WeaponItem => weaponItem;
    public ItemInstance ArmorItem => armorItem;
    public ItemInstance RingItem => ringItem;

    private void OnValidate()
    {
        if (weapon != null && weapon.Slot != ItemSlot.Weapon)
        {
            weapon = null;
        }

        if (armor != null && armor.Slot != ItemSlot.Armor)
        {
            armor = null;
        }

        if (ring != null && ring.Slot != ItemSlot.Ring)
        {
            ring = null;
        }
    }

    public ItemDefinition GetEquipped(ItemSlot slot)
    {
        switch (slot)
        {
            case ItemSlot.Weapon:
                return weapon;
            case ItemSlot.Armor:
                return armor;
            case ItemSlot.Ring:
                return ring;
            default:
                return null;
        }
    }

    public bool TryEquip(ItemDefinition item)
    {
        if (item == null)
        {
            return false;
        }

        SetEquipped(item.Slot, item, null);
        return true;
    }

    public bool TryEquip(ItemInstance item)
    {
        if (item == null)
        {
            return false;
        }

        SetEquipped(item.Slot, item.Definition, item);
        return true;
    }

    public void Unequip(ItemSlot slot)
    {
        SetEquipped(slot, null, null);
    }

    public void UnequipAll()
    {
        Unequip(ItemSlot.Weapon);
        Unequip(ItemSlot.Armor);
        Unequip(ItemSlot.Ring);
    }

    public void RefreshEquippedModifiers()
    {
        Changed?.Invoke();
    }

    public long[] GetEquippedItemInstanceIds()
    {
        List<long> results = new List<long>(3);
        AppendEquippedItemInstanceId(weaponItem, results);
        AppendEquippedItemInstanceId(armorItem, results);
        AppendEquippedItemInstanceId(ringItem, results);
        return results.ToArray();
    }

    public void AppendModifiers(StatId statId, List<StatMod> results)
    {
        if (results == null)
        {
            return;
        }

        AppendEquipmentModifiers(weapon, weaponItem, statId, results);
        AppendEquipmentModifiers(armor, armorItem, statId, results);
        AppendEquipmentModifiers(ring, ringItem, statId, results);
    }

    private void SetEquipped(ItemSlot slot, ItemDefinition item, ItemInstance itemInstance)
    {
        if (item != null && item.Slot != slot)
        {
            return;
        }

        if (itemInstance != null && itemInstance.Slot != slot)
        {
            return;
        }

        ItemInstance previousItem = GetEquippedItem(slot);
        switch (slot)
        {
            case ItemSlot.Weapon:
                if (weapon == item && weaponItem == itemInstance)
                {
                    return;
                }

                weapon = item;
                weaponItem = itemInstance;
                break;
            case ItemSlot.Armor:
                if (armor == item && armorItem == itemInstance)
                {
                    return;
                }

                armor = item;
                armorItem = itemInstance;
                break;
            case ItemSlot.Ring:
                if (ring == item && ringItem == itemInstance)
                {
                    return;
                }

                ring = item;
                ringItem = itemInstance;
                break;
        }

        if (previousItem != null && previousItem != itemInstance)
        {
            previousItem.SetEquipped(false);
        }

        itemInstance?.SetEquipped(true);
        Changed?.Invoke();
    }

    public ItemInstance GetEquippedItem(ItemSlot slot)
    {
        switch (slot)
        {
            case ItemSlot.Weapon:
                return weaponItem;
            case ItemSlot.Armor:
                return armorItem;
            case ItemSlot.Ring:
                return ringItem;
            default:
                return null;
        }
    }

    private static void AppendEquippedItemInstanceId(ItemInstance item, List<long> results)
    {
        if (item != null && item.InstanceId > 0)
        {
            results.Add(item.InstanceId);
        }
    }

    private static void AppendEquipmentModifiers(ItemDefinition definition, ItemInstance itemInstance, StatId statId, List<StatMod> results)
    {
        if (itemInstance != null)
        {
            itemInstance.AppendModifiers(statId, results);
            return;
        }

        AppendDefinitionModifiers(definition, statId, results);
    }

    private static void AppendDefinitionModifiers(ItemDefinition item, StatId statId, List<StatMod> results)
    {
        if (item == null)
        {
            return;
        }

        StatMod[] modifiers = item.Modifiers;
        if (modifiers == null)
        {
            return;
        }

        for (int i = 0; i < modifiers.Length; i++)
        {
            StatMod modifier = modifiers[i];
            if (modifier != null && modifier.AppliesTo(statId))
            {
                results.Add(modifier);
            }
        }
    }
}
