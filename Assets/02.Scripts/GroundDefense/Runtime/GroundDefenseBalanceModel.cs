using System;
using UnityEngine;

public static class GroundDefenseBalanceModel
{
    public const int LevelsPerBand = 10;
    public const float IncomingPressureBandMultiplier = 1.65f;
    public const float IncomingPressureStepPerLevel = 0.07f;
    public const float DefenseOutputBandMultiplier = 1.45f;
    public const float DefenseOutputStepPerLevel = 0.025f;
    public const float PressureCapacityBandMultiplier = 1.4f;
    public const float PressureCapacityStepPerLevel = 0.04f;
    public const float ProgressRequirementBandMultiplier = 1.5f;
    public const float ProgressRequirementStepPerLevel = 0.05f;
    public const float RewardBandMultiplier = 1.5f;
    public const float RewardStepPerLevel = 0.05f;
    public const int BaseMilestoneGoldReward = 120;
    public const int BaseMilestoneScrapReward = 16;
    public const float MilestoneRewardBandMultiplier = 1.6f;
    public const float MaxScaleMultiplier = 1000000000f;

    public static GroundDefenseBalanceProfile Evaluate(int frontlineLevel)
    {
        int safeLevel = Mathf.Max(1, frontlineLevel);
        int zeroBasedLevel = safeLevel - 1;
        int bandIndex = zeroBasedLevel / LevelsPerBand;
        int levelInBand = zeroBasedLevel % LevelsPerBand;

        return new GroundDefenseBalanceProfile(
            safeLevel,
            bandIndex + 1,
            levelInBand,
            EvaluateScale(IncomingPressureBandMultiplier, IncomingPressureStepPerLevel, bandIndex, levelInBand),
            EvaluateScale(DefenseOutputBandMultiplier, DefenseOutputStepPerLevel, bandIndex, levelInBand),
            EvaluateScale(PressureCapacityBandMultiplier, PressureCapacityStepPerLevel, bandIndex, levelInBand),
            EvaluateScale(ProgressRequirementBandMultiplier, ProgressRequirementStepPerLevel, bandIndex, levelInBand),
            EvaluateScale(RewardBandMultiplier, RewardStepPerLevel, bandIndex, levelInBand));
    }

    public static ResourceAmount[] GetMilestoneRewards(GroundDefenseBalanceProfile profile)
    {
        if (profile.BandNumber <= 1)
        {
            return Array.Empty<ResourceAmount>();
        }

        int milestoneIndex = profile.BandNumber - 2;
        float rewardScale = EvaluateMilestoneRewardScale(milestoneIndex);
        return new[]
        {
            new ResourceAmount(ResourceId.Gold, ScaleWholeReward(BaseMilestoneGoldReward, rewardScale)),
            new ResourceAmount(ResourceId.Scrap, ScaleWholeReward(BaseMilestoneScrapReward, rewardScale))
        };
    }

    private static float EvaluateScale(float bandMultiplier, float stepPerLevel, int bandIndex, int levelInBand)
    {
        double bandScale = Math.Pow(Math.Max(1d, bandMultiplier), Math.Max(0, bandIndex));
        double withinBandScale = 1d + Math.Max(0d, stepPerLevel) * Math.Max(0, levelInBand);
        double scale = bandScale * withinBandScale;

        if (double.IsNaN(scale) || double.IsInfinity(scale))
        {
            return MaxScaleMultiplier;
        }

        return Mathf.Clamp((float)scale, 1f, MaxScaleMultiplier);
    }

    private static float EvaluateMilestoneRewardScale(int milestoneIndex)
    {
        double scale = Math.Pow(Math.Max(1d, MilestoneRewardBandMultiplier), Math.Max(0, milestoneIndex));
        if (double.IsNaN(scale) || double.IsInfinity(scale))
        {
            return MaxScaleMultiplier;
        }

        return Mathf.Clamp((float)scale, 1f, MaxScaleMultiplier);
    }

    private static int ScaleWholeReward(int baseAmount, float multiplier)
    {
        double scaled = Math.Ceiling(Math.Max(0, baseAmount) * Math.Max(1f, multiplier));
        return scaled >= int.MaxValue ? int.MaxValue : Math.Max(0, (int)scaled);
    }
}

public readonly struct GroundDefenseBalanceProfile
{
    public int FrontlineLevel { get; }
    public int BandNumber { get; }
    public int LevelInBand { get; }
    public int BandStartLevel => (BandNumber - 1) * GroundDefenseBalanceModel.LevelsPerBand + 1;
    public int NextBandLevel => BandNumber * GroundDefenseBalanceModel.LevelsPerBand + 1;
    public float IncomingPressureMultiplier { get; }
    public float DefenseOutputMultiplier { get; }
    public float PressureCapacityMultiplier { get; }
    public float ProgressRequirementMultiplier { get; }
    public float RewardMultiplier { get; }

    public GroundDefenseBalanceProfile(
        int frontlineLevel,
        int bandNumber,
        int levelInBand,
        float incomingPressureMultiplier,
        float defenseOutputMultiplier,
        float pressureCapacityMultiplier,
        float progressRequirementMultiplier,
        float rewardMultiplier)
    {
        FrontlineLevel = Mathf.Max(1, frontlineLevel);
        BandNumber = Mathf.Max(1, bandNumber);
        LevelInBand = Mathf.Clamp(levelInBand, 0, GroundDefenseBalanceModel.LevelsPerBand - 1);
        IncomingPressureMultiplier = Mathf.Max(1f, incomingPressureMultiplier);
        DefenseOutputMultiplier = Mathf.Max(1f, defenseOutputMultiplier);
        PressureCapacityMultiplier = Mathf.Max(1f, pressureCapacityMultiplier);
        ProgressRequirementMultiplier = Mathf.Max(1f, progressRequirementMultiplier);
        RewardMultiplier = Mathf.Max(1f, rewardMultiplier);
    }
}
