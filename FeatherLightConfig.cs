using System.Text.Json.Serialization;

namespace FeatherLight;

public enum WeightMode
{
    Percentage,
    Definitive
}

public sealed record WeightRule(WeightMode Mode, double Value)
{
    public double Apply(double originalWeight)
    {
        return Mode switch
        {
            WeightMode.Percentage => originalWeight * (1d - (Value / 100d)),
            WeightMode.Definitive => Value,
            _ => throw new ArgumentOutOfRangeException(nameof(Mode), Mode, "Unsupported weight mode.")
        };
    }
}

public sealed record CategorySelection(
    bool Ammo,
    bool AmmoBoxes,
    bool Magazines,
    bool Throwables,
    bool Food,
    bool Drinks,
    bool Meds,
    bool Armor,
    bool Weapons,
    bool Rigs,
    bool Backpacks
);

public sealed record ResolvedFeatherLightConfig(
    string Preset,
    WeightRule Weight,
    CategorySelection Categories,
    bool ConflictDiagnosticsEnabled,
    int MaxConflictEntries,
    IReadOnlySet<string> IncludedTemplateIds,
    IReadOnlyList<string> IncludedCategoryIds,
    IReadOnlySet<string> ExcludedTemplateIds,
    IReadOnlyList<string> ExcludedCategoryIds
);

public sealed record WeightRuleConfig
{
    [JsonPropertyName("mode")]
    public string? Mode { get; init; }

    [JsonPropertyName("percentageReduction")]
    public double? PercentageReduction { get; init; }

    [JsonPropertyName("definitiveWeightKg")]
    public double? DefinitiveWeightKg { get; init; }
}

public sealed record CategoryConfig
{
    [JsonPropertyName("ammo")]
    public bool? Ammo { get; init; }

    [JsonPropertyName("ammoBoxes")]
    public bool? AmmoBoxes { get; init; }

    [JsonPropertyName("magazines")]
    public bool? Magazines { get; init; }

    [JsonPropertyName("throwables")]
    public bool? Throwables { get; init; }

    [JsonPropertyName("food")]
    public bool? Food { get; init; }

    [JsonPropertyName("drinks")]
    public bool? Drinks { get; init; }

    [JsonPropertyName("meds")]
    public bool? Meds { get; init; }

    [JsonPropertyName("armor")]
    public bool? Armor { get; init; }

    [JsonPropertyName("weapons")]
    public bool? Weapons { get; init; }

    [JsonPropertyName("rigs")]
    public bool? Rigs { get; init; }

    [JsonPropertyName("backpacks")]
    public bool? Backpacks { get; init; }

    public bool TryResolve(out CategorySelection selection, out string error)
    {
        var missing = new List<string>();
        AddIfMissing(Ammo, "ammo", missing);
        AddIfMissing(AmmoBoxes, "ammoBoxes", missing);
        AddIfMissing(Magazines, "magazines", missing);
        AddIfMissing(Throwables, "throwables", missing);
        AddIfMissing(Food, "food", missing);
        AddIfMissing(Drinks, "drinks", missing);
        AddIfMissing(Meds, "meds", missing);
        AddIfMissing(Armor, "armor", missing);
        AddIfMissing(Weapons, "weapons", missing);
        AddIfMissing(Rigs, "rigs", missing);
        AddIfMissing(Backpacks, "backpacks", missing);

        if (missing.Count > 0)
        {
            selection = default!;
            error = $"Missing category options: {string.Join(", ", missing)}";
            return false;
        }

        selection = new CategorySelection(
            Ammo!.Value,
            AmmoBoxes!.Value,
            Magazines!.Value,
            Throwables!.Value,
            Food!.Value,
            Drinks!.Value,
            Meds!.Value,
            Armor!.Value,
            Weapons!.Value,
            Rigs!.Value,
            Backpacks!.Value
        );
        error = string.Empty;
        return true;
    }

    private static void AddIfMissing(bool? value, string name, List<string> missing)
    {
        if (value is null)
        {
            missing.Add(name);
        }
    }
}

public sealed record ConflictDiagnosticsConfig
{
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; init; }

    [JsonPropertyName("maxLoggedItems")]
    public int? MaxLoggedItems { get; init; }
}

public sealed record FeatherLightConfig
{
    private const int MaximumConflictLogEntries = 200;

    [JsonPropertyName("preset")]
    public string? Preset { get; init; }

    [JsonPropertyName("weight")]
    public WeightRuleConfig? Weight { get; init; }

    [JsonPropertyName("categories")]
    public CategoryConfig? Categories { get; init; }

    [JsonPropertyName("conflictDiagnostics")]
    public ConflictDiagnosticsConfig? ConflictDiagnostics { get; init; }

    [JsonPropertyName("includedTemplateIds")]
    public List<string>? IncludedTemplateIds { get; init; }

    [JsonPropertyName("includedCategoryIds")]
    public List<string>? IncludedCategoryIds { get; init; }

    [JsonPropertyName("excludedTemplateIds")]
    public List<string>? ExcludedTemplateIds { get; init; }

    [JsonPropertyName("excludedCategoryIds")]
    public List<string>? ExcludedCategoryIds { get; init; }

    public bool TryResolve(out ResolvedFeatherLightConfig resolved, out string error)
    {
        resolved = default!;
        var preset = Preset?.Trim();
        if (string.IsNullOrWhiteSpace(preset))
        {
            error = "preset is required.";
            return false;
        }

        if (!TryResolveDiagnostics(out var diagnosticsEnabled, out var maxConflictEntries, out error)
            || !TryResolveIdList(IncludedTemplateIds, "includedTemplateIds", out var includedTemplates, out error)
            || !TryResolveIdList(IncludedCategoryIds, "includedCategoryIds", out var includedCategories, out error)
            || !TryResolveIdList(ExcludedTemplateIds, "excludedTemplateIds", out var excludedTemplates, out error)
            || !TryResolveIdList(ExcludedCategoryIds, "excludedCategoryIds", out var excludedCategories, out error))
        {
            return false;
        }

        WeightRule weight;
        CategorySelection categories;
        switch (preset.ToLowerInvariant())
        {
            case "custom":
                if (!TryResolveCustom(out weight, out categories, out error))
                {
                    return false;
                }

                break;
            case "casual":
                weight = new WeightRule(WeightMode.Percentage, 50d);
                categories = AllCategories();
                break;
            case "reducedsupplies":
                weight = new WeightRule(WeightMode.Percentage, 75d);
                categories = SupplyCategories();
                break;
            case "fullyweightless":
                weight = new WeightRule(WeightMode.Definitive, 0d);
                categories = AllCategories();
                break;
            default:
                error = "preset must be custom, casual, reducedSupplies, or fullyWeightless.";
                return false;
        }

        resolved = new ResolvedFeatherLightConfig(
            preset,
            weight,
            categories,
            diagnosticsEnabled,
            maxConflictEntries,
            includedTemplates.ToHashSet(StringComparer.OrdinalIgnoreCase),
            includedCategories,
            excludedTemplates.ToHashSet(StringComparer.OrdinalIgnoreCase),
            excludedCategories
        );
        error = string.Empty;
        return true;
    }

    private bool TryResolveCustom(out WeightRule weight, out CategorySelection categories, out string error)
    {
        weight = default!;
        categories = default!;
        if (Weight?.Mode is not { } mode)
        {
            error = "weight.mode is required for the custom preset.";
            return false;
        }

        switch (mode.Trim().ToLowerInvariant())
        {
            case "percentage":
                if (Weight.PercentageReduction is not { } percentage
                    || !double.IsFinite(percentage)
                    || percentage is < 0 or > 100)
                {
                    error = "weight.percentageReduction must be between 0 and 100.";
                    return false;
                }

                weight = new WeightRule(WeightMode.Percentage, percentage);
                break;
            case "definitive":
                if (Weight.DefinitiveWeightKg is not { } definitiveWeight
                    || !double.IsFinite(definitiveWeight)
                    || definitiveWeight < 0)
                {
                    error = "weight.definitiveWeightKg must be zero or greater.";
                    return false;
                }

                weight = new WeightRule(WeightMode.Definitive, definitiveWeight);
                break;
            default:
                error = "weight.mode must be percentage or definitive.";
                return false;
        }

        if (Categories is null)
        {
            error = "categories is required for the custom preset.";
            return false;
        }

        if (!Categories.TryResolve(out categories, out error))
        {
            return false;
        }

        return true;
    }

    private bool TryResolveDiagnostics(out bool enabled, out int maxEntries, out string error)
    {
        enabled = false;
        maxEntries = 0;
        if (ConflictDiagnostics?.Enabled is not { } configuredEnabled
            || ConflictDiagnostics.MaxLoggedItems is not { } configuredMax)
        {
            error = "conflictDiagnostics.enabled and maxLoggedItems are required.";
            return false;
        }

        if (configuredMax is < 0 or > MaximumConflictLogEntries)
        {
            error = $"conflictDiagnostics.maxLoggedItems must be between 0 and {MaximumConflictLogEntries}.";
            return false;
        }

        enabled = configuredEnabled;
        maxEntries = configuredMax;
        error = string.Empty;
        return true;
    }

    private static bool TryResolveIdList(
        List<string>? source,
        string name,
        out List<string> normalized,
        out string error)
    {
        normalized = [];
        if (source is null)
        {
            error = $"{name} is required; use an empty array when no IDs are needed.";
            return false;
        }

        foreach (var value in source)
        {
            var candidate = value?.Trim();
            if (candidate is null || candidate.Length != 24 || !candidate.All(Uri.IsHexDigit))
            {
                error = $"{name} contains an invalid 24-character template ID: '{value}'.";
                return false;
            }

            normalized.Add(candidate.ToLowerInvariant());
        }

        normalized = normalized.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        error = string.Empty;
        return true;
    }

    private static CategorySelection AllCategories()
    {
        return new CategorySelection(true, true, true, true, true, true, true, true, true, true, true);
    }

    private static CategorySelection SupplyCategories()
    {
        return new CategorySelection(true, true, true, true, true, true, true, false, false, false, false);
    }
}
