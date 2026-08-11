using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>
/// Validates a v2 checkpoint before any owner projects it into the live scene. Validation never
/// repairs values: the writer creates a new generation and the reader selects a valid generation.
/// </summary>
public static class GameProfileSaveValidator
{
    private const string PrototypeDefinitionPrefix = "prototype_";

    public static void Seal(GameProfileSave profile)
    {
        if (profile == null)
        {
            return;
        }

        profile.integrityHash = ComputeIntegrityHash(profile);
    }

    public static bool TryValidate(
        GameProfileSave profile,
        ItemDefinitionRegistry definitionRegistry,
        out string summary)
    {
        List<string> errors = new List<string>();
        List<string> warnings = new List<string>();
        if (profile == null)
        {
            summary = "Profile blocked: data is null.";
            return false;
        }

        ValidateHeader(profile, errors);
        AccountSnapshot account = profile.account;
        if (account == null)
        {
            errors.Add("account snapshot is missing");
        }
        else
        {
            ValidateCurrencies(account.currencies, errors);
            ValidateDefense(account.defense, errors);
            ValidateExpedition(account.expedition, errors);
            ValidateInventoryAndHero(account.inventory, account.hero, definitionRegistry, errors, warnings);
            ValidateUiSettings(account.uiSettings, warnings);
        }

        if (!WorldSaveSnapshotValidator.TryValidate(profile.defenseWorld, out string defenseWorldError))
        {
            errors.Add(defenseWorldError);
        }

        ValidateDungeonWorld(profile.dungeonWorld, account?.expedition, errors);
        summary = BuildSummary(profile, errors, warnings);
        return errors.Count == 0;
    }

    public static string BuildShortSummary(GameProfileSave profile)
    {
        if (profile == null)
        {
            return "Profile: missing";
        }

        AccountSnapshot account = profile.account;
        DungeonExpeditionSnapshot expedition = account?.expedition;
        int currencyCount = account?.currencies == null ? 0 : account.currencies.Length;
        int itemCount = account?.inventory?.itemInstances == null ? 0 : account.inventory.itemInstances.Length;
        int equippedCount = account?.hero?.equippedItemInstanceIds == null ? 0 : account.hero.equippedItemInstanceIds.Length;
        int frontlineLevel = account?.defense == null ? 0 : Math.Max(1, account.defense.frontlineLevel);
        DungeonRunState dungeonState = expedition == null ? DungeonRunState.Ready : expedition.state;
        int dungeonDepth = expedition == null ? 1 : Math.Max(1, expedition.depth);
        int worldActorCount = profile.dungeonWorld?.actors == null ? 0 : profile.dungeonWorld.actors.Length;

        return $"Profile v{profile.formatVersion} g{profile.generation}: currencies {currencyCount}, FL {frontlineLevel}, dungeon {dungeonState} D{dungeonDepth}, world actors {worldActorCount}, inventory {itemCount}, equipped {equippedCount}.";
    }

    private static void ValidateHeader(GameProfileSave profile, List<string> errors)
    {
        if (profile.formatVersion != GameProfileSave.CurrentFormatVersion)
        {
            errors.Add($"profile format {profile.formatVersion} is unsupported");
        }

        if (profile.generation < 1)
        {
            errors.Add("profile generation must be at least 1");
        }

        if (string.IsNullOrWhiteSpace(profile.savedAtUtc) ||
            !DateTime.TryParse(profile.savedAtUtc, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out _))
        {
            errors.Add("savedAtUtc is not an ISO-compatible UTC timestamp");
        }

        if (string.IsNullOrWhiteSpace(profile.integrityHash) ||
            !string.Equals(profile.integrityHash, ComputeIntegrityHash(profile), StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("profile integrity hash does not match its payload");
        }
    }

    private static string ComputeIntegrityHash(GameProfileSave profile)
    {
        GameProfileSave unsigned = new GameProfileSave
        {
            formatVersion = profile.formatVersion,
            generation = profile.generation,
            savedAtUtc = profile.savedAtUtc,
            integrityHash = string.Empty,
            account = profile.account,
            defenseWorld = profile.defenseWorld,
            dungeonWorld = profile.dungeonWorld
        };
        byte[] payload = Encoding.UTF8.GetBytes(JsonUtility.ToJson(unsigned, false));
        using SHA256 sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(payload);
        StringBuilder builder = new StringBuilder(hash.Length * 2);
        for (int i = 0; i < hash.Length; i++)
        {
            builder.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private static void ValidateDungeonWorld(
        DungeonWorldSnapshot dungeonWorld,
        DungeonExpeditionSnapshot expedition,
        List<string> errors)
    {
        if (expedition == null)
        {
            return;
        }

        if (expedition.state == DungeonRunState.Ready)
        {
            if (dungeonWorld != null && dungeonWorld.isOpen)
            {
                errors.Add("ready expedition cannot retain an open dungeon world");
            }

            return;
        }

        if (dungeonWorld == null)
        {
            errors.Add("active expedition requires an open dungeon world snapshot");
            return;
        }

        if (!WorldSaveSnapshotValidator.TryValidate(dungeonWorld, expedition, out string dungeonWorldError))
        {
            errors.Add(dungeonWorldError);
        }
    }

    private static void ValidateCurrencies(ResourceAmount[] currencies, List<string> errors)
    {
        if (currencies == null || currencies.Length == 0)
        {
            errors.Add("currencies are missing");
            return;
        }

        HashSet<ResourceId> seen = new HashSet<ResourceId>();
        for (int i = 0; i < currencies.Length; i++)
        {
            ResourceId resource = currencies[i].Resource;
            if (!Enum.IsDefined(typeof(ResourceId), resource))
            {
                errors.Add($"currency slot {i} has an unknown resource id");
                continue;
            }

            if (!seen.Add(resource))
            {
                errors.Add($"currency {resource} is duplicated");
            }
        }

        if (!seen.Contains(ResourceId.Gold))
        {
            errors.Add("Gold is missing from currencies");
        }

        if (!seen.Contains(ResourceId.Scrap))
        {
            errors.Add("Scrap is missing from currencies");
        }
    }

    private static void ValidateDefense(DefenseSaveData defense, List<string> errors)
    {
        if (defense == null)
        {
            errors.Add("defense data is missing");
            return;
        }

        if (!Enum.IsDefined(typeof(DefenseState), defense.state) || !Enum.IsDefined(typeof(FrontlineMode), defense.mode))
        {
            errors.Add("defense state or mode is invalid");
        }

        if (defense.frontlineLevel < 1 || defense.wallLevel < 1 || defense.towerLevel < 1 || defense.defenderLevel < 1 ||
            defense.wallCurrentHealth < 0f || defense.enemyPressure < 0f || defense.frontlineProgress < 0f ||
            defense.totalElapsed < 0f || defense.levelElapsed < 0f)
        {
            errors.Add("defense values are invalid");
        }
    }

    private static void ValidateExpedition(DungeonExpeditionSnapshot expedition, List<string> errors)
    {
        if (expedition == null)
        {
            errors.Add("expedition snapshot is missing");
            return;
        }

        if (!expedition.TryValidate(out string snapshotError))
        {
            errors.Add(snapshotError);
            return;
        }

        if (expedition.totalRooms < 1 || expedition.currentRoomIndex < 0 || expedition.currentRoomIndex >= expedition.totalRooms ||
            expedition.roomsCompleted < 0 || expedition.roomsCompleted > expedition.totalRooms || expedition.elapsedSeconds < 0f ||
            expedition.contractOfferSeed < 0 || expedition.encounterSeed < 0)
        {
            errors.Add("expedition room, time, or seed values are invalid");
        }
    }

    private static void ValidateInventoryAndHero(
        InventorySaveData inventory,
        HeroSaveData hero,
        ItemDefinitionRegistry definitionRegistry,
        List<string> errors,
        List<string> warnings)
    {
        if (inventory == null)
        {
            errors.Add("inventory data is missing");
            return;
        }

        if (inventory.nextItemInstanceId < 1)
        {
            errors.Add("inventory nextItemInstanceId must be at least 1");
        }

        if (definitionRegistry != null && !definitionRegistry.TryValidate(out string registryReport))
        {
            errors.Add(registryReport);
        }

        ItemInstanceSaveData[] items = inventory.itemInstances ?? Array.Empty<ItemInstanceSaveData>();
        Dictionary<long, ItemInstanceSaveData> itemsById = new Dictionary<long, ItemInstanceSaveData>();
        HashSet<ItemSlot> equippedSlots = new HashSet<ItemSlot>();
        int unresolvedDefinitionCount = 0;
        for (int i = 0; i < items.Length; i++)
        {
            ItemInstanceSaveData item = items[i];
            if (item == null || item.instanceId <= 0 || !itemsById.TryAdd(item.instanceId, item))
            {
                errors.Add($"inventory item slot {i} is incomplete or duplicated");
                continue;
            }

            if (!Enum.IsDefined(typeof(ItemSlot), item.slot) || !Enum.IsDefined(typeof(ItemRarity), item.rarity) ||
                item.level < 1 || item.durability < 0 || item.durability > 100 || string.IsNullOrWhiteSpace(item.definitionId))
            {
                errors.Add($"inventory item {item.instanceId} is invalid");
            }
            else if (item.definitionId.StartsWith(PrototypeDefinitionPrefix, StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add($"inventory item {item.instanceId} uses a prototype definition");
            }
            else if (definitionRegistry != null && !definitionRegistry.TryResolve(item.definitionId, out _, out _))
            {
                unresolvedDefinitionCount++;
            }

            if (item.equipped && !equippedSlots.Add(item.slot))
            {
                errors.Add($"multiple equipped items were saved in slot {item.slot}");
            }
        }

        if (definitionRegistry == null && items.Length > 0)
        {
            warnings.Add("item definition registry is unavailable");
        }
        else if (unresolvedDefinitionCount > 0)
        {
            errors.Add($"{unresolvedDefinitionCount} saved item definition(s) are unresolved");
        }

        if (hero == null)
        {
            errors.Add("hero data is missing");
            return;
        }

        long[] equippedIds = hero.equippedItemInstanceIds ?? Array.Empty<long>();
        HashSet<long> seenIds = new HashSet<long>();
        for (int i = 0; i < equippedIds.Length; i++)
        {
            long id = equippedIds[i];
            if (id <= 0 || !seenIds.Add(id) || !itemsById.TryGetValue(id, out ItemInstanceSaveData item) || !item.equipped)
            {
                errors.Add($"hero equipped item id at slot {i} does not match inventory");
            }
        }
    }

    private static void ValidateUiSettings(UiSettingsSaveData uiSettings, List<string> warnings)
    {
        if (uiSettings == null)
        {
            warnings.Add("HUD settings are missing and will use runtime defaults");
        }
    }

    private static string BuildSummary(GameProfileSave profile, List<string> errors, List<string> warnings)
    {
        StringBuilder builder = new StringBuilder(320);
        builder.Append(errors.Count == 0 ? "Profile OK: " : "Profile blocked: ");
        builder.Append(BuildShortSummary(profile));
        builder.Append(" Errors ").Append(errors.Count).Append(", warnings ").Append(warnings.Count).Append('.');
        AppendFindings(builder, "Error", errors);
        AppendFindings(builder, "Warning", warnings);
        return builder.ToString();
    }

    private static void AppendFindings(StringBuilder builder, string label, List<string> findings)
    {
        int count = Math.Min(3, findings.Count);
        for (int i = 0; i < count; i++)
        {
            builder.AppendLine();
            builder.Append(label).Append(": ").Append(findings[i]);
        }
    }
}
