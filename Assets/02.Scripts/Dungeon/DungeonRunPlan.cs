using System;

[Serializable]
public class DungeonRunPlan
{
    public const int CurrentPlanVersion = 2;
    public const string TransitionalTemplateId = "prototype_crypt";

    public int version = CurrentPlanVersion;
    public int runSeed;
    public int currentDepth = 1;
    public int currentRoomIndex;
    public string currentRoomTemplateId = TransitionalTemplateId;
    public bool hasAssignedRoomTemplate;
    public int currentRoomSeed;
    public int propPlacementSeed;
    public int enemyPlacementSeed;
    public int portalPlacementSeed;
    public bool rewardPending;
    public int pendingRewardDepth;

    public static DungeonRunPlan CreateNew(string dungeonId, int runSeed, int depth)
    {
        DungeonRunPlan plan = new DungeonRunPlan
        {
            version = CurrentPlanVersion,
            runSeed = NormalizeSeed(runSeed)
        };
        plan.SetCurrentRoom(dungeonId, depth, 0);
        plan.SetRewardPending(false, 0);
        return plan;
    }

    public static DungeonRunPlan CreateMigrated(
        string dungeonId,
        int depth,
        int roomIndex,
        int contractOfferSeed,
        int encounterSeed)
    {
        int legacySeed = Mix(StableHash(dungeonId), contractOfferSeed);
        legacySeed = Mix(legacySeed, encounterSeed);
        legacySeed = Mix(legacySeed, depth);
        legacySeed = Mix(legacySeed, roomIndex);
        DungeonRunPlan plan = CreateNew(dungeonId, legacySeed, depth);
        plan.SetCurrentRoom(dungeonId, depth, roomIndex);
        return plan;
    }

    public static int CreateRuntimeSeed()
    {
        long tickValue = DateTime.UtcNow.Ticks ^ ((long)Environment.TickCount * 486187739L);
        return NormalizeSeed(unchecked((int)(tickValue ^ (tickValue >> 32))));
    }

    public DungeonRunPlan Clone()
    {
        return new DungeonRunPlan
        {
            version = version,
            runSeed = runSeed,
            currentDepth = currentDepth,
            currentRoomIndex = currentRoomIndex,
            currentRoomTemplateId = currentRoomTemplateId,
            hasAssignedRoomTemplate = hasAssignedRoomTemplate,
            currentRoomSeed = currentRoomSeed,
            propPlacementSeed = propPlacementSeed,
            enemyPlacementSeed = enemyPlacementSeed,
            portalPlacementSeed = portalPlacementSeed,
            rewardPending = rewardPending,
            pendingRewardDepth = pendingRewardDepth
        };
    }

    public void Normalize(string dungeonId, int depth, int roomIndex)
    {
        version = CurrentPlanVersion;
        runSeed = NormalizeSeed(runSeed);
        SetCurrentRoom(dungeonId, depth, roomIndex);
        SetRewardPending(rewardPending, pendingRewardDepth);
    }

    public void SetCurrentRoom(string dungeonId, int depth, int roomIndex)
    {
        int normalizedDepth = Math.Max(1, depth);
        int normalizedRoomIndex = Math.Max(0, roomIndex);
        bool roomChanged = currentDepth != normalizedDepth || currentRoomIndex != normalizedRoomIndex;
        currentDepth = normalizedDepth;
        currentRoomIndex = normalizedRoomIndex;

        if (roomChanged || !hasAssignedRoomTemplate || string.IsNullOrWhiteSpace(currentRoomTemplateId))
        {
            currentRoomTemplateId = NormalizeTemplateId(null, dungeonId);
            hasAssignedRoomTemplate = false;
        }

        currentRoomSeed = DeriveSeed(runSeed, currentDepth, currentRoomIndex, 101);
        propPlacementSeed = DeriveSeed(runSeed, currentDepth, currentRoomIndex, 211);
        enemyPlacementSeed = DeriveSeed(runSeed, currentDepth, currentRoomIndex, 307);
        portalPlacementSeed = DeriveSeed(runSeed, currentDepth, currentRoomIndex, 401);
    }

    public bool AssignCurrentRoomTemplate(string templateId)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            return false;
        }

        string normalizedTemplateId = templateId.Trim();
        bool changed = !hasAssignedRoomTemplate ||
                       !string.Equals(currentRoomTemplateId, normalizedTemplateId, StringComparison.Ordinal);
        currentRoomTemplateId = normalizedTemplateId;
        hasAssignedRoomTemplate = true;
        return changed;
    }

    public void SetRewardPending(bool isPending, int rewardDepth)
    {
        rewardPending = isPending;
        pendingRewardDepth = isPending ? Math.Max(1, rewardDepth) : 0;
    }

    public bool TryValidate(
        int expectedDepth,
        int expectedRoomIndex,
        bool expectedRewardPending,
        out string error)
    {
        if (version != CurrentPlanVersion)
        {
            error = "dungeon run plan version is invalid";
            return false;
        }

        if (runSeed < 0 || currentRoomSeed < 0 || propPlacementSeed < 0 ||
            enemyPlacementSeed < 0 || portalPlacementSeed < 0)
        {
            error = "dungeon run plan seeds cannot be negative";
            return false;
        }

        if (currentDepth < 1 || currentRoomIndex < 0)
        {
            error = "dungeon run plan depth and room index must be non-negative";
            return false;
        }

        if (string.IsNullOrWhiteSpace(currentRoomTemplateId))
        {
            error = "dungeon run plan has no current room template id";
            return false;
        }

        if (!hasAssignedRoomTemplate &&
            !string.Equals(currentRoomTemplateId, TransitionalTemplateId, StringComparison.Ordinal))
        {
            error = "dungeon run plan has an unassigned room template id";
            return false;
        }

        if (currentDepth != Math.Max(1, expectedDepth) ||
            currentRoomIndex != Math.Max(0, expectedRoomIndex))
        {
            error = "dungeon run plan does not match the active dungeon depth and room";
            return false;
        }

        if (rewardPending != expectedRewardPending)
        {
            error = "dungeon run plan reward state does not match dungeon reward state";
            return false;
        }

        if (rewardPending && pendingRewardDepth < 1)
        {
            error = "dungeon run plan pending reward depth must be at least one";
            return false;
        }

        if (!rewardPending && pendingRewardDepth != 0)
        {
            error = "dungeon run plan has a reward depth without a pending reward";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static string NormalizeTemplateId(string templateId, string dungeonId)
    {
        if (!string.IsNullOrWhiteSpace(templateId))
        {
            return templateId.Trim();
        }

        return string.IsNullOrWhiteSpace(dungeonId)
            ? TransitionalTemplateId
            : dungeonId.Trim();
    }

    private static int DeriveSeed(int runSeed, int depth, int roomIndex, int salt)
    {
        int result = Mix(runSeed, depth);
        result = Mix(result, roomIndex);
        return Mix(result, salt);
    }

    private static int StableHash(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        int hash = 17;
        string normalized = value.Trim();
        for (int i = 0; i < normalized.Length; i++)
        {
            hash = Mix(hash, normalized[i]);
        }

        return hash;
    }

    private static int Mix(int seed, int value)
    {
        unchecked
        {
            uint mixed = (uint)seed;
            mixed ^= (uint)value + 0x9e3779b9u + (mixed << 6) + (mixed >> 2);
            mixed *= 16777619u;
            return (int)(mixed & 0x7fffffffu);
        }
    }

    private static int NormalizeSeed(int seed)
    {
        return seed == int.MinValue ? int.MaxValue : Math.Abs(seed);
    }
}
