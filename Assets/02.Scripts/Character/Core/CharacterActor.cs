using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CharacterStats))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(CharacterMotor))]
[RequireComponent(typeof(CombatDriver))]
[RequireComponent(typeof(EquipmentSlots))]
[RequireComponent(typeof(NavMeshAgent))]
public class CharacterActor : MonoBehaviour
{
    [SerializeField] private CharacterTeam team = CharacterTeam.Neutral;

    public CharacterTeam Team => team;
    public CharacterStats Stats { get; private set; }
    public Health Health { get; private set; }
    public CharacterMotor Motor { get; private set; }
    public CombatDriver Combat { get; private set; }
    public EquipmentSlots Equipment { get; private set; }

    private void Awake()
    {
        Stats = GetComponent<CharacterStats>();
        Health = GetComponent<Health>();
        Motor = GetComponent<CharacterMotor>();
        Combat = GetComponent<CombatDriver>();
        Equipment = GetComponent<EquipmentSlots>();
    }
}

public enum CharacterTeam
{
    Neutral,
    Player,
    Enemy
}
