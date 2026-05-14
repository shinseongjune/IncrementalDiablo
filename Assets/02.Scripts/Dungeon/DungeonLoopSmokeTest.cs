using System.Text;
using UnityEngine;
using UnityObject = UnityEngine.Object;

public static class DungeonLoopSmokeTest
{
    public static bool TryRun(out string report)
    {
        StringBuilder builder = new StringBuilder(640);

        ExpeditionDirector expedition = UnityObject.FindAnyObjectByType<ExpeditionDirector>();
        SimpleInventory inventory = UnityObject.FindAnyObjectByType<SimpleInventory>();
        DefenseSaveManager saveManager = UnityObject.FindAnyObjectByType<DefenseSaveManager>();
        EquipmentSlots equipmentSlots = FindEquipmentSlots();
        CharacterStats characterStats = equipmentSlots == null
            ? UnityObject.FindAnyObjectByType<CharacterStats>()
            : equipmentSlots.GetComponent<CharacterStats>();

        if (expedition == null)
        {
            report = "Playable loop smoke test blocked: ExpeditionDirector is missing.";
            return false;
        }

        if (inventory == null)
        {
            report = "Playable loop smoke test blocked: SimpleInventory is missing.";
            return false;
        }

        if (saveManager == null)
        {
            report = "Playable loop smoke test blocked: DefenseSaveManager is missing.";
            return false;
        }

        if (equipmentSlots == null)
        {
            report = "Playable loop smoke test blocked: EquipmentSlots is missing.";
            return false;
        }

        int startingInventoryCount = inventory.Count;
        AppendStep(builder, $"Start inventory: {startingInventoryCount}/{inventory.Capacity}.");

        if (!TryClearExpedition(expedition, builder, out string clearFailure))
        {
            report = Finish(false, builder, clearFailure);
            return false;
        }

        if (inventory.Count <= startingInventoryCount)
        {
            report = Finish(false, builder, "Dungeon clear did not add an item to inventory.");
            return false;
        }

        ItemInstance latestItem = GetLatestItem(inventory);
        if (latestItem == null)
        {
            report = Finish(false, builder, "Inventory count increased but no latest item could be found.");
            return false;
        }

        if (!inventory.TryEquip(latestItem.InstanceId, equipmentSlots, out string equipFailure))
        {
            report = Finish(false, builder, $"Equip failed for {latestItem.DisplayName}: {equipFailure}.");
            return false;
        }

        long equippedId = latestItem.InstanceId;
        AppendStep(builder, $"Equipped #{equippedId} {latestItem.DisplayName}.");
        AppendStats(builder, characterStats, "Equipped stats");

        if (!saveManager.TrySave())
        {
            report = Finish(false, builder, "Save failed after equipping the dungeon reward.");
            return false;
        }

        if (!saveManager.TryValidateSavedFile(out string savedFileReport))
        {
            report = Finish(false, builder, $"Saved file validation failed: {savedFileReport}");
            return false;
        }

        AppendStep(builder, "Saved file validation passed.");

        inventory.UnequipAll(equipmentSlots);
        AppendStep(builder, "Cleared live equipped flags before load restore check.");

        if (!saveManager.TryLoad())
        {
            report = Finish(false, builder, "Load failed after saving the smoke-test snapshot.");
            return false;
        }

        if (!saveManager.TryValidateCurrentSaveData(out string snapshotReport))
        {
            report = Finish(false, builder, $"Loaded snapshot validation failed: {snapshotReport}");
            return false;
        }

        if (!inventory.TryGet(equippedId, out ItemInstance restoredItem))
        {
            report = Finish(false, builder, $"Loaded inventory is missing equipped item id {equippedId}.");
            return false;
        }

        if (!restoredItem.Equipped)
        {
            report = Finish(false, builder, $"Loaded item #{equippedId} is present but not marked equipped.");
            return false;
        }

        if (!Contains(equipmentSlots.GetEquippedItemInstanceIds(), equippedId))
        {
            report = Finish(false, builder, $"EquipmentSlots did not restore equipped item id {equippedId}.");
            return false;
        }

        AppendStep(builder, $"Load restored equipped item #{equippedId}.");
        AppendStats(builder, characterStats, "Restored stats");
        report = Finish(true, builder, "Save/load playable-loop smoke test passed.");
        return true;
    }

    private static bool TryClearExpedition(ExpeditionDirector expedition, StringBuilder builder, out string failure)
    {
        failure = string.Empty;

        if (!expedition.IsRunning && !expedition.StartExpedition())
        {
            failure = "Expedition could not be started.";
            return false;
        }

        AppendStep(builder, "Started dungeon expedition.");

        int guard = Mathf.Max(1, expedition.TotalRooms) + 2;
        while (expedition.IsRunning && guard > 0)
        {
            if (!expedition.CompleteRoom())
            {
                failure = "Expedition room clear call failed while the run was active.";
                return false;
            }

            guard--;
        }

        if (expedition.IsRunning)
        {
            failure = "Expedition did not leave Running state after clearing guarded rooms.";
            return false;
        }

        if (expedition.State != DungeonRunState.Cleared)
        {
            failure = $"Expedition ended in {expedition.State}, not Cleared.";
            return false;
        }

        if (expedition.RewardPending && !expedition.TryGrantPendingReward())
        {
            failure = "Expedition cleared but pending reward could not be granted.";
            return false;
        }

        AppendStep(builder, "Cleared dungeon and granted reward.");
        return true;
    }

    private static ItemInstance GetLatestItem(SimpleInventory inventory)
    {
        if (inventory == null || inventory.Items.Count == 0)
        {
            return null;
        }

        return inventory.Items[inventory.Items.Count - 1];
    }

    private static EquipmentSlots FindEquipmentSlots()
    {
        PlayerController player = UnityObject.FindAnyObjectByType<PlayerController>();
        if (player != null && player.TryGetComponent(out EquipmentSlots playerEquipmentSlots))
        {
            return playerEquipmentSlots;
        }

        return UnityObject.FindAnyObjectByType<EquipmentSlots>();
    }

    private static bool Contains(long[] values, long target)
    {
        if (values == null)
        {
            return false;
        }

        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] == target)
            {
                return true;
            }
        }

        return false;
    }

    private static void AppendStats(StringBuilder builder, CharacterStats characterStats, string label)
    {
        if (characterStats == null)
        {
            AppendStep(builder, $"{label}: CharacterStats missing.");
            return;
        }

        AppendStep(
            builder,
            $"{label}: ATK {characterStats.GetValue(StatId.AttackDamage):0.#}, HP {characterStats.GetValue(StatId.MaxHealth):0.#}, APS {characterStats.GetValue(StatId.AttackSpeed):0.##}.");
    }

    private static void AppendStep(StringBuilder builder, string message)
    {
        builder.AppendLine(message);
    }

    private static string Finish(bool passed, StringBuilder builder, string finalMessage)
    {
        builder.Insert(0, passed ? "Playable loop smoke test passed.\n" : "Playable loop smoke test blocked.\n");
        builder.Append(finalMessage);
        return builder.ToString();
    }
}
