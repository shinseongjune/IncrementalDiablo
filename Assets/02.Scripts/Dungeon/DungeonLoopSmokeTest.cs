using System.Text;
using UnityEngine;
using UnityObject = UnityEngine.Object;

public static class DungeonLoopSmokeTest
{
    public static bool TryRun(out string report)
    {
        StringBuilder builder = new StringBuilder(640);

        if (!WorldCheckpointSelfTest.TryRun(out string checkpointReport))
        {
            report = checkpointReport;
            return false;
        }

        AppendStep(builder, checkpointReport);

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

        if (!TryValidateExpeditionSnapshotContract(expedition, builder, out string snapshotFailure))
        {
            report = Finish(false, builder, snapshotFailure);
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

        if (!expedition.IsRunning)
        {
            if (!expedition.SelectSecondContract())
            {
                failure = "Expedition could not select the second dungeon contract before start.";
                return false;
            }

            AppendStep(builder, $"Selected dungeon contract {expedition.SelectedContract.DisplayName}.");
        }

        if (!expedition.IsRunning && !expedition.StartExpedition())
        {
            failure = "Expedition could not be started.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(expedition.ActiveContractId))
        {
            failure = "Expedition started without an active dungeon contract.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(expedition.ActiveEncounterId))
        {
            failure = "Expedition started without an active dungeon encounter.";
            return false;
        }

        AppendStep(builder, $"Started dungeon expedition with {expedition.ActiveContract.DisplayName} / {expedition.ActiveEncounter.DisplayName}.");

        if (!expedition.CompleteRoom())
        {
            failure = "Expedition room clear call failed while the run was active.";
            return false;
        }

        if (!expedition.IsAwaitingRoomExit || !expedition.RewardPending)
        {
            failure = "Expedition did not preserve a pending reward and return-or-descend choice after clearing a room.";
            return false;
        }

        int clearedDepth = expedition.Depth;
        int clearedRoomIndex = expedition.CurrentRoomIndex;
        if (!expedition.TryEnterDeeperRoom() || !expedition.IsRunning ||
            expedition.Depth != clearedDepth + 1 || expedition.CurrentRoomIndex != clearedRoomIndex + 1)
        {
            failure = "Deeper exit did not advance the active seeded room plan.";
            return false;
        }

        if (expedition.RunPlan == null || !expedition.RunPlan.rewardPending)
        {
            failure = "Deeper exit did not retain the unbanked reward in DungeonRunPlan.";
            return false;
        }

        if (!expedition.CompleteRoom())
        {
            failure = "Expedition could not clear the room reached through the deeper exit.";
            return false;
        }

        if (!expedition.TryReturnToHub())
        {
            failure = "Return portal could not bank the cleared room reward.";
            return false;
        }

        if (expedition.State != DungeonRunState.Ready)
        {
            failure = $"Return portal ended in {expedition.State}, not Ready.";
            return false;
        }

        AppendStep(builder, "Cleared a room, descended with the pending reward, then banked through the return portal.");
        return true;
    }

    private static bool TryValidateExpeditionSnapshotContract(
        ExpeditionDirector expedition,
        StringBuilder builder,
        out string failure)
    {
        failure = string.Empty;
        if (!expedition.IsSnapshotReady)
        {
            failure = "Expedition snapshot is not ready before the smoke test starts.";
            return false;
        }

        DungeonRunPlan plan = DungeonRunPlan.CreateNew("snapshot_test", 987654, 3);
        plan.AssignCurrentRoomTemplate("crypt_a");
        DungeonExpeditionSnapshot running = new DungeonExpeditionSnapshot
        {
            state = DungeonRunState.Running,
            resumePoint = DungeonRoomResumePoint.RestartCurrentRoom,
            dungeonId = "snapshot_test",
            depth = 3,
            selectedDepth = 3,
            highestUnlockedDepth = 3,
            totalRooms = 1,
            currentRoomIndex = 0,
            hasRunPlan = true,
            runPlan = plan,
            rewardPending = false
        };

        if (!running.TryValidate(out string runningError))
        {
            failure = $"Running expedition snapshot is invalid: {runningError}";
            return false;
        }

        DungeonExpeditionSnapshot restoredRunning = running.Clone();
        if (!restoredRunning.TryValidate(out string restoredRunningError) ||
            restoredRunning.state != DungeonRunState.Running ||
            restoredRunning.resumePoint != DungeonRoomResumePoint.RestartCurrentRoom)
        {
            failure = $"Running expedition snapshot did not survive a direct clone: {restoredRunningError}";
            return false;
        }

        DungeonExpeditionSnapshot awaitingExit = running.Clone();
        awaitingExit.state = DungeonRunState.AwaitingExit;
        awaitingExit.resumePoint = DungeonRoomResumePoint.AwaitingExit;
        awaitingExit.rewardPending = true;
        awaitingExit.runPlan.SetRewardPending(true, 3);
        if (!awaitingExit.TryValidate(out string awaitingError))
        {
            failure = $"Awaiting-exit expedition snapshot is invalid: {awaitingError}";
            return false;
        }

        DungeonExpeditionSnapshot restoredAwaitingExit = awaitingExit.Clone();
        if (!restoredAwaitingExit.TryValidate(out string restoredAwaitingError) ||
            !restoredAwaitingExit.rewardPending ||
            restoredAwaitingExit.resumePoint != DungeonRoomResumePoint.AwaitingExit)
        {
            failure = $"Awaiting-exit expedition snapshot did not survive a direct clone: {restoredAwaitingError}";
            return false;
        }

        DungeonExpeditionSnapshot staleReady = new DungeonExpeditionSnapshot
        {
            state = DungeonRunState.Ready,
            resumePoint = DungeonRoomResumePoint.None,
            dungeonId = "snapshot_test",
            depth = 3,
            selectedDepth = 3,
            highestUnlockedDepth = 3,
            totalRooms = 1,
            hasRunPlan = true,
            runPlan = DungeonRunPlan.CreateNew("snapshot_test", 654321, 3)
        };

        if (staleReady.TryValidate(out _))
        {
            failure = "Stale Ready expedition snapshot unexpectedly passed validation.";
            return false;
        }

        AppendStep(builder, "Validated direct running and awaiting-exit snapshots; stale Ready data is rejected without repair.");
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
