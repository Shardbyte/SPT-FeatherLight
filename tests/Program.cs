using System.Text.Json;
using FeatherLight;

var failures = new List<string>();

Run("complete configuration is valid", () =>
{
    var config = Deserialize("""
        {
          "lightAmmo": { "enabled": true },
          "lightThrowables": { "enabled": false },
          "lightFood": { "enabled": true },
          "lightDrink": { "enabled": false },
          "lightMeds": { "enabled": true }
        }
        """);
    Assert(config.TryValidate(out _), "Expected complete configuration to be valid.");
});

Run("missing category is rejected", () =>
{
    var config = Deserialize("""
        {
          "lightAmmo": { "enabled": true },
          "lightThrowables": { "enabled": true },
          "lightFood": { "enabled": true },
          "lightDrink": { "enabled": true }
        }
        """);
    Assert(!config.TryValidate(out var error) && error.Contains("lightMeds"), "Expected lightMeds error.");
});

Run("missing enabled value is rejected", () =>
{
    var config = Deserialize("""
        {
          "lightAmmo": {},
          "lightThrowables": { "enabled": true },
          "lightFood": { "enabled": true },
          "lightDrink": { "enabled": true },
          "lightMeds": { "enabled": true }
        }
        """);
    Assert(!config.TryValidate(out var error) && error.Contains("lightAmmo"), "Expected lightAmmo error.");
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

FeatherLightConfig Deserialize(string json)
{
    return JsonSerializer.Deserialize<FeatherLightConfig>(json)
        ?? throw new InvalidOperationException("Configuration deserialized to null.");
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
