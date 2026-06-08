using System;
using UnityEngine;

public static class DungeonDepthBalanceModel
{
    public const int DepthsPerBand = 10;
    public const float EnemyHealthBandMultiplier = 1.8f;
    public const float EnemyHealthStepPerDepth = 0.08f;
    public const float EnemyDamageBandMultiplier = 1.5f;
    public const float EnemyDamageStepPerDepth = 0.05f;
    public const float RewardPowerBandMultiplier = 1.55f;
    public const float RewardPowerStepPerDepth = 0.055f;
    public const float MaterialYieldBandMultiplier = 1.3f;
    public const float MaterialYieldStepPerDepth = 0.03f;
    public const float MaxScaleMultiplier = 1000000000f;

    public static DungeonDepthBalanceProfile Evaluate(int depth)
    {
        int safeDepth = Mathf.Max(1, depth);
        int zeroBasedDepth = safeDepth - 1;
        int bandIndex = zeroBasedDepth / DepthsPerBand;
        int depthInBand = zeroBasedDepth % DepthsPerBand;

        return new DungeonDepthBalanceProfile(
            safeDepth,
            bandIndex + 1,
            depthInBand,
            EvaluateScale(EnemyHealthBandMultiplier, EnemyHealthStepPerDepth, bandIndex, depthInBand),
            EvaluateScale(EnemyDamageBandMultiplier, EnemyDamageStepPerDepth, bandIndex, depthInBand),
            EvaluateScale(RewardPowerBandMultiplier, RewardPowerStepPerDepth, bandIndex, depthInBand),
            EvaluateScale(MaterialYieldBandMultiplier, MaterialYieldStepPerDepth, bandIndex, depthInBand));
    }

    private static float EvaluateScale(float bandMultiplier, float stepPerDepth, int bandIndex, int depthInBand)
    {
        double bandScale = Math.Pow(Math.Max(1d, bandMultiplier), Math.Max(0, bandIndex));
        double withinBandScale = 1d + Math.Max(0d, stepPerDepth) * Math.Max(0, depthInBand);
        double scale = bandScale * withinBandScale;

        if (double.IsNaN(scale) || double.IsInfinity(scale))
        {
            return MaxScaleMultiplier;
        }

        return Mathf.Clamp((float)scale, 1f, MaxScaleMultiplier);
    }
}

public struct DungeonDepthBalanceProfile
{
    public int Depth { get; }
    public int BandNumber { get; }
    public int DepthInBand { get; }
    public float EnemyHealthMultiplier { get; }
    public float EnemyDamageMultiplier { get; }
    public float RewardPowerMultiplier { get; }
    public float MaterialYieldMultiplier { get; }

    public DungeonDepthBalanceProfile(
        int depth,
        int bandNumber,
        int depthInBand,
        float enemyHealthMultiplier,
        float enemyDamageMultiplier,
        float rewardPowerMultiplier,
        float materialYieldMultiplier)
    {
        Depth = Mathf.Max(1, depth);
        BandNumber = Mathf.Max(1, bandNumber);
        DepthInBand = Mathf.Clamp(depthInBand, 0, DungeonDepthBalanceModel.DepthsPerBand - 1);
        EnemyHealthMultiplier = Mathf.Max(1f, enemyHealthMultiplier);
        EnemyDamageMultiplier = Mathf.Max(1f, enemyDamageMultiplier);
        RewardPowerMultiplier = Mathf.Max(1f, rewardPowerMultiplier);
        MaterialYieldMultiplier = Mathf.Max(1f, materialYieldMultiplier);
    }
}
