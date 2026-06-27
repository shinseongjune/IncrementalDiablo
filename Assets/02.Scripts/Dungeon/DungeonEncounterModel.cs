using System;
using UnityEngine;

public static class DungeonEncounterModel
{
    public const string DefaultEncounterId = "crypt_skirmish";
    public const string EncounterDenominator = "per dungeon run spawned encounter";

    private static readonly DungeonEncounterProfile[] StarterEncounters =
    {
        new DungeonEncounterProfile(
            "crypt_skirmish",
            "Crypt Skirmish",
            "A standard room using the current depth and contract threat.",
            false,
            false,
            1f,
            1f,
            0),
        new DungeonEncounterProfile(
            "elite_guard",
            "Elite Guard",
            "A reinforced elite pushes the same room with higher durability and damage.",
            true,
            false,
            1.28f,
            1.12f,
            1),
        new DungeonEncounterProfile(
            "tomb_warden",
            "Tomb Warden",
            "A boss-style encounter with a larger threat spike and stronger item roll.",
            false,
            true,
            1.65f,
            1.22f,
            2)
    };

    public static int StarterEncounterCount => StarterEncounters.Length;

    public static DungeonEncounterProfile GetDefault()
    {
        return StarterEncounters[0];
    }

    public static DungeonEncounterProfile GetEncounterOrDefault(string encounterId)
    {
        return TryGetEncounter(encounterId, out DungeonEncounterProfile profile)
            ? profile
            : GetDefault();
    }

    public static bool TryGetEncounter(string encounterId, out DungeonEncounterProfile profile)
    {
        if (!string.IsNullOrWhiteSpace(encounterId))
        {
            for (int i = 0; i < StarterEncounters.Length; i++)
            {
                DungeonEncounterProfile candidate = StarterEncounters[i];
                if (string.Equals(candidate.Id, encounterId, StringComparison.OrdinalIgnoreCase))
                {
                    profile = candidate;
                    return true;
                }
            }
        }

        profile = default;
        return false;
    }

    public static DungeonEncounterProfile GetStarterEncounter(int index)
    {
        if (StarterEncounters.Length == 0)
        {
            return default;
        }

        int safeIndex = PositiveModulo(index, StarterEncounters.Length);
        return StarterEncounters[safeIndex];
    }

    public static DungeonEncounterProfile BuildEncounter(int selectedDepth, int encounterSeed, string selectedContractId)
    {
        int safeDepth = Mathf.Max(1, selectedDepth);
        int safeSeed = Mathf.Max(0, encounterSeed);

        if (safeDepth >= 5 && safeDepth % 5 == 0)
        {
            return GetEncounterOrDefault("tomb_warden");
        }

        int contractBias = ResolveContractBias(selectedContractId);
        int roll = PositiveModulo(safeSeed + contractBias, 4);
        if (roll == 1)
        {
            return GetEncounterOrDefault("elite_guard");
        }

        if (safeDepth >= 3 && roll == 3)
        {
            return GetEncounterOrDefault("tomb_warden");
        }

        return GetDefault();
    }

    public static string FormatShortText(DungeonEncounterProfile profile)
    {
        if (!profile.IsValid)
        {
            profile = GetDefault();
        }

        string kind = profile.IsBoss ? "Boss" : profile.IsElite ? "Elite" : "Normal";
        return $"{profile.DisplayName} ({kind}, HP x{profile.EnemyHealthMultiplier:0.##}, DMG x{profile.EnemyDamageMultiplier:0.##}, reward D+{profile.RewardDepthOffset})";
    }

    public static string FormatDetailText(DungeonEncounterProfile profile)
    {
        if (!profile.IsValid)
        {
            profile = GetDefault();
        }

        return $"{FormatShortText(profile)} - {profile.Description}";
    }

    private static int ResolveContractBias(string selectedContractId)
    {
        if (string.Equals(selectedContractId, "blood_price", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return 0;
    }

    private static int PositiveModulo(int value, int divisor)
    {
        if (divisor <= 0)
        {
            return 0;
        }

        int result = value % divisor;
        return result < 0 ? result + divisor : result;
    }
}

public struct DungeonEncounterProfile
{
    public string Id { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public bool IsElite { get; }
    public bool IsBoss { get; }
    public float EnemyHealthMultiplier { get; }
    public float EnemyDamageMultiplier { get; }
    public int RewardDepthOffset { get; }
    public bool IsValid => !string.IsNullOrWhiteSpace(Id);

    public DungeonEncounterProfile(
        string id,
        string displayName,
        string description,
        bool isElite,
        bool isBoss,
        float enemyHealthMultiplier,
        float enemyDamageMultiplier,
        int rewardDepthOffset)
    {
        Id = string.IsNullOrWhiteSpace(id) ? DungeonEncounterModel.DefaultEncounterId : id.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? Id : displayName.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? string.Empty : description.Trim();
        IsElite = isElite;
        IsBoss = isBoss;
        EnemyHealthMultiplier = Mathf.Max(0.1f, enemyHealthMultiplier);
        EnemyDamageMultiplier = Mathf.Max(0.1f, enemyDamageMultiplier);
        RewardDepthOffset = Mathf.Max(0, rewardDepthOffset);
    }
}
