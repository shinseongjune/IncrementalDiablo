using System;
using UnityEngine;

/// <summary>
/// The only persisted description of an expedition. Runtime scenes, spawned actors, HUD, and camera
/// are projections of this snapshot and never become an additional source of truth.
/// </summary>
[Serializable]
public sealed class DungeonExpeditionSnapshot
{
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;
    public DungeonRunState state = DungeonRunState.Ready;
    public DungeonRoomResumePoint resumePoint = DungeonRoomResumePoint.None;
    public string dungeonId;
    public int depth = 1;
    public int selectedDepth = 1;
    public int highestUnlockedDepth = 1;
    public int contractOfferSeed;
    public string offeredContractIdA = DungeonContractModel.DefaultContractId;
    public string offeredContractIdB = "ravenous_pact";
    public string selectedContractId = DungeonContractModel.DefaultContractId;
    public string activeContractId = DungeonContractModel.DefaultContractId;
    public string lastContractSummary;
    public int encounterSeed;
    public string selectedEncounterId = DungeonEncounterModel.DefaultEncounterId;
    public string activeEncounterId = DungeonEncounterModel.DefaultEncounterId;
    public string lastEncounterSummary;
    public int totalRooms = 1;
    public int currentRoomIndex;
    public int roomsCompleted;
    public DungeonRunPlan runPlan;
    public float elapsedSeconds;
    public bool rewardPending;
    public string lastResult;

    public static DungeonExpeditionSnapshot FromLegacy(DungeonSaveData source)
    {
        if (source == null)
        {
            return new DungeonExpeditionSnapshot();
        }

        return new DungeonExpeditionSnapshot
        {
            version = CurrentVersion,
            state = source.state,
            resumePoint = GetExpectedResumePoint(source.state),
            dungeonId = source.dungeonId,
            depth = source.depth,
            selectedDepth = source.selectedDepth,
            highestUnlockedDepth = source.highestUnlockedDepth,
            contractOfferSeed = source.contractOfferSeed,
            offeredContractIdA = source.offeredContractIdA,
            offeredContractIdB = source.offeredContractIdB,
            selectedContractId = source.selectedContractId,
            activeContractId = source.activeContractId,
            lastContractSummary = source.lastContractSummary,
            encounterSeed = source.encounterSeed,
            selectedEncounterId = source.selectedEncounterId,
            activeEncounterId = source.activeEncounterId,
            lastEncounterSummary = source.lastEncounterSummary,
            totalRooms = source.totalRooms,
            currentRoomIndex = source.currentRoomIndex,
            roomsCompleted = source.roomsCompleted,
            runPlan = source.runPlan == null ? null : source.runPlan.Clone(),
            elapsedSeconds = source.elapsedSeconds,
            rewardPending = source.rewardPending,
            lastResult = source.lastResult
        };
    }

    public DungeonExpeditionSnapshot Clone()
    {
        return new DungeonExpeditionSnapshot
        {
            version = version,
            state = state,
            resumePoint = resumePoint,
            dungeonId = dungeonId,
            depth = depth,
            selectedDepth = selectedDepth,
            highestUnlockedDepth = highestUnlockedDepth,
            contractOfferSeed = contractOfferSeed,
            offeredContractIdA = offeredContractIdA,
            offeredContractIdB = offeredContractIdB,
            selectedContractId = selectedContractId,
            activeContractId = activeContractId,
            lastContractSummary = lastContractSummary,
            encounterSeed = encounterSeed,
            selectedEncounterId = selectedEncounterId,
            activeEncounterId = activeEncounterId,
            lastEncounterSummary = lastEncounterSummary,
            totalRooms = totalRooms,
            currentRoomIndex = currentRoomIndex,
            roomsCompleted = roomsCompleted,
            runPlan = runPlan == null ? null : runPlan.Clone(),
            elapsedSeconds = elapsedSeconds,
            rewardPending = rewardPending,
            lastResult = lastResult
        };
    }

    public DungeonSaveData ToSaveData()
    {
        DungeonSaveData saveData = new DungeonSaveData();
        CopyTo(saveData);
        saveData.expeditionSnapshot = Clone();
        return saveData;
    }

    public void CopyTo(DungeonSaveData target)
    {
        if (target == null)
        {
            return;
        }

        target.state = state;
        target.dungeonId = dungeonId;
        target.depth = depth;
        target.selectedDepth = selectedDepth;
        target.highestUnlockedDepth = highestUnlockedDepth;
        target.contractOfferSeed = contractOfferSeed;
        target.offeredContractIdA = offeredContractIdA;
        target.offeredContractIdB = offeredContractIdB;
        target.selectedContractId = selectedContractId;
        target.activeContractId = activeContractId;
        target.lastContractSummary = lastContractSummary;
        target.encounterSeed = encounterSeed;
        target.selectedEncounterId = selectedEncounterId;
        target.activeEncounterId = activeEncounterId;
        target.lastEncounterSummary = lastEncounterSummary;
        target.totalRooms = totalRooms;
        target.currentRoomIndex = currentRoomIndex;
        target.roomsCompleted = roomsCompleted;
        target.runPlan = runPlan == null ? null : runPlan.Clone();
        target.elapsedSeconds = elapsedSeconds;
        target.rewardPending = rewardPending;
        target.lastResult = lastResult;
    }

    public bool TryValidate(out string error)
    {
        if (version != CurrentVersion)
        {
            error = $"Dungeon expedition snapshot version {version} is unsupported.";
            return false;
        }

        if (!Enum.IsDefined(typeof(DungeonRunState), state))
        {
            error = "Dungeon expedition snapshot has an invalid run state.";
            return false;
        }

        if (state != DungeonRunState.Ready &&
            state != DungeonRunState.Running &&
            state != DungeonRunState.AwaitingExit)
        {
            error = $"Dungeon expedition snapshot state {state} is not a resumable checkpoint.";
            return false;
        }

        DungeonRoomResumePoint expectedResumePoint = GetExpectedResumePoint(state);
        if (resumePoint != expectedResumePoint)
        {
            error = $"Dungeon expedition snapshot resume point {resumePoint} conflicts with state {state}.";
            return false;
        }

        if (depth < 1 || selectedDepth < 1 || highestUnlockedDepth < depth || selectedDepth > highestUnlockedDepth)
        {
            error = "Dungeon expedition snapshot has an invalid depth range.";
            return false;
        }

        bool needsRunPlan = state == DungeonRunState.Running || state == DungeonRunState.AwaitingExit;
        if (!needsRunPlan)
        {
            if (runPlan != null || rewardPending)
            {
                error = "Ready expedition snapshot cannot retain a run plan or pending reward.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        if (runPlan == null)
        {
            error = "Active expedition snapshot requires a DungeonRunPlan.";
            return false;
        }

        if (!runPlan.TryValidate(depth, currentRoomIndex, rewardPending, out error))
        {
            return false;
        }

        if (state == DungeonRunState.AwaitingExit && !rewardPending)
        {
            error = "AwaitingExit snapshot requires a pending reward.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public bool MatchesLegacy(DungeonSaveData legacy)
    {
        if (legacy == null ||
            state != legacy.state ||
            !string.Equals(dungeonId, legacy.dungeonId, StringComparison.Ordinal) ||
            depth != legacy.depth ||
            selectedDepth != legacy.selectedDepth ||
            highestUnlockedDepth != legacy.highestUnlockedDepth ||
            contractOfferSeed != legacy.contractOfferSeed ||
            !string.Equals(offeredContractIdA, legacy.offeredContractIdA, StringComparison.Ordinal) ||
            !string.Equals(offeredContractIdB, legacy.offeredContractIdB, StringComparison.Ordinal) ||
            !string.Equals(selectedContractId, legacy.selectedContractId, StringComparison.Ordinal) ||
            !string.Equals(activeContractId, legacy.activeContractId, StringComparison.Ordinal) ||
            encounterSeed != legacy.encounterSeed ||
            !string.Equals(selectedEncounterId, legacy.selectedEncounterId, StringComparison.Ordinal) ||
            !string.Equals(activeEncounterId, legacy.activeEncounterId, StringComparison.Ordinal) ||
            totalRooms != legacy.totalRooms ||
            currentRoomIndex != legacy.currentRoomIndex ||
            roomsCompleted != legacy.roomsCompleted ||
            !string.Equals(lastContractSummary, legacy.lastContractSummary, StringComparison.Ordinal) ||
            !string.Equals(lastEncounterSummary, legacy.lastEncounterSummary, StringComparison.Ordinal) ||
            !string.Equals(lastResult, legacy.lastResult, StringComparison.Ordinal) ||
            !Mathf.Approximately(elapsedSeconds, legacy.elapsedSeconds) ||
            rewardPending != legacy.rewardPending)
        {
            return false;
        }

        if (runPlan == null || legacy.runPlan == null)
        {
            return runPlan == null && legacy.runPlan == null;
        }

        DungeonRunPlan legacyPlan = legacy.runPlan;
        return runPlan.version == legacyPlan.version &&
               runPlan.runSeed == legacyPlan.runSeed &&
               runPlan.currentDepth == legacyPlan.currentDepth &&
               runPlan.currentRoomIndex == legacyPlan.currentRoomIndex &&
               string.Equals(runPlan.currentRoomTemplateId, legacyPlan.currentRoomTemplateId, StringComparison.Ordinal) &&
               runPlan.hasAssignedRoomTemplate == legacyPlan.hasAssignedRoomTemplate &&
               runPlan.currentRoomSeed == legacyPlan.currentRoomSeed &&
               runPlan.propPlacementSeed == legacyPlan.propPlacementSeed &&
               runPlan.enemyPlacementSeed == legacyPlan.enemyPlacementSeed &&
               runPlan.portalPlacementSeed == legacyPlan.portalPlacementSeed &&
               runPlan.rewardPending == legacyPlan.rewardPending &&
               runPlan.pendingRewardDepth == legacyPlan.pendingRewardDepth;
    }

    public static DungeonRoomResumePoint GetExpectedResumePoint(DungeonRunState runState)
    {
        return runState switch
        {
            DungeonRunState.Running => DungeonRoomResumePoint.RestartCurrentRoom,
            DungeonRunState.AwaitingExit => DungeonRoomResumePoint.AwaitingExit,
            _ => DungeonRoomResumePoint.None
        };
    }
}

public enum DungeonRoomResumePoint
{
    None = 0,
    RestartCurrentRoom = 1,
    AwaitingExit = 2
}
