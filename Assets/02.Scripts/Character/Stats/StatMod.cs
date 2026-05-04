using System;
using UnityEngine;

[Serializable]
public class StatMod
{
    public enum StatModType
    {
        Flat,
        PercentAdd,
        PercentMult
    }

    [SerializeField] private StatId statId = StatId.AttackDamage;
    [SerializeField] private StatModType type = StatModType.Flat;
    [SerializeField] private float value;

    public StatId StatId => statId;
    public StatModType Type => type;
    public float Value => value;

    public StatMod()
    {
    }

    public StatMod(StatId statId, StatModType type, float value)
    {
        this.statId = statId;
        this.type = type;
        this.value = value;
    }

    public bool AppliesTo(StatId targetStat)
    {
        return statId == targetStat && !Mathf.Approximately(value, 0f);
    }
}
