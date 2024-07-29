<!--
#
#
###########################
#                         #
#  Saint @ Shardbyte.com  #
#                         #
###########################
# Author: Shardbyte
# License: MIT
#
-->
<div id="header" align="center">
  <img src="https://raw.githubusercontent.com/Shardbyte/Shardbyte/main/img/logo-shardbyte-master-light.webp" alt="logo-shardbyte" width="150"/>
</div>

---

## Overview

**FeatherLight** is a mod for SPT that makes multiple categories of items weightless, enhancing your gameplay experience by reducing encumbrance and allowing for greater mobility and agility in raids.

## Features

- **Weightless Items**: Multiple categories of items are set to zero weight.
- **Improved Mobility**: Move faster and carry more without the burden of weight.
- **Customizable**: Easily adjust which categories of items are weightless.

## Usage

After installing the FeatherLight mod, the following categories of items will be weightless by default:
    - Ammo
    - Throwables (Grenades)
    - Food
    - Drink
    - Medical Supplies

### Customization

To customize which categories of items are weightless:

1. Open the `config.jsonc` file located in the mod's directory.
2. Edit the configuration to include or exclude categories as needed.
3. Save your changes and restart SPT for the changes to take effect.

Example configuration:
```jsonc
{
// Make Ammo weightless
// Default: true
  "lightAmmo": {
    "enabled": true
},
// Make Throwables weightless
// Default: true
"lightThrowables": {
  "enabled": true
},
// Make Food weightless
// Default: true
"lightFood": {
  "enabled": true
},
// Make Drinks weightless
// Default: true
"lightDrink": {
  "enabled": true
},
// Make Medical Supplies weightless
// Default: true
"lightMeds": {
  "enabled": true
}
}
```

### Compatibility

**FeatherLight** is compatible with the latest version of SPT and works seamlessly with most other mods.
  - Supports: 3.9.0-3.9.x

### License

This project is licensed under the MIT License - see the LICENSE file for details.

Enjoy a lighter, faster Tarkov experience with FeatherLight!
