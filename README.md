# FeatherLight

FeatherLight is an SPT server mod that makes selected item categories weightless. It changes item-template weights when the server starts, so the behavior applies consistently to newly generated and existing instances of those templates.

## Compatibility

- SPT 4.1.5
- Server mod; no BepInEx plugin is required

The DLL is built against SPT 4.1.5. Rebuild it against the matching server packages before using it with another SPT patch.

## Categories

The following categories are enabled by default:

- Loose ammunition
- Throwable weapons and grenades
- Food
- Drinks
- Medical supplies

Food and drinks are evaluated independently. Disabling drinks keeps drink items at their original weight even when food remains enabled.

## Configuration

Edit `SPT_Runtime/user/mods/Shardbyte-FeatherLight/config/config.json`, then restart the SPT server. Every option must contain an `enabled` Boolean value.

```json
{
  "lightAmmo": { "enabled": true },
  "lightThrowables": { "enabled": true },
  "lightFood": { "enabled": true },
  "lightDrink": { "enabled": true },
  "lightMeds": { "enabled": true }
}
```

FeatherLight logs how many templates it changed in each enabled category. Invalid or incomplete configuration is rejected before any item weights are modified.

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
