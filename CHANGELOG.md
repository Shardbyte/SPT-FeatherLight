# Changelog

## 2.1.1 - 2026-09-13

- Run the configuration regression tests before creating a release archive.

## 2.1.0

- Added percentage reduction and definitive-weight modes.
- Added separate ammo-box, magazine, armor, weapon, rig, and backpack toggles.
- Added custom included and excluded template/category IDs, with exclusions taking priority.
- Added casual, reduced-supplies, fully-weightless, and custom presets.
- Added bounded conflict diagnostics using a baseline captured before later mod changes.

## 2.0.0

- Rebuilt FeatherLight as a native C# server mod for SPT 4.1.5.
- Preserved independent toggles for ammunition, throwables, food, drinks, and medical supplies.
- Added safe configuration validation and per-category change counts.
- Reduced startup work to one pass over the item-template database.
- Added directly extractable Shardbyte-prefixed packaging and version metadata.

## 1.0.0

- Original TypeScript release for SPT 3.9.x.
