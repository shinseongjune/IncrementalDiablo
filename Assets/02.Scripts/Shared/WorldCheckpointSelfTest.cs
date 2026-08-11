using System;
using UnityEngine;

/// <summary>
/// In-memory contract test used by the playable-loop smoke command. It proves format round-trip,
/// hash corruption rejection, highest-generation recovery selection, and a running-room checkpoint
/// without touching the player's persistent files.
/// </summary>
public static class WorldCheckpointSelfTest
{
    public static bool TryRun(out string report)
    {
        GameProfileSave running = CreateRunningProfile(7);
        GameProfileSaveValidator.Seal(running);
        string json = JsonUtility.ToJson(running);
        GameProfileSave roundTrip = JsonUtility.FromJson<GameProfileSave>(json);
        if (!GameProfileSaveValidator.TryValidate(roundTrip, null, out string roundTripReport))
        {
            report = $"World checkpoint round-trip failed: {roundTripReport}";
            return false;
        }

        GameProfileSave awaitingExit = CreateAwaitingExitProfile(8);
        GameProfileSaveValidator.Seal(awaitingExit);
        if (!GameProfileSaveValidator.TryValidate(awaitingExit, null, out string awaitingExitReport))
        {
            report = $"World checkpoint AwaitingExit validation failed: {awaitingExitReport}";
            return false;
        }

        GameProfileSave mismatchedLifecycle = JsonUtility.FromJson<GameProfileSave>(JsonUtility.ToJson(awaitingExit));
        mismatchedLifecycle.dungeonWorld.combat.state = CombatRoomState.Running;
        GameProfileSaveValidator.Seal(mismatchedLifecycle);
        if (GameProfileSaveValidator.TryValidate(mismatchedLifecycle, null, out _))
        {
            report = "World checkpoint accepted an AwaitingExit combat lifecycle mismatch.";
            return false;
        }

        GameProfileSave corrupt = JsonUtility.FromJson<GameProfileSave>(json);
        corrupt.integrityHash = "corrupt";
        if (GameProfileSaveValidator.TryValidate(corrupt, null, out _))
        {
            report = "World checkpoint corruption was accepted.";
            return false;
        }

        GameProfileSave older = CreateRunningProfile(6);
        GameProfileSaveValidator.Seal(older);
        GameProfileSave selected = WorldCheckpointRecovery.SelectHighestValid(older, roundTrip);
        if (selected == null || selected.generation != roundTrip.generation)
        {
            report = "World checkpoint recovery did not select the highest valid generation.";
            return false;
        }

        report = "World checkpoint self-test passed: running/AwaitingExit lifecycle validation, corruption rejection, and highest-generation recovery.";
        return true;
    }

    private static GameProfileSave CreateRunningProfile(long generation)
    {
        DungeonRunPlan plan = DungeonRunPlan.CreateNew("self_test", 1717, 1);
        plan.AssignCurrentRoomTemplate("crypt_a");
        DungeonExpeditionSnapshot expedition = new DungeonExpeditionSnapshot
        {
            state = DungeonRunState.Running,
            resumePoint = DungeonRoomResumePoint.RestartCurrentRoom,
            dungeonId = "self_test",
            depth = 1,
            selectedDepth = 1,
            highestUnlockedDepth = 1,
            activeContractId = DungeonContractModel.DefaultContractId,
            activeEncounterId = DungeonEncounterModel.DefaultEncounterId,
            runPlan = plan,
            totalRooms = 1,
            currentRoomIndex = 0
        };
        return new GameProfileSave
        {
            generation = generation,
            savedAtUtc = DateTime.UtcNow.ToString("O"),
            account = new AccountSnapshot
            {
                currencies = new[]
                {
                    new ResourceAmount(ResourceId.Gold, 10),
                    new ResourceAmount(ResourceId.Scrap, 5)
                },
                expedition = expedition
            },
            defenseWorld = new DefenseWorldSnapshot
            {
                buildings = new[]
                {
                    new DefenseBuildingWorldSnapshot
                    {
                        entityId = "defense-wall",
                        archetypeId = "wall",
                        currentHealth = 100f,
                        maxHealth = 100f
                    }
                }
            },
            dungeonWorld = new DungeonWorldSnapshot
            {
                isOpen = true,
                templateId = "crypt_a",
                roomSeed = plan.currentRoomSeed,
                roomIndex = 0,
                combat = new DungeonCombatWorldSnapshot { state = CombatRoomState.Running },
                hero = CreateActor("hero", "player", CharacterTeam.Player),
                actors = new[] { CreateActor("dungeon-enemy-01", "melee_enemy", CharacterTeam.Enemy) }
            }
        };
    }

    private static GameProfileSave CreateAwaitingExitProfile(long generation)
    {
        GameProfileSave profile = CreateRunningProfile(generation);
        profile.account.expedition.state = DungeonRunState.AwaitingExit;
        profile.account.expedition.resumePoint = DungeonRoomResumePoint.AwaitingExit;
        profile.account.expedition.rewardPending = true;
        profile.account.expedition.runPlan.SetRewardPending(true, 1);
        profile.dungeonWorld.combat.state = CombatRoomState.Cleared;
        profile.dungeonWorld.actors = Array.Empty<DungeonActorWorldSnapshot>();
        return profile;
    }

    private static DungeonActorWorldSnapshot CreateActor(string id, string archetype, CharacterTeam team)
    {
        return new DungeonActorWorldSnapshot
        {
            entityId = id,
            archetypeId = archetype,
            team = team,
            currentHealth = 100f,
            maxHealth = 100f,
            action = WorldActorAction.Idle,
            active = true
        };
    }
}
