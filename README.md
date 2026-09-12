# FeatherLight

FeatherLight is an SPT server mod that reduces selected item-template weights by a percentage or assigns a definitive weight. The behavior applies consistently to newly generated and existing instances of those templates.

## Compatibility

- SPT 4.1.5
- Server mod; no BepInEx plugin is required

The DLL is built against SPT 4.1.5. Rebuild it against the matching server packages before using it with another SPT patch.

## Presets

- `custom`: uses the configured weight rule and individual category toggles.
- `casual`: reduces every built-in category by 50%.
- `reducedSupplies`: reduces ammunition, ammo boxes, magazines, throwables, food, drinks, and meds by 75%; equipment is unchanged.
- `fullyWeightless`: assigns 0 kg to every built-in category.

Named presets replace the values under `weight` and `categories`. Custom included and excluded IDs still apply to every preset. The default `custom` configuration preserves the original FeatherLight behavior: loose ammunition, throwables, food, drinks, and meds become weightless, while the six new categories remain unchanged.

## Weight rules

For percentage mode, set `mode` to `percentage` and `percentageReduction` from 0 through 100. A value of 40 makes a 1 kg item weigh 0.6 kg.

For an exact result, set `mode` to `definitive` and set `definitiveWeightKg` to zero or any positive weight. Only the value for the active mode is used.

Built-in toggles are available for loose ammunition, ammo boxes, magazines, throwables, food, drinks, medical items, body armor, weapons, tactical or armored rigs, and backpacks. Specific categories are evaluated before broad ancestors so, for example, disabling throwables is respected when weapons are enabled.

Food and drinks are evaluated independently. Disabling drinks keeps drink items at their original weight even when food remains enabled.

## Configuration

Edit `SPT_Runtime/user/mods/Shardbyte-FeatherLight/config/config.json`, then restart the SPT server.

```json
{
  "preset": "custom",
  "weight": {
    "mode": "percentage",
    "percentageReduction": 100,
    "definitiveWeightKg": 0
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
```

IDs must be 24-character hexadecimal SPT template IDs. Explicit inclusions add templates even when their built-in toggle is disabled. Exclusions always take priority over presets, toggles, and inclusions.

FeatherLight captures an early weight baseline, applies its changes later in server startup, and reports selected templates changed in between. Conflict output is bounded by `maxLoggedItems` from 0 through 200. FeatherLight calculates reductions from its baseline to prevent compounded percentages. A mod that changes weights after FeatherLight runs can still override the final result and cannot be diagnosed during startup.

Invalid or incomplete configuration is rejected before any item weights are modified.

## Install

Extract the release ZIP into the root of the SPT installation. Confirm this file exists before starting the server:

```text
SPT_Runtime/user/mods/Shardbyte-FeatherLight/FeatherLight.dll
```

The archive includes the complete install hierarchy and a `VERSION.txt` manifest. Remove any older `Shardbyte-FeatherLight` TypeScript installation before extracting this rebuilt edition so SPT cannot load both implementations.

## Build and package

```bash
dotnet build FeatherLight.csproj -c Release
dotnet run --project tests/FeatherLight.Tests.csproj -c Release
./package.sh
```

The packaging script validates the configuration, builds the project, and creates `artifacts/Shardbyte-FeatherLight-<version>.zip`.

## License

FeatherLight is available under the MIT License. See `LICENSE`.
