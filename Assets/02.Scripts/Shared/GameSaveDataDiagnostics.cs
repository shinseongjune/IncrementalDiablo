using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static class GameSaveDataDiagnostics
{
    private const string PrototypeDefinitionPrefix = "prototype_";

    public static bool TryValidate(GameSaveData saveData, out string summary)
    {
        List<string> errors = new List<string>();
        List<string> warnings = new List<string>();

        if (saveData == null)
        {
            summary = "Save snapshot blocked: save data is null.";
            return false;
        }

        ValidateHeader(saveData, errors, warnings);
        ValidateCurrencies(saveData.currencies, errors);
        ValidateDefense(saveData.defense, errors);
        ValidateDungeon(saveData.dungeon, errors);
        ValidateInventoryAndHero(saveData.inventory, saveData.hero, errors, warnings);

        summary = BuildSummary(saveData, errors, warnings);
        return errors.Count == 0;
    }

    public static string BuildShortSummary(GameSaveData saveData)
    {
        if (saveData == null)
        {
            return "Save snapshot: missing";
        }

        int currencyCount = saveData.currencies == null ? 0 : saveData.currencies.Length;
        int itemCount = saveData.inventory?.itemInstances == null ? 0 : saveData.inventory.itemInstances.Length;
        int equippedCount = saveData.hero?.equippedItemInstanceIds == null ? 0 : saveData.hero.equippedItemInstanceIds.Length;
        int frontlineLevel = saveData.defense == null ? 0 : Math.Max(1, saveData.defense.frontlineLevel);
        DungeonRunState dungeonState = saveData.dungeon == null ? DungeonRunState.Ready : saveData.dungeon.state;

        return $"Save snapshot: currencies {currencyCount}, FL {frontlineLevel}, dungeon {dungeonState}, inventory {itemCount}, equipped {equippedCount}.";
    }

    private static void ValidateHeader(GameSaveData saveData, List<string> errors, List<string> warnings)
    {
        if (saveData.version <= 0)
        {
            errors.Add("version must be greater than zero");
        }

        if (string.IsNullOrWhiteSpace(saveData.savedAtUtc))
        {
            warnings.Add("savedAtUtc is empty, so offline progress cannot be bounded from the snapshot");
            return;
        }

        if (!DateTime.TryParse(saveData.savedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out _))
        {
            errors.Add("savedAtUtc is not an ISO-compatible UTC timestamp");
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

        if (!Enum.IsDefined(typeof(DefenseState), defense.state))
        {
            errors.Add("defense state is invalid");
        }

        if (!Enum.IsDefined(typeof(FrontlineMode), defense.mode))
        {
            errors.Add("frontline mode is invalid");
        }

        if (defense.frontlineLevel < 1)
        {
            errors.Add("frontline level must be at least 1");
        }

        if (defense.wallLevel < 1 || defense.towerLevel < 1 || defense.defenderLevel < 1)
        {
            errors.Add("defense upgrade levels must be at least 1");
        }

        if (defense.wallCurrentHealth < 0f)
        {
            errors.Add("wall health cannot be negative");
        }

        if (defense.enemyPressure < 0f || defense.frontlineProgress < 0f || defense.totalElapsed < 0f || defense.levelElapsed < 0f)
        {
            errors.Add("defense timers, pressure, and progress cannot be negative");
        }
    }

    private static void ValidateDungeon(DungeonSaveData dungeon, List<string> errors)
    {
        if (dungeon == null)
        {
            errors.Add("dungeon data is missing");
            return;
        }

        if (!Enum.IsDefined(typeof(DungeonRunState), dungeon.state))
        {
            errors.Add("dungeon state is invalid");
        }

        if (dungeon.depth < 1)
        {
            errors.Add("dungeon depth must be at least 1");
        }

        if (dungeon.totalRooms < 1)
        {
            errors.Add("dungeon totalRooms must be at least 1");
        }

        if (dungeon.currentRoomIndex < 0)
        {
            errors.Add("dungeon currentRoomIndex cannot be negative");
        }

        if (dungeon.totalRooms > 0 && dungeon.currentRoomIndex >= dungeon.totalRooms)
        {
            errors.Add("dungeon currentRoomIndex must be less than totalRooms");
        }

        if (dungeon.roomsCompleted < 0 || dungeon.roomsCompleted > Math.Max(1, dungeon.totalRooms))
        {
            errors.Add("dungeon roomsCompleted is outside the valid room range");
        }

        if (dungeon.elapsedSeconds < 0f)
        {
            errors.Add("dungeon elapsedSeconds cannot be negative");
        }
    }

    private static void ValidateInventoryAndHero(InventorySaveData inventory, HeroSaveData hero, List<string> errors, List<string> warnings)
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

        ItemInstanceSaveData[] items = inventory.itemInstances ?? new ItemInstanceSaveData[0];
        Dictionary<long, ItemInstanceSaveData> itemsById = new Dictionary<long, ItemInstanceSaveData>();
        HashSet<ItemSlot> equippedSlots = new HashSet<ItemSlot>();
        int prototypeItemCount = 0;

        for (int i = 0; i < items.Length; i++)
        {
            ItemInstanceSaveData item = items[i];
            if (item == null)
            {
                errors.Add($"inventory item slot {i} is null");
                continue;
            }

            if (item.instanceId <= 0)
            {
                errors.Add($"inventory item slot {i} has no positive instance id");
                continue;
            }

            if (itemsById.ContainsKey(item.instanceId))
            {
                errors.Add($"inventory item instance id {item.instanceId} is duplicated");
                continue;
            }

            itemsById.Add(item.instanceId, item);

            if (!Enum.IsDefined(typeof(ItemSlot), item.slot))
            {
                errors.Add($"inventory item {item.instanceId} has an invalid slot");
            }

            if (!Enum.IsDefined(typeof(ItemRarity), item.rarity))
            {
                errors.Add($"inventory item {item.instanceId} has an invalid rarity");
            }

            if (item.level < 1)
            {
                errors.Add($"inventory item {item.instanceId} level must be at least 1");
            }

            if (item.durability < 0 || item.durability > 100)
            {
                errors.Add($"inventory item {item.instanceId} durability must stay between 0 and 100");
            }

            if (string.IsNullOrWhiteSpace(item.definitionId))
            {
                warnings.Add($"inventory item {item.instanceId} has no definition id and can only restore as a snapshot");
            }
            else if (item.definitionId.StartsWith(PrototypeDefinitionPrefix, StringComparison.OrdinalIgnoreCase))
            {
                prototypeItemCount++;
            }

            if (item.equipped && !equippedSlots.Add(item.slot))
            {
                errors.Add($"multiple equipped items were saved in slot {item.slot}");
            }
        }

        if (items.Length == 0)
        {
            warnings.Add("inventory is empty; run a dungeon reward before treating this as a full loop smoke test");
        }

        if (prototypeItemCount > 0)
        {
            warnings.Add($"{prototypeItemCount} runtime prototype item(s) will restore from saved slot/rarity/power; replace this with authored ItemDefinition registry before production balance");
        }

        ValidateHeroEquipment(hero, itemsById, errors, warnings);
    }

    private static void ValidateHeroEquipment(HeroSaveData hero, Dictionary<long, ItemInstanceSaveData> itemsById, List<string> errors, List<string> warnings)
    {
        if (hero == null)
        {
            errors.Add("hero data is missing");
            return;
        }

        long[] equippedIds = hero.equippedItemInstanceIds ?? new long[0];
        HashSet<long> equippedIdSet = new HashSet<long>();

        for (int i = 0; i < equippedIds.Length; i++)
        {
            long instanceId = equippedIds[i];
            if (instanceId <= 0)
            {
                errors.Add($"hero equipped id at slot {i} is not positive");
                continue;
            }

            if (!equippedIdSet.Add(instanceId))
            {
                errors.Add($"hero equipped item id {instanceId} is duplicated");
                continue;
            }

            if (!itemsById.TryGetValue(instanceId, out ItemInstanceSaveData item))
            {
                errors.Add($"hero equipped item id {instanceId} is not present in inventory");
                continue;
            }

            if (!item.equipped)
            {
                warnings.Add($"hero equipped item id {instanceId} is not marked equipped in inventory");
            }
        }

        foreach (KeyValuePair<long, ItemInstanceSaveData> pair in itemsById)
        {
            if (pair.Value.equipped && !equippedIdSet.Contains(pair.Key))
            {
                warnings.Add($"inventory item {pair.Key} is marked equipped but is absent from hero.equippedItemInstanceIds");
            }
        }
    }

    private static string BuildSummary(GameSaveData saveData, List<string> errors, List<string> warnings)
    {
        StringBuilder builder = new StringBuilder(320);
        builder.Append(errors.Count == 0 ? "Save snapshot OK" : "Save snapshot blocked");
        builder.Append(": ");
        builder.Append(BuildShortSummary(saveData));
        builder.Append(" Errors ");
        builder.Append(errors.Count);
        builder.Append(", warnings ");
        builder.Append(warnings.Count);
        builder.Append('.');

        AppendFindings(builder, "Error", errors, 3);
        AppendFindings(builder, "Warning", warnings, 3);
        return builder.ToString();
    }

    private static void AppendFindings(StringBuilder builder, string label, List<string> findings, int maxCount)
    {
        int count = Math.Min(maxCount, findings.Count);
        for (int i = 0; i < count; i++)
        {
            builder.AppendLine();
            builder.Append(label);
            builder.Append(": ");
            builder.Append(findings[i]);
        }

        int remaining = findings.Count - count;
        if (remaining > 0)
        {
            builder.AppendLine();
            builder.Append(label);
            builder.Append(": ");
            builder.Append(remaining);
            builder.Append(" more.");
        }
    }
}
