using System;
using UnityEngine;

public static class DungeonContractModel
{
    public const string DefaultContractId = "steady_clear";
    public const string RewardDenominator = "per-clear guaranteed item reward";

    private static readonly DungeonContractProfile[] StarterContracts =
    {
        new DungeonContractProfile(
            "steady_clear",
            "Steady Clear",
            "No added risk; baseline threat and item power.",
            1f,
            1f,
            0),
        new DungeonContractProfile(
            "ravenous_pact",
            "Ravenous Pact",
            "Tougher enemies for a stronger item roll.",
            1.18f,
            1.08f,
            3),
        new DungeonContractProfile(
            "blood_price",
            "Blood Price",
            "Sharper enemy hits for a better item roll.",
            1.06f,
            1.24f,
            2)
    };

    public static int StarterContractCount => StarterContracts.Length;

    public static DungeonContractProfile GetDefault()
    {
        return StarterContracts[0];
    }

    public static DungeonContractProfile GetContractOrDefault(string contractId)
    {
        return TryGetContract(contractId, out DungeonContractProfile profile)
            ? profile
            : GetDefault();
    }

    public static bool TryGetContract(string contractId, out DungeonContractProfile profile)
    {
        if (!string.IsNullOrWhiteSpace(contractId))
        {
            for (int i = 0; i < StarterContracts.Length; i++)
            {
                DungeonContractProfile candidate = StarterContracts[i];
                if (string.Equals(candidate.Id, contractId, StringComparison.OrdinalIgnoreCase))
                {
                    profile = candidate;
                    return true;
                }
            }
        }

        profile = default;
        return false;
    }

    public static DungeonContractProfile GetStarterContract(int index)
    {
        if (StarterContracts.Length == 0)
        {
            return default;
        }

        int safeIndex = PositiveModulo(index, StarterContracts.Length);
        return StarterContracts[safeIndex];
    }

    public static void BuildOffer(int selectedDepth, int offerSeed, out string firstContractId, out string secondContractId)
    {
        int count = StarterContracts.Length;
        if (count == 0)
        {
            firstContractId = string.Empty;
            secondContractId = string.Empty;
            return;
        }

        int firstIndex = PositiveModulo(Mathf.Max(1, selectedDepth) - 1 + offerSeed, count);
        int offset = count <= 2 ? 1 : 1 + PositiveModulo(offerSeed, count - 1);
        int secondIndex = PositiveModulo(firstIndex + offset, count);
        if (secondIndex == firstIndex)
        {
            secondIndex = PositiveModulo(firstIndex + 1, count);
        }

        firstContractId = StarterContracts[firstIndex].Id;
        secondContractId = StarterContracts[secondIndex].Id;
    }

    public static string FormatOfferText(string firstContractId, string secondContractId)
    {
        DungeonContractProfile first = GetContractOrDefault(firstContractId);
        DungeonContractProfile second = GetContractOrDefault(secondContractId);
        return $"A: {FormatShortText(first)} / B: {FormatShortText(second)}";
    }

    public static string FormatShortText(DungeonContractProfile profile)
    {
        if (!profile.IsValid)
        {
            profile = GetDefault();
        }

        return $"{profile.DisplayName} (HP x{profile.EnemyHealthMultiplier:0.##}, DMG x{profile.EnemyDamageMultiplier:0.##}, reward D+{profile.RewardDepthOffset})";
    }

    public static string FormatDetailText(DungeonContractProfile profile)
    {
        if (!profile.IsValid)
        {
            profile = GetDefault();
        }

        return $"{FormatShortText(profile)} - {profile.Description}";
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

public struct DungeonContractProfile
{
    public string Id { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public float EnemyHealthMultiplier { get; }
    public float EnemyDamageMultiplier { get; }
    public int RewardDepthOffset { get; }
    public bool IsValid => !string.IsNullOrWhiteSpace(Id);

    public DungeonContractProfile(
        string id,
        string displayName,
        string description,
        float enemyHealthMultiplier,
        float enemyDamageMultiplier,
        int rewardDepthOffset)
    {
        Id = string.IsNullOrWhiteSpace(id) ? DungeonContractModel.DefaultContractId : id.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? Id : displayName.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? string.Empty : description.Trim();
        EnemyHealthMultiplier = Mathf.Max(0.1f, enemyHealthMultiplier);
        EnemyDamageMultiplier = Mathf.Max(0.1f, enemyDamageMultiplier);
        RewardDepthOffset = Mathf.Max(0, rewardDepthOffset);
    }
}
