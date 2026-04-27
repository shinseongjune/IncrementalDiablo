public class StatMod
{
    public enum StatModType
    {
        Flat,
        PercentAdd,
        PercentMult
    }

    public StatModType Type;
    public float Value;
}
