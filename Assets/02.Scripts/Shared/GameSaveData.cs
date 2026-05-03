using System;

[Serializable]
public class GameSaveData
{
    public int version = 1;
    public string savedAtUtc;
    public float playTimeSeconds;
    public ResourceAmount[] currencies;
    public DefenseSaveData defense = new DefenseSaveData();
}

[Serializable]
public class DefenseSaveData
{
    public DefenseState state = DefenseState.Idle;
    public FrontlineMode mode = FrontlineMode.Push;
    public int frontlineLevel = 1;
    public int wallLevel = 1;
    public int towerLevel = 1;
    public int defenderLevel = 1;
    public float wallCurrentHealth = 100f;
    public float enemyPressure;
    public float frontlineProgress;
    public bool wallDamaged;
    public float totalElapsed;
    public float levelElapsed;
}
