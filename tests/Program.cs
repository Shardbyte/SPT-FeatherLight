using System.Text.Json;
using FeatherLight;
using SPTarkov.Server.Core.DI;

var failures = new List<string>();

Run("custom percentage configuration resolves", () =>
{
    var config = Deserialize(CustomConfig());
    Assert(config.TryResolve(out var resolved, out var error), error);
    Assert(resolved.Weight.Mode == WeightMode.Percentage, "Expected percentage mode.");
    AssertClose(0.6d, resolved.Weight.Apply(1d));
    Assert(resolved.Categories.Ammo && !resolved.Categories.Armor, "Expected custom category toggles.");
});

Run("definitive weight resolves", () =>
{
    var config = Deserialize(CustomConfig("definitive", 40, 0.125));
    Assert(config.TryResolve(out var resolved, out var error), error);
    Assert(resolved.Weight.Mode == WeightMode.Definitive, "Expected definitive mode.");
    AssertClose(0.125d, resolved.Weight.Apply(9d));
});

Run("percentage is bounded", () =>
{
    var config = Deserialize(CustomConfig("percentage", 101, 0));
    Assert(!config.TryResolve(out _, out var error), "Expected invalid percentage.");
    Assert(error.Contains("between 0 and 100"), "Expected percentage range error.");
});

Run("missing category is rejected for custom", () =>
{
    var config = Deserialize(CustomConfig().Replace("\"backpacks\": false", "\"backpacksMissing\": false"));
    Assert(!config.TryResolve(out _, out var error), "Expected missing category error.");
    Assert(error.Contains("backpacks"), "Expected backpacks error.");
});

Run("casual preset overrides custom values", () =>
{
    var config = Deserialize(CustomConfig().Replace("\"custom\"", "\"casual\""));
    Assert(config.TryResolve(out var resolved, out var error), error);
    AssertClose(0.5d, resolved.Weight.Apply(1d));
    Assert(resolved.Categories.Armor && resolved.Categories.Backpacks, "Casual should include equipment.");
});

Run("reduced supplies excludes equipment", () =>
{
    var config = Deserialize(CustomConfig().Replace("\"custom\"", "\"reducedSupplies\""));
    Assert(config.TryResolve(out var resolved, out var error), error);
    AssertClose(0.25d, resolved.Weight.Apply(1d));
    Assert(resolved.Categories.AmmoBoxes && resolved.Categories.Magazines, "Supply categories should be enabled.");
    Assert(!resolved.Categories.Armor && !resolved.Categories.Weapons, "Equipment should be disabled.");
});

Run("fully weightless preset uses definitive zero", () =>
{
    var config = Deserialize(CustomConfig().Replace("\"custom\"", "\"fullyWeightless\""));
    Assert(config.TryResolve(out var resolved, out var error), error);
    Assert(resolved.Weight.Mode == WeightMode.Definitive, "Expected definitive mode.");
    AssertClose(0d, resolved.Weight.Apply(12d));
    Assert(resolved.Categories.Weapons && resolved.Categories.Rigs, "All categories should be enabled.");
});

Run("invalid template ID is rejected", () =>
{
    var config = Deserialize(CustomConfig().Replace("\"includedTemplateIds\": []", "\"includedTemplateIds\": [\"bad-id\"]"));
    Assert(!config.TryResolve(out _, out var error), "Expected invalid ID.");
    Assert(error.Contains("includedTemplateIds"), "Expected includedTemplateIds error.");
});

Run("ID lists are normalized and deduplicated", () =>
{
    var config = Deserialize(CustomConfig().Replace(
        "\"includedTemplateIds\": []",
        "\"includedTemplateIds\": [\"ABCDEFABCDEFABCDEFABCDEF\", \"abcdefabcdefabcdefabcdef\"]"
    ));
    Assert(config.TryResolve(out var resolved, out var error), error);
    Assert(resolved.IncludedTemplateIds.Count == 1, "Expected duplicate IDs to collapse.");
    Assert(resolved.IncludedTemplateIds.Contains("abcdefabcdefabcdefabcdef"), "Expected lowercase normalized ID.");
});

Run("conflict log limit is bounded", () =>
{
    var config = Deserialize(CustomConfig().Replace("\"maxLoggedItems\": 25", "\"maxLoggedItems\": 201"));
    Assert(!config.TryResolve(out _, out var error), "Expected invalid conflict limit.");
    Assert(error.Contains("between 0 and 200"), "Expected conflict limit error.");
});

Run("conflict capture precedes application phase", () =>
{
    Assert(OnLoadOrder.Preload < OnLoadOrder.GameCallbacks, "Expected Preload before GameCallbacks.");
});

if (failures.Count > 0)
{
    Console.Error.WriteLine($"{failures.Count} test(s) failed:");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine($"- {failure}");
    }

    return 1;
}

return 0;

static FeatherLightConfig Deserialize(string json)
{
    return JsonSerializer.Deserialize<FeatherLightConfig>(json)
        ?? throw new InvalidOperationException("Configuration deserialized to null.");
}

static string CustomConfig(string mode = "percentage", double percentage = 40, double definitive = 0)
{
    return $$"""
        {
          "preset": "custom",
          "weight": {
            "mode": "{{mode}}",
            "percentageReduction": {{percentage}},
            "definitiveWeightKg": {{definitive}}
          },
          "categories": {
            "ammo": true,
            "ammoBoxes": false,
            "magazines": false,
            "throwables": true,
            "food": true,
            "drinks": true,
            "meds": true,
            "armor": false,
            "weapons": false,
            "rigs": false,
            "backpacks": false
          },
          "conflictDiagnostics": {
            "enabled": true,
            "maxLoggedItems": 25
          },
          "includedTemplateIds": [],
          "includedCategoryIds": [],
          "excludedTemplateIds": [],
          "excludedCategoryIds": []
        }
        """;
}

void Run(string name, Action test)
{
    try
    {
        test();
        Console.WriteLine($"PASS: {name}");
    }
    catch (Exception exception)
    {
        failures.Add($"{name}: {exception.Message}");
    }
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertClose(double expected, double actual)
{
    if (Math.Abs(expected - actual) > 0.000001d)
    {
        throw new InvalidOperationException($"Expected {expected}, got {actual}.");
    }
}
