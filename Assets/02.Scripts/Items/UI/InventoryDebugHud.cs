using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryDebugHud : MonoBehaviour
{
    [SerializeField] private SimpleInventory inventory;
    [SerializeField] private ItemSalvageService salvageService;
    [SerializeField] private EquipmentSlots equipmentSlots;
    [SerializeField] private CharacterStats characterStats;
    [SerializeField] private CurrencyWallet wallet;
    [SerializeField] private bool autoFindReferences = true;
    [SerializeField] private bool showPanel = true;
    [SerializeField] private Rect panelRect = new Rect(412f, 16f, 400f, 292f);
    [SerializeField] private string lastActionMessage;

    private void Reset()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        panelRect.width = Mathf.Max(300f, panelRect.width);
        panelRect.height = Mathf.Max(190f, panelRect.height);
    }

    private void OnGUI()
    {
        if (!showPanel)
        {
            return;
        }

        ResolveReferences();

        GUILayout.BeginArea(panelRect, GUI.skin.window);
        GUILayout.Label("Inventory Loop Debug");

        if (inventory == null)
        {
            GUILayout.Label("SimpleInventory: missing");
            if (GUILayout.Button("Refresh References"))
            {
                ResolveReferences(true);
            }

            GUILayout.EndArea();
            return;
        }

        ItemInstance latest = GetLatestItem();
        GUILayout.Label($"Inventory: {inventory.Count}/{inventory.Capacity}");
        GUILayout.Label(wallet == null ? "Wallet: missing" : wallet.FormatAll());
        DrawHeroStats();

        if (latest == null)
        {
            GUILayout.Label("Latest Item: none");
        }
        else
        {
            GUILayout.Label($"Latest: #{latest.InstanceId} {latest.DisplayName}");
            GUILayout.Label($"{latest.Rarity} {latest.Slot} Lv.{latest.Level} Power {latest.RolledPower} {(latest.Equipped ? "Equipped" : "Stored")}");
        }

        GUILayout.BeginHorizontal();
        if (DrawButton("Equip Latest", latest != null))
        {
            TryEquipLatest(latest);
        }

        if (DrawButton("Salvage Latest", latest != null))
        {
            TrySalvageLatest(latest);
        }

        if (DrawButton("Unequip All", inventory.Count > 0))
        {
            UnequipAll();
        }

        GUILayout.EndHorizontal();

        if (!string.IsNullOrWhiteSpace(lastActionMessage))
        {
            GUILayout.Label(lastActionMessage);
        }

        GUILayout.EndArea();
    }

    private ItemInstance GetLatestItem()
    {
        IReadOnlyList<ItemInstance> items = inventory.Items;
        return items.Count == 0 ? null : items[items.Count - 1];
    }

    private void TryEquipLatest(ItemInstance item)
    {
        if (item == null)
        {
            lastActionMessage = "Equip failed: no item in inventory.";
            return;
        }

        ResolveEquipmentSlots();
        if (!inventory.TryEquip(item.InstanceId, equipmentSlots, out string failureReason))
        {
            lastActionMessage = $"Equip failed: {item.DisplayName} ({failureReason}).";
            return;
        }

        lastActionMessage = $"Equipped {item.DisplayName}.";
    }

    private void TrySalvageLatest(ItemInstance item)
    {
        if (item == null)
        {
            lastActionMessage = "Salvage failed: no item in inventory.";
            return;
        }

        ResolveReferences();
        if (salvageService == null)
        {
            lastActionMessage = "Salvage failed: no ItemSalvageService found.";
            return;
        }

        string itemName = item.DisplayName;
        ItemSlot slot = item.Slot;
        bool wasEquipped = item.Equipped;

        if (!salvageService.TrySalvage(item, out ResourceAmount[] rewards))
        {
            lastActionMessage = $"Salvage failed: {itemName}.";
            return;
        }

        if (wasEquipped)
        {
            ResolveEquipmentSlots();
            equipmentSlots?.Unequip(slot);
        }

        lastActionMessage = $"Salvaged {itemName}: {FormatRewards(rewards)}.";
    }

    private void UnequipAll()
    {
        ResolveEquipmentSlots();
        inventory.UnequipAll(equipmentSlots);

        lastActionMessage = "Cleared equipped flags.";
    }

    private static string FormatRewards(ResourceAmount[] rewards)
    {
        if (rewards == null || rewards.Length == 0)
        {
            return "no materials";
        }

        return string.Join(", ", Array.ConvertAll(rewards, reward => reward.ToString()));
    }

    private void DrawHeroStats()
    {
        ResolveCharacterStats();
        if (characterStats == null)
        {
            GUILayout.Label("Hero Stats: missing");
            return;
        }

        GUILayout.Label(
            $"Hero Stats: ATK {characterStats.GetValue(StatId.AttackDamage):0.#} / HP {characterStats.GetValue(StatId.MaxHealth):0.#} / APS {characterStats.GetValue(StatId.AttackSpeed):0.##} / CD {characterStats.GetValue(StatId.AttackCooldown):0.##}s");
    }

    private static bool DrawButton(string label, bool enabled)
    {
        bool previousEnabled = GUI.enabled;
        GUI.enabled = previousEnabled && enabled;
        bool clicked = GUILayout.Button(label);
        GUI.enabled = previousEnabled;
        return clicked && enabled;
    }

    private void ResolveReferences(bool force = false)
    {
        if (!autoFindReferences && !force)
        {
            return;
        }

        if (inventory == null || force)
        {
            inventory = FindAnyObjectByType<SimpleInventory>();
        }

        if (salvageService == null || force)
        {
            salvageService = FindAnyObjectByType<ItemSalvageService>();
        }

        if (wallet == null || force)
        {
            wallet = FindAnyObjectByType<CurrencyWallet>();
        }

        if (equipmentSlots == null || force)
        {
            ResolveEquipmentSlots();
        }

        if (characterStats == null || force)
        {
            ResolveCharacterStats();
        }
    }

    private void ResolveEquipmentSlots()
    {
        if (equipmentSlots != null)
        {
            return;
        }

        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null && player.TryGetComponent(out EquipmentSlots playerEquipment))
        {
            equipmentSlots = playerEquipment;
            return;
        }

        EquipmentSlots[] slots = FindObjectsByType<EquipmentSlots>(FindObjectsInactive.Exclude);
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].gameObject.name.Contains("Player", StringComparison.OrdinalIgnoreCase))
            {
                equipmentSlots = slots[i];
                return;
            }
        }

        equipmentSlots = slots.Length == 0 ? null : slots[0];
    }

    private void ResolveCharacterStats()
    {
        if (characterStats != null)
        {
            return;
        }

        if (equipmentSlots != null && equipmentSlots.TryGetComponent(out CharacterStats equipmentStats))
        {
            characterStats = equipmentStats;
            return;
        }

        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null && player.TryGetComponent(out CharacterStats playerStats))
        {
            characterStats = playerStats;
            return;
        }

        characterStats = FindAnyObjectByType<CharacterStats>();
    }
}
