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
        typeof(ModMetadata).Assembly.GetName().Version?.ToString(3) ?? "2.0.0"
    );
    public SemanticVersioning.Range SptVersion { get; init; } = new("4.1.5");
    public bool HasPrepatcher { get; init; } = false;
    public List<string>? Incompatibilities { get; init; } = [];
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; } = "https://github.com/Shardbyte/SPT-FeatherLight";
    public string License { get; init; } = "MIT";
}

[Injectable(TypePriority = OnLoadOrder.Preload + 1)]
public sealed class FeatherLightMod(
    TemplateTable templateTable,
    ItemHelper itemHelper,
    ModHelper modHelper,
    ISptLogger<FeatherLightMod> logger) : IOnLoad
{
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

        if (!config.TryValidate(out var validationError))
        {
            logger.Error($"FeatherLight configuration is invalid and no item weights were changed: {validationError}");
            return Task.CompletedTask;
        }

        var changedCounts = new Dictionary<ItemCategory, int>();
        foreach (var item in templateTable.Items.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (item.Properties?.Weight is not > 0
                || !TryGetEnabledCategory(item.Id, config, out var category))
            {
                continue;
            }

            item.Properties.Weight = 0;
            changedCounts[category] = changedCounts.GetValueOrDefault(category) + 1;
        }

        logger.Success(
            $"FeatherLight changed {changedCounts.Values.Sum()} item templates: "
            + $"ammo={changedCounts.GetValueOrDefault(ItemCategory.Ammo)}, "
            + $"throwables={changedCounts.GetValueOrDefault(ItemCategory.Throwables)}, "
            + $"food={changedCounts.GetValueOrDefault(ItemCategory.Food)}, "
            + $"drinks={changedCounts.GetValueOrDefault(ItemCategory.Drinks)}, "
            + $"meds={changedCounts.GetValueOrDefault(ItemCategory.Meds)}."
        );

        return Task.CompletedTask;
    }

    private bool TryGetEnabledCategory(MongoId itemId, FeatherLightConfig config, out ItemCategory category)
    {
        if (itemHelper.IsOfBaseclass(itemId, BaseClasses.AMMO))
        {
            category = ItemCategory.Ammo;
            return config.LightAmmo!.Enabled!.Value;
        }

        if (itemHelper.IsOfBaseclass(itemId, BaseClasses.THROW_WEAP))
        {
            category = ItemCategory.Throwables;
            return config.LightThrowables!.Enabled!.Value;
        }

        // Check drinks before food so the settings remain independent if their hierarchies overlap.
        if (itemHelper.IsOfBaseclass(itemId, BaseClasses.FOOD_DRINK))
        {
            category = ItemCategory.Drinks;
            return config.LightDrink!.Enabled!.Value;
        }

        if (itemHelper.IsOfBaseclass(itemId, BaseClasses.FOOD))
        {
            category = ItemCategory.Food;
            return config.LightFood!.Enabled!.Value;
        }

        if (itemHelper.IsOfBaseclass(itemId, BaseClasses.MEDICAL))
        {
            category = ItemCategory.Meds;
            return config.LightMeds!.Enabled!.Value;
        }

        category = default;
        return false;
    }

    private enum ItemCategory
    {
        Ammo,
        Throwables,
        Food,
        Drinks,
        Meds
    }
}
