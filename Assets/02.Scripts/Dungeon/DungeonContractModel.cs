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

    public static string FormatGoalText(DungeonContractProfile profile)
    {
        if (!profile.IsValid)
        {
            profile = GetDefault();
        }

        if (profile.RewardDepthOffset <= 0 && GetThreatScore(profile) <= 1.001f)
        {
            return "Goal: safest clear and recovery baseline.";
        }

        string rewardText = FormatRewardGoal(profile);
        if (profile.EnemyDamageMultiplier > profile.EnemyHealthMultiplier + 0.05f)
        {
            return $"Goal: {rewardText}; accept sharper enemy hits.";
        }

        if (profile.EnemyHealthMultiplier > profile.EnemyDamageMultiplier + 0.05f)
        {
            return $"Goal: {rewardText}; accept longer enemy fights.";
        }

        return $"Goal: {rewardText}; accept higher listed threat.";
    }

    public static string FormatGoalComparisonText(DungeonContractProfile selected, DungeonContractProfile alternative)
    {
        if (!selected.IsValid)
        {
            selected = GetDefault();
        }

        if (!alternative.IsValid ||
            string.Equals(selected.Id, alternative.Id, StringComparison.OrdinalIgnoreCase))
        {
            return FormatGoalText(selected);
        }

        float selectedThreat = GetThreatScore(selected);
        float alternativeThreat = GetThreatScore(alternative);
        int rewardDelta = selected.RewardDepthOffset - alternative.RewardDepthOffset;
        string alternativeName = alternative.DisplayName;

        if (selected.RewardDepthOffset <= 0 && selectedThreat <= alternativeThreat + 0.001f)
        {
            return alternative.RewardDepthOffset > selected.RewardDepthOffset
                ? $"Goal: safest clear; switch to {alternativeName} for {FormatRewardGoal(alternative)}."
                : "Goal: safest clear and recovery baseline.";
        }

        if (rewardDelta > 0)
        {
            return selectedThreat > alternativeThreat + 0.001f
                ? $"Goal: {FormatRewardGoal(selected)}; accept higher threat than {alternativeName}."
                : $"Goal: {FormatRewardGoal(selected)} with no extra shown threat versus {alternativeName}.";
        }

        if (rewardDelta == 0)
        {
            if (selectedThreat < alternativeThreat - 0.001f)
            {
                return $"Goal: same reward as {alternativeName} with lower threat.";
            }

            if (selectedThreat > alternativeThreat + 0.001f)
            {
                return $"Goal: same reward as {alternativeName}, but higher threat.";
            }
        }

        if (selectedThreat < alternativeThreat - 0.001f)
        {
            return $"Goal: safer clear than {alternativeName}; lower reward depth.";
        }

        return $"Goal: weaker tradeoff than {alternativeName}; switch or refresh unless practicing danger.";
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

    private static float GetThreatScore(DungeonContractProfile profile)
    {
        if (!profile.IsValid)
        {
            profile = GetDefault();
        }

        return Mathf.Max(profile.EnemyHealthMultiplier, profile.EnemyDamageMultiplier);
    }

    private static string FormatRewardGoal(DungeonContractProfile profile)
    {
        if (!profile.IsValid)
        {
            profile = GetDefault();
        }

        return profile.RewardDepthOffset <= 0
            ? "baseline reward"
            : $"reward D+{profile.RewardDepthOffset}";
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
