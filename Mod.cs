using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace FeatherLight;

public sealed record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.shardbyte.featherlight";
    public string Name { get; init; } = "FeatherLight";
    public string Author { get; init; } = "Shardbyte";
    public List<string>? Contributors { get; init; } = ["Chomp"];
    public SemanticVersioning.Version Version { get; init; } = new(
        typeof(ModMetadata).Assembly.GetName().Version?.ToString(3) ?? "2.1.0"
    );
    public SemanticVersioning.Range SptVersion { get; init; } = new("4.1.5");
    public bool HasPrepatcher { get; init; } = false;
    public List<string>? Incompatibilities { get; init; } = [];
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; } = "https://github.com/Shardbyte/SPT-FeatherLight";
    public string License { get; init; } = "MIT";
}

[Injectable(TypePriority = OnLoadOrder.GameCallbacks - 1)]
public sealed class FeatherLightMod(
    TemplateTable templateTable,
    ItemHelper itemHelper,
    ModHelper modHelper,
    WeightBaseline baseline,
    ISptLogger<FeatherLightMod> logger) : IOnLoad
{
    private const double WeightComparisonTolerance = 0.000001d;

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        FeatherLightConfig config;
        try
        {
            config = modHelper.GetJsonDataFromModFile<FeatherLightConfig>("config", "config.json");
        }
        catch (Exception exception)
        {
            logger.Error($"FeatherLight could not read config/config.json and made no changes: {exception.Message}");
            return Task.CompletedTask;
        }

        if (config is null)
        {
            logger.Error("FeatherLight configuration is invalid and no item weights were changed: the file contains null.");
            return Task.CompletedTask;
        }

        if (!config.TryResolve(out var resolved, out var validationError))
        {
            logger.Error($"FeatherLight configuration is invalid and no item weights were changed: {validationError}");
            return Task.CompletedTask;
        }

        var includedCategories = ToMongoIds(resolved.IncludedCategoryIds);
        var excludedCategories = ToMongoIds(resolved.ExcludedCategoryIds);
        var changedCounts = new Dictionary<ItemCategory, int>();
        var conflictCount = 0;
        var loggedConflictCount = 0;
        var invalidWeightCount = 0;

        if (resolved.ConflictDiagnosticsEnabled && baseline.Weights.Count == 0)
        {
            logger.Warning("FeatherLight conflict diagnostics are enabled, but the early weight baseline is unavailable.");
        }

        foreach (var item in templateTable.Items.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var properties = item.Properties;
            if (properties?.Weight is not { } currentWeight
                || !TryGetSelectedCategory(item.Id, resolved, includedCategories, excludedCategories, out var category))
            {
                continue;
            }

            var hasBaseline = baseline.Weights.TryGetValue(item.Id, out var baselineWeight);
            if (!hasBaseline && (!double.IsFinite(currentWeight) || currentWeight < 0))
            {
                invalidWeightCount++;
                continue;
            }

            var sourceWeight = hasBaseline ? baselineWeight : currentWeight;
            if (hasBaseline)
            {
                if (resolved.ConflictDiagnosticsEnabled
                    && WeightsDiffer(currentWeight, baselineWeight))
                {
                    conflictCount++;
                    if (loggedConflictCount < resolved.MaxConflictEntries)
                    {
                        logger.Warning(
                            $"FeatherLight conflict: template {item.Id} changed from baseline "
                            + $"{baselineWeight:0.######} kg to {currentWeight:0.######} kg before FeatherLight applied."
                        );
                        loggedConflictCount++;
                    }
                }
            }

            var targetWeight = resolved.Weight.Apply(sourceWeight);
            if (!WeightsDiffer(currentWeight, targetWeight))
            {
                continue;
            }

            properties.Weight = targetWeight;
            changedCounts[category] = changedCounts.GetValueOrDefault(category) + 1;
        }

        LogUnknownConfiguredIds(resolved, includedCategories, excludedCategories);
        if (resolved.ConflictDiagnosticsEnabled && conflictCount > 0)
        {
            logger.Warning(
                $"FeatherLight detected {conflictCount} selected templates changed after its baseline snapshot; "
                + $"logged {loggedConflictCount}. FeatherLight calculated its result from the captured baseline."
            );
        }

        logger.Success(
            $"FeatherLight preset '{resolved.Preset}' changed {changedCounts.Values.Sum()} item templates "
            + $"using {DescribeWeightRule(resolved.Weight)}. "
            + string.Join(", ", Enum.GetValues<ItemCategory>().Select(
                category => $"{GetCategoryName(category)}={changedCounts.GetValueOrDefault(category)}"
            ))
            + $". Invalid weights skipped={invalidWeightCount}."
        );

        return Task.CompletedTask;
    }

    private bool TryGetSelectedCategory(
        MongoId itemId,
        ResolvedFeatherLightConfig config,
        MongoId[] includedCategories,
        MongoId[] excludedCategories,
        out ItemCategory category)
    {
        var itemIdText = itemId.ToString();
        if (config.ExcludedTemplateIds.Contains(itemIdText)
            || (excludedCategories.Length > 0 && itemHelper.IsOfBaseclasses(itemId, excludedCategories)))
        {
            category = default;
            return false;
        }

        if (config.IncludedTemplateIds.Contains(itemIdText)
            || (includedCategories.Length > 0 && itemHelper.IsOfBaseclasses(itemId, includedCategories)))
        {
            category = ItemCategory.Custom;
            return true;
        }

        if (itemHelper.IsOfBaseclass(itemId, BaseClasses.AMMO))
        {
            category = ItemCategory.Ammo;
            return config.Categories.Ammo;
        }

        if (itemHelper.IsOfBaseclass(itemId, BaseClasses.AMMO_BOX))
        {
            category = ItemCategory.AmmoBoxes;
            return config.Categories.AmmoBoxes;
        }

        if (itemHelper.IsOfBaseclass(itemId, BaseClasses.MAGAZINE))
        {
            category = ItemCategory.Magazines;
            return config.Categories.Magazines;
        }

        if (itemHelper.IsOfBaseclass(itemId, BaseClasses.THROW_WEAP))
        {
            category = ItemCategory.Throwables;
            return config.Categories.Throwables;
        }

        // Narrow categories come before their possible ancestors so toggles stay independent.
        if (itemHelper.IsOfBaseclass(itemId, BaseClasses.DRINK))
        {
            category = ItemCategory.Drinks;
            return config.Categories.Drinks;
        }

        if (itemHelper.IsOfBaseclass(itemId, BaseClasses.FOOD))
        {
            category = ItemCategory.Food;
            return config.Categories.Food;
        }

        if (itemHelper.IsOfBaseclass(itemId, BaseClasses.MEDS))
        {
            category = ItemCategory.Meds;
            return config.Categories.Meds;
        }

        if (itemHelper.IsOfBaseclass(itemId, BaseClasses.VEST))
        {
            category = ItemCategory.Rigs;
            return config.Categories.Rigs;
        }

        if (itemHelper.IsOfBaseclass(itemId, BaseClasses.ARMOR))
        {
            category = ItemCategory.Armor;
            return config.Categories.Armor;
        }

        if (itemHelper.IsOfBaseclass(itemId, BaseClasses.BACKPACK))
        {
            category = ItemCategory.Backpacks;
            return config.Categories.Backpacks;
        }

        if (itemHelper.IsOfBaseclass(itemId, BaseClasses.WEAPON))
        {
            category = ItemCategory.Weapons;
            return config.Categories.Weapons;
        }

        category = default;
        return false;
    }

    private void LogUnknownConfiguredIds(
        ResolvedFeatherLightConfig config,
        MongoId[] includedCategories,
        MongoId[] excludedCategories)
    {
        foreach (var templateId in config.IncludedTemplateIds.Concat(config.ExcludedTemplateIds))
        {
            if (!templateTable.Items.ContainsKey(new MongoId(templateId)))
            {
                logger.Warning($"FeatherLight configured template ID was not found: {templateId}");
            }
        }

        foreach (var categoryId in includedCategories.Concat(excludedCategories))
        {
            if (!templateTable.Items.ContainsKey(categoryId))
            {
                logger.Warning($"FeatherLight configured category ID was not found: {categoryId}");
            }
        }
    }

    private static MongoId[] ToMongoIds(IEnumerable<string> ids)
    {
        return ids.Select(id => new MongoId(id)).ToArray();
    }

    private static string DescribeWeightRule(WeightRule rule)
    {
        return rule.Mode == WeightMode.Percentage
            ? $"a {rule.Value:0.##}% reduction"
            : $"a definitive weight of {rule.Value:0.######} kg";
    }

    private static string GetCategoryName(ItemCategory category)
    {
        return category switch
        {
            ItemCategory.AmmoBoxes => "ammo boxes",
            _ => category.ToString().ToLowerInvariant()
        };
    }

    private static bool WeightsDiffer(double left, double right)
    {
        return !double.IsFinite(left)
            || !double.IsFinite(right)
            || Math.Abs(left - right) > WeightComparisonTolerance;
    }

    private enum ItemCategory
    {
        Ammo,
        AmmoBoxes,
        Magazines,
        Throwables,
        Food,
        Drinks,
        Meds,
        Armor,
        Weapons,
        Rigs,
        Backpacks,
        Custom
    }
}
