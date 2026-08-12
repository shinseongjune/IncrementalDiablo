using System;

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
    public int encounterSeed;
    public string selectedEncounterId = DungeonEncounterModel.DefaultEncounterId;
    public string activeEncounterId = DungeonEncounterModel.DefaultEncounterId;
    public int totalRooms = 1;
    public int currentRoomIndex;
    public int roomsCompleted;
    public DungeonRunPlan runPlan;
    public float elapsedSeconds;
    public bool rewardPending;

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
            encounterSeed = encounterSeed,
            selectedEncounterId = selectedEncounterId,
            activeEncounterId = activeEncounterId,
            totalRooms = totalRooms,
            currentRoomIndex = currentRoomIndex,
            roomsCompleted = roomsCompleted,
            runPlan = runPlan == null ? null : runPlan.Clone(),
            elapsedSeconds = elapsedSeconds,
            rewardPending = rewardPending,
        };
    }

    /// <summary>
    /// Produces the canonical payload for a closed expedition without touching the runtime owner.
    /// Old scene serialization can retain a previously allocated run-plan object even after the
    /// state returns to Ready; that transient plan must never make a Ready checkpoint unwritable.
    /// </summary>
    public void NormalizeReadyStateForCheckpoint()
    {
        if (state != DungeonRunState.Ready)
        {
            return;
        }

        resumePoint = DungeonRoomResumePoint.None;
        runPlan = null;
        rewardPending = false;
        activeContractId = string.Empty;
        activeEncounterId = string.Empty;
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
