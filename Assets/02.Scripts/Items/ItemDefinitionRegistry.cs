using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[CreateAssetMenu(menuName = "Incremental Diablo/Items/Item Definition Registry")]
public sealed class ItemDefinitionRegistry : ScriptableObject
{
    [SerializeField] private ItemDefinition[] definitions = new ItemDefinition[0];
    [SerializeField] private ItemDefinitionIdMigration[] idMigrations = new ItemDefinitionIdMigration[0];

    public int DefinitionCount => definitions == null ? 0 : definitions.Length;
    public int MigrationCount => idMigrations == null ? 0 : idMigrations.Length;

    public bool Contains(ItemDefinition definition)
    {
        return definition != null
            && TryResolve(definition.Id, out ItemDefinition resolvedDefinition, out _)
            && resolvedDefinition == definition;
    }

    public bool TryResolve(
        string savedDefinitionId,
        out ItemDefinition definition,
        out ItemDefinitionResolution resolution)
    {
        definition = FindCanonicalDefinition(savedDefinitionId);
        if (definition != null)
        {
            resolution = ItemDefinitionResolution.Canonical;
            return true;
        }

        if (idMigrations != null)
        {
            for (int i = 0; i < idMigrations.Length; i++)
            {
                ItemDefinitionIdMigration migration = idMigrations[i];
                if (migration == null ||
                    !string.Equals(migration.LegacyId, savedDefinitionId, StringComparison.Ordinal) ||
                    migration.Replacement == null ||
                    !ContainsCanonicalReference(migration.Replacement))
                {
                    continue;
                }

                definition = migration.Replacement;
                resolution = ItemDefinitionResolution.Migrated;
                return true;
            }
        }

        resolution = ItemDefinitionResolution.Unknown;
        return false;
    }

    public ItemDefinitionMigrationReport MigrateInventorySaveData(InventorySaveData saveData)
    {
        ItemDefinitionMigrationReport report = new ItemDefinitionMigrationReport();
        ItemInstanceSaveData[] items = saveData?.itemInstances ?? new ItemInstanceSaveData[0];
        for (int i = 0; i < items.Length; i++)
        {
            ItemInstanceSaveData item = items[i];
            if (item == null)
            {
                continue;
            }

            if (!TryResolve(item.definitionId, out ItemDefinition definition, out ItemDefinitionResolution resolution))
            {
                report.AddUnresolved(item.definitionId);
                continue;
            }

            if (resolution == ItemDefinitionResolution.Migrated)
            {
                item.definitionId = definition.Id;
                report.AddMigrated();
            }
            else
            {
                report.AddResolved();
            }
        }

        return report;
    }

    public bool TryValidate(out string report)
    {
        List<string> errors = new List<string>();
        HashSet<string> canonicalIds = new HashSet<string>(StringComparer.Ordinal);
        ItemDefinition[] sourceDefinitions = definitions ?? new ItemDefinition[0];
        for (int i = 0; i < sourceDefinitions.Length; i++)
        {
            ItemDefinition definition = sourceDefinitions[i];
            if (definition == null)
            {
                errors.Add($"definition slot {i} is empty");
                continue;
            }

            if (string.IsNullOrWhiteSpace(definition.Id))
            {
                errors.Add($"definition slot {i} has no stable id");
                continue;
            }

            if (!canonicalIds.Add(definition.Id))
            {
                errors.Add($"definition id '{definition.Id}' is duplicated");
            }
        }

        HashSet<string> legacyIds = new HashSet<string>(StringComparer.Ordinal);
        ItemDefinitionIdMigration[] migrations = idMigrations ?? new ItemDefinitionIdMigration[0];
        for (int i = 0; i < migrations.Length; i++)
        {
            ItemDefinitionIdMigration migration = migrations[i];
            if (migration == null || string.IsNullOrWhiteSpace(migration.LegacyId))
            {
                errors.Add($"migration slot {i} has no legacy id");
                continue;
            }

            if (!legacyIds.Add(migration.LegacyId))
            {
                errors.Add($"legacy id '{migration.LegacyId}' is duplicated");
            }

            if (canonicalIds.Contains(migration.LegacyId))
            {
                errors.Add($"legacy id '{migration.LegacyId}' conflicts with a canonical id");
            }

            if (migration.Replacement == null || !ContainsCanonicalReference(migration.Replacement))
            {
                errors.Add($"legacy id '{migration.LegacyId}' has no registered replacement");
            }
        }

        if (errors.Count == 0)
        {
            report = $"Item registry OK: {canonicalIds.Count} definitions, {legacyIds.Count} id migrations.";
            return true;
        }

        report = $"Item registry blocked: {errors.Count} error(s). {string.Join("; ", errors)}";
        return false;
    }

    private ItemDefinition FindCanonicalDefinition(string definitionId)
    {
        if (string.IsNullOrWhiteSpace(definitionId) || definitions == null)
        {
            return null;
        }

        for (int i = 0; i < definitions.Length; i++)
        {
            ItemDefinition definition = definitions[i];
            if (definition != null && string.Equals(definition.Id, definitionId, StringComparison.Ordinal))
            {
                return definition;
            }
        }

        return null;
    }

    private bool ContainsCanonicalReference(ItemDefinition definition)
    {
        if (definition == null || definitions == null)
        {
            return false;
        }

        for (int i = 0; i < definitions.Length; i++)
        {
            if (definitions[i] == definition)
            {
                return true;
            }
        }

        return false;
    }
}

public enum ItemDefinitionResolution
{
    Unknown,
    Canonical,
    Migrated
}

[Serializable]
public sealed class ItemDefinitionIdMigration
{
    [SerializeField] private string legacyId;
    [SerializeField] private ItemDefinition replacement;

    public string LegacyId => legacyId;
    public ItemDefinition Replacement => replacement;
}

public sealed class ItemDefinitionMigrationReport
{
    private readonly List<string> unresolvedIds = new List<string>();

    public int ResolvedCount { get; private set; }
    public int MigratedCount { get; private set; }
    public int UnresolvedCount { get; private set; }
    public bool HasUnresolved => UnresolvedCount > 0;

    public void AddResolved()
    {
        ResolvedCount++;
    }

    public void AddMigrated()
    {
        MigratedCount++;
    }

    public void AddUnresolved(string definitionId)
    {
        UnresolvedCount++;
        string normalizedId = string.IsNullOrWhiteSpace(definitionId) ? "<missing>" : definitionId;
        if (!unresolvedIds.Contains(normalizedId))
        {
            unresolvedIds.Add(normalizedId);
        }
    }

    public string BuildSummary()
    {
        StringBuilder builder = new StringBuilder(160);
        builder.Append("Item migration: ");
        builder.Append(ResolvedCount);
        builder.Append(" resolved, ");
        builder.Append(MigratedCount);
        builder.Append(" id remapped, ");
        builder.Append(UnresolvedCount);
        builder.Append(" unresolved");

        if (unresolvedIds.Count > 0)
        {
            builder.Append(" [");
            int visibleCount = Math.Min(3, unresolvedIds.Count);
            for (int i = 0; i < visibleCount; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(unresolvedIds[i]);
            }

            if (unresolvedIds.Count > visibleCount)
            {
                builder.Append(", ...");
            }

            builder.Append(']');
        }

        builder.Append('.');
        return builder.ToString();
    }
}
