using System;
using System.Collections.Generic;
using UnityEngine;

public enum WorldActorAction
{
    Idle,
    Moving,
    ChasingTarget,
    WindingUp,
    Attacking,
    ReturningHome,
    Defeated
}

[Serializable]
public sealed class AccountSnapshot
{
    public float playTimeSeconds;
    public ResourceAmount[] currencies = Array.Empty<ResourceAmount>();
    public DefenseSaveData defense = new DefenseSaveData();
    public HeroSaveData hero = new HeroSaveData();
    public InventorySaveData inventory = new InventorySaveData();
    public UiSettingsSaveData uiSettings = new UiSettingsSaveData();
    // Expedition progression and the authoritative lifecycle live with the account; the optional
    // dungeon world only exists while that lifecycle has an open additive room to project.
    public DungeonExpeditionSnapshot expedition = new DungeonExpeditionSnapshot();
}

[Serializable]
public sealed class DefenseWorldSnapshot
{
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;
    public DefenseBuildingWorldSnapshot[] buildings = Array.Empty<DefenseBuildingWorldSnapshot>();
    public DefenseUnitWorldSnapshot[] units = Array.Empty<DefenseUnitWorldSnapshot>();
}

[Serializable]
public sealed class DefenseBuildingWorldSnapshot
{
    public string entityId;
    public string archetypeId;
    public Vector3 position;
    public Quaternion rotation = Quaternion.identity;
    public float currentHealth;
    public float maxHealth;
    public bool active = true;
}

[Serializable]
public sealed class DefenseUnitWorldSnapshot
{
    public string entityId;
    public GroundDefenseNavMeshUnitSide side;
    public Vector3 position;
    public Quaternion rotation = Quaternion.identity;
    public Vector3 homePosition;
    public float currentHealth;
    public float maxHealth;
    public WorldActorAction action;
    public string targetEntityId;
}

[Serializable]
public sealed class DungeonWorldSnapshot
{
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;
    public bool isOpen;
    public string templateId;
    public int roomSeed;
    public int roomIndex;
    public DungeonCombatWorldSnapshot combat = new DungeonCombatWorldSnapshot();
    public DungeonActorWorldSnapshot hero = new DungeonActorWorldSnapshot();
    public DungeonActorWorldSnapshot[] actors = Array.Empty<DungeonActorWorldSnapshot>();
}

[Serializable]
public sealed class DungeonCombatWorldSnapshot
{
    public CombatRoomState state = CombatRoomState.Idle;
    public float countdownRemaining;
    public float elapsedSeconds;
    public float currentHeroHealth;
    public float currentEnemyHealth;
}

[Serializable]
public sealed class DungeonActorWorldSnapshot
{
    public string entityId;
    public string archetypeId;
    public CharacterTeam team;
    public Vector3 position;
    public Quaternion rotation = Quaternion.identity;
    public float currentHealth;
    public float maxHealth;
    public WorldActorAction action;
    public float actionRemainingSeconds;
    public string targetEntityId;
    public bool active = true;
}

public static class WorldSaveSnapshotValidator
{
    public static bool TryValidate(DefenseWorldSnapshot snapshot, out string error)
    {
        if (snapshot == null || snapshot.version != DefenseWorldSnapshot.CurrentVersion)
        {
            error = "Defense world snapshot version is unsupported.";
            return false;
        }

        HashSet<string> entities = new HashSet<string>(StringComparer.Ordinal);
        if (!TryValidateBuildings(snapshot.buildings, entities, out error) ||
            !TryValidateDefenseUnits(snapshot.units, entities, out error))
        {
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool TryValidate(DungeonWorldSnapshot snapshot, DungeonExpeditionSnapshot expedition, out string error)
    {
        if (snapshot == null)
        {
            error = "Dungeon world snapshot is missing.";
            return false;
        }

        if (snapshot.version != DungeonWorldSnapshot.CurrentVersion)
        {
            error = "Dungeon world snapshot version is unsupported.";
            return false;
        }

        if (!snapshot.isOpen)
        {
            if (expedition != null && expedition.state != DungeonRunState.Ready)
            {
                error = "Closed dungeon world conflicts with an active expedition.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        if (expedition == null || expedition.state == DungeonRunState.Ready || expedition.runPlan == null)
        {
            error = "Open dungeon world requires an active expedition plan.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(snapshot.templateId) ||
            !string.Equals(snapshot.templateId, expedition.runPlan.currentRoomTemplateId, StringComparison.Ordinal) ||
            snapshot.roomSeed != expedition.runPlan.currentRoomSeed ||
            snapshot.roomIndex != expedition.currentRoomIndex)
        {
            error = "Dungeon world does not match the active expedition room plan.";
            return false;
        }

        if (!Enum.IsDefined(typeof(CombatRoomState), snapshot.combat?.state) ||
            snapshot.combat == null || snapshot.combat.countdownRemaining < 0f || snapshot.combat.elapsedSeconds < 0f ||
            snapshot.combat.currentHeroHealth < 0f || snapshot.combat.currentEnemyHealth < 0f)
        {
            error = "Dungeon combat world state is invalid.";
            return false;
        }

        // A checkpoint is only legal at a settled room boundary. Keeping this validation in the
        // pure payload validator means TryPreflightRestore rejects it before account or defense
        // owners are projected into the live scene.
        if (expedition.state == DungeonRunState.Running && snapshot.combat.state != CombatRoomState.Running)
        {
            error = "Running expedition requires running combat world state.";
            return false;
        }

        if (expedition.state == DungeonRunState.AwaitingExit &&
            (snapshot.combat.state != CombatRoomState.Cleared || (snapshot.actors?.Length ?? 0) != 0))
        {
            error = "AwaitingExit expedition requires a cleared combat world with no active enemies.";
            return false;
        }

        if (!TryValidateDungeonActor(snapshot.hero, "hero", CharacterTeam.Player, out error))
        {
            return false;
        }

        HashSet<string> entities = new HashSet<string>(StringComparer.Ordinal) { snapshot.hero.entityId };
        DungeonActorWorldSnapshot[] actors = snapshot.actors ?? Array.Empty<DungeonActorWorldSnapshot>();
        for (int i = 0; i < actors.Length; i++)
        {
            if (!TryValidateDungeonActor(actors[i], $"actor {i}", CharacterTeam.Enemy, out error) ||
                !entities.Add(actors[i].entityId))
            {
                error = string.IsNullOrWhiteSpace(error) ? $"Dungeon actor {i} has a duplicate entity id." : error;
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    private static bool TryValidateBuildings(
        DefenseBuildingWorldSnapshot[] buildings,
        HashSet<string> entities,
        out string error)
    {
        buildings ??= Array.Empty<DefenseBuildingWorldSnapshot>();
        for (int i = 0; i < buildings.Length; i++)
        {
            DefenseBuildingWorldSnapshot building = buildings[i];
            if (building == null || string.IsNullOrWhiteSpace(building.entityId) ||
                string.IsNullOrWhiteSpace(building.archetypeId) || !entities.Add(building.entityId) ||
                building.currentHealth < 0f || building.maxHealth <= 0f || building.currentHealth > building.maxHealth)
            {
                error = $"Defense building {i} is invalid or duplicated.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    private static bool TryValidateDefenseUnits(
        DefenseUnitWorldSnapshot[] units,
        HashSet<string> entities,
        out string error)
    {
        units ??= Array.Empty<DefenseUnitWorldSnapshot>();
        for (int i = 0; i < units.Length; i++)
        {
            DefenseUnitWorldSnapshot unit = units[i];
            if (unit == null || string.IsNullOrWhiteSpace(unit.entityId) || !entities.Add(unit.entityId) ||
                !Enum.IsDefined(typeof(GroundDefenseNavMeshUnitSide), unit.side) ||
                !Enum.IsDefined(typeof(WorldActorAction), unit.action) ||
                unit.currentHealth < 0f || unit.maxHealth <= 0f || unit.currentHealth > unit.maxHealth)
            {
                error = $"Defense unit {i} is invalid or duplicated.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    public static bool TryValidateDungeonActor(
        DungeonActorWorldSnapshot actor,
        string label,
        CharacterTeam expectedTeam,
        out string error)
    {
        if (actor == null || string.IsNullOrWhiteSpace(actor.entityId) ||
            string.IsNullOrWhiteSpace(actor.archetypeId) || actor.team != expectedTeam ||
            !Enum.IsDefined(typeof(WorldActorAction), actor.action) || actor.currentHealth < 0f ||
            actor.maxHealth <= 0f || actor.currentHealth > actor.maxHealth || actor.actionRemainingSeconds < 0f)
        {
            error = $"Dungeon {label} is invalid.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}

public static class WorldCheckpointRecovery
{
    /// <summary>
    /// Both arguments are already checksum-validated by the reader. Selecting here is deliberately
    /// value-only so the same highest-generation rule is used by file recovery and smoke tests.
    /// </summary>
    public static GameProfileSave SelectHighestValid(GameProfileSave primary, GameProfileSave backup)
    {
        if (primary == null)
        {
            return backup;
        }

        return backup != null && backup.generation > primary.generation ? backup : primary;
    }
}
