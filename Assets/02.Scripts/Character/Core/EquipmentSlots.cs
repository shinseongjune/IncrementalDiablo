using System;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentSlots : MonoBehaviour
{
    [SerializeField] private ItemDefinition weapon;
    [SerializeField] private ItemDefinition armor;
    [SerializeField] private ItemDefinition ring;

    public event Action Changed;

    public ItemDefinition Weapon => weapon;
    public ItemDefinition Armor => armor;
    public ItemDefinition Ring => ring;

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

        SetEquipped(item.Slot, item);
        return true;
    }

    public void Unequip(ItemSlot slot)
    {
        SetEquipped(slot, null);
    }

    public void AppendModifiers(StatId statId, List<StatMod> results)
    {
        if (results == null)
        {
            return;
        }

        AppendItemModifiers(weapon, statId, results);
        AppendItemModifiers(armor, statId, results);
        AppendItemModifiers(ring, statId, results);
    }

    private void SetEquipped(ItemSlot slot, ItemDefinition item)
    {
        if (item != null && item.Slot != slot)
        {
            return;
        }

        switch (slot)
        {
            case ItemSlot.Weapon:
                if (weapon == item)
                {
                    return;
                }

                weapon = item;
                break;
            case ItemSlot.Armor:
                if (armor == item)
                {
                    return;
                }

                armor = item;
                break;
            case ItemSlot.Ring:
                if (ring == item)
                {
                    return;
                }

                ring = item;
                break;
        }

        Changed?.Invoke();
    }

    private static void AppendItemModifiers(ItemDefinition item, StatId statId, List<StatMod> results)
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
