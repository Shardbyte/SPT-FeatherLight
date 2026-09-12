using System.Text.Json.Serialization;

namespace FeatherLight;

public sealed record FeatureToggle
{
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; init; }
}

public sealed record FeatherLightConfig
{
    [JsonPropertyName("lightAmmo")]
    public FeatureToggle? LightAmmo { get; init; }

    [JsonPropertyName("lightThrowables")]
    public FeatureToggle? LightThrowables { get; init; }

    [JsonPropertyName("lightFood")]
    public FeatureToggle? LightFood { get; init; }

    [JsonPropertyName("lightDrink")]
    public FeatureToggle? LightDrink { get; init; }

    [JsonPropertyName("lightMeds")]
    public FeatureToggle? LightMeds { get; init; }

    public bool TryValidate(out string error)
    {
        var missingOptions = new List<string>();
        if (LightAmmo?.Enabled is null)
        {
            missingOptions.Add("lightAmmo");
        }

        if (LightThrowables?.Enabled is null)
        {
            missingOptions.Add("lightThrowables");
        }

        if (LightFood?.Enabled is null)
        {
            missingOptions.Add("lightFood");
        }

        if (LightDrink?.Enabled is null)
        {
            missingOptions.Add("lightDrink");
        }

        if (LightMeds?.Enabled is null)
        {
            missingOptions.Add("lightMeds");
        }

        error = missingOptions.Count == 0
            ? string.Empty
            : $"Missing or null configuration options: {string.Join(", ", missingOptions)}";
        return missingOptions.Count == 0;
    }
}
