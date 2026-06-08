using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    private const float PercentScale = 0.01f;

    [Header("Core")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float moveSpeed = 4f;

    [Header("Combat")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackCooldown = 1f;

    private readonly List<StatMod> modifierBuffer = new List<StatMod>(8);
    private EquipmentSlots equipmentSlots;
    private bool subscribedToEquipment;
    private float runtimeMaxHealthMultiplier = 1f;
    private float runtimeAttackDamageMultiplier = 1f;

    public event Action Changed;

    private void Awake()
    {
        ResolveEquipmentSlots();
    }

    private void OnEnable()
    {
        SubscribeToEquipment();
    }

    private void OnDisable()
    {
        UnsubscribeFromEquipment();
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        attackDamage = Mathf.Max(0f, attackDamage);
        attackRange = Mathf.Max(0f, attackRange);
        attackCooldown = Mathf.Max(0.05f, attackCooldown);
    }

    public float GetValue(StatId statId)
    {
        if (statId == StatId.AttackCooldown)
        {
            return GetAttackCooldown();
        }

        return ClampStat(statId, ApplyModifiers(statId, GetBaseValue(statId)));
    }

    public void SetRuntimeCombatMultipliers(float maxHealthMultiplier, float attackDamageMultiplier)
    {
        float safeMaxHealthMultiplier = Mathf.Max(1f, maxHealthMultiplier);
        float safeAttackDamageMultiplier = Mathf.Max(1f, attackDamageMultiplier);
        if (Mathf.Approximately(runtimeMaxHealthMultiplier, safeMaxHealthMultiplier) &&
            Mathf.Approximately(runtimeAttackDamageMultiplier, safeAttackDamageMultiplier))
        {
            return;
        }

        runtimeMaxHealthMultiplier = safeMaxHealthMultiplier;
        runtimeAttackDamageMultiplier = safeAttackDamageMultiplier;
        Changed?.Invoke();
    }

    private float GetBaseValue(StatId statId)
    {
        switch (statId)
        {
            case StatId.MaxHealth:
                return maxHealth * runtimeMaxHealthMultiplier;
            case StatId.AttackDamage:
                return attackDamage * runtimeAttackDamageMultiplier;
            case StatId.AttackRange:
                return attackRange;
            case StatId.AttackCooldown:
                return attackCooldown;
            case StatId.AttackSpeed:
                return 1f / Mathf.Max(0.05f, attackCooldown);
            case StatId.MoveSpeed:
                return moveSpeed;
            case StatId.DefenseWallHpBonus:
            case StatId.TowerDamageBonus:
            case StatId.DefenderDamageBonus:
                return 0f;
            default:
                Debug.LogWarning($"Unhandled stat id: {statId}", this);
                return 0f;
        }
    }

    private float GetAttackCooldown()
    {
        float attackSpeed = ApplyModifiers(StatId.AttackSpeed, GetBaseValue(StatId.AttackSpeed));
        float cooldown = 1f / Mathf.Max(0.05f, attackSpeed);
        return ClampStat(StatId.AttackCooldown, ApplyModifiers(StatId.AttackCooldown, cooldown));
    }

    private float ApplyModifiers(StatId statId, float baseValue)
    {
        ResolveEquipmentSlots();

        if (equipmentSlots == null)
        {
            return baseValue;
        }

        modifierBuffer.Clear();
        equipmentSlots.AppendModifiers(statId, modifierBuffer);

        float flatBonus = 0f;
        float percentAdd = 0f;
        float percentMultiplier = 1f;

        for (int i = 0; i < modifierBuffer.Count; i++)
        {
            StatMod modifier = modifierBuffer[i];
            switch (modifier.Type)
            {
                case StatMod.StatModType.Flat:
                    flatBonus += modifier.Value;
                    break;
                case StatMod.StatModType.PercentAdd:
                    percentAdd += modifier.Value * PercentScale;
                    break;
                case StatMod.StatModType.PercentMult:
                    percentMultiplier *= Mathf.Max(0f, 1f + modifier.Value * PercentScale);
                    break;
            }
        }

        return (baseValue + flatBonus) * Mathf.Max(0f, 1f + percentAdd) * percentMultiplier;
    }

    private float ClampStat(StatId statId, float value)
    {
        switch (statId)
        {
            case StatId.MaxHealth:
                return Mathf.Max(1f, value);
            case StatId.AttackCooldown:
                return Mathf.Max(0.05f, value);
            case StatId.AttackDamage:
            case StatId.AttackRange:
            case StatId.AttackSpeed:
            case StatId.MoveSpeed:
            case StatId.DefenseWallHpBonus:
            case StatId.TowerDamageBonus:
            case StatId.DefenderDamageBonus:
                return Mathf.Max(0f, value);
            default:
                return value;
        }
    }

    private void ResolveEquipmentSlots()
    {
        if (equipmentSlots == null)
        {
            equipmentSlots = GetComponent<EquipmentSlots>();
        }
    }

    private void SubscribeToEquipment()
    {
        ResolveEquipmentSlots();

        if (equipmentSlots == null || subscribedToEquipment)
        {
            return;
        }

        equipmentSlots.Changed += HandleEquipmentChanged;
        subscribedToEquipment = true;
    }

    private void UnsubscribeFromEquipment()
    {
        if (equipmentSlots == null || !subscribedToEquipment)
        {
            return;
        }

        equipmentSlots.Changed -= HandleEquipmentChanged;
        subscribedToEquipment = false;
    }

    private void HandleEquipmentChanged()
    {
        Changed?.Invoke();
    }
}
