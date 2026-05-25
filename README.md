# Artisan

A quality-of-life and gameplay enhancement mod for YAPYAP focused on improving multiplayer compatibility, performance, and customization.

Artisan brings together multiple improvements inspired by existing community mods while addressing limitations, bugs, and performance issues found in previous implementations.

## Why Artisan Exists

Artisan was created after discovering several useful community mods that introduced interesting features but also presented limitations, bugs, or multiplayer/performance issues.

Normally, contributing fixes directly to the original projects would be the preferred approach. However, the mod pages for the referenced projects did not provide public repositories or issue trackers (such as GitHub pages) where bugs or improvements could be reported or contributed.

Because of this, Artisan was developed as an independent project focused on improving, reimplementing, and expanding those ideas while maintaining compatibility, configurability, and performance.

This project is intended as a complement to community work and not as criticism of the original authors, whose mods served as inspiration.

## Features

### Monster Health Bars + Multiplayer Damage Indicators

Artisan adds health bars and floating damage indicators to monsters.

Unlike similar implementations, these indicators are synchronized properly for multiplayer and work for every player with Artisan installed.

Inspired by XiaohaiMod's HealthBar mod:

https://thunderstore.io/c/yapyap/p/XiaohaiMod/HealthBar/

Improvements made by Artisan:

- Fully functional in multiplayer
- Works for all players with Artisan installed

### Aero & Teleblast Damage

Adds damage to:

- Aero
- Teleblast

Inspired by ControllerAndHisFriends' AeroWithDamage:

https://thunderstore.io/c/yapyap/p/ControllerAndHisFriends/AeroWithDamage/

Artisan improvements:

- Fixes issues that could cause Aero to behave incorrectly
- Cleaner implementation
- Fully configurable
- Individual spell damage values can be adjusted or disabled

### Extra Inventory Slots

Adds additional inventory slots to increase carrying capacity.

Inspired by H4Mods' MoreInventorySlots:

https://thunderstore.io/c/yapyap/p/H4Mods/MoreInventorySlots/

Artisan improvements:

- Eliminates performance bottlenecks found in previous implementations
- Avoids lag spikes when:
  - switching selected inventory slots
  - changing held items
  - moving items between slots
- Improved inventory UI behavior:
	- Focuses on adding extra slots without breaking the UI, but it does hide the background decoration of the inventory when extra slots are enabled. This is a trade-off to maintain a cohesive UI while adding functionality.
	- Added shortcut keys for extra slots (4-8) to quickly switch between them
- Can be disabled entirely

## Configuration

Every Artisan feature is configurable.

You can:

- Enable or disable health bars
- Enable or disable damage indicators
- Enable or disable extra inventory slots
- Enable or disable Aero damage
- Enable or disable Teleblast damage
- Adjust feature-specific values

Configuration file:

```text
BepInEx/config/gamedroit.artisan.cfg
```

Changes can be made without modifying the mod itself.

## Goals

Artisan aims to provide:

- Better multiplayer support
- Higher stability
- Better performance
- Modular features
- Full configurability
- Compatibility-focused implementations

## Installation

1. Install BepInEx
2. Install Artisan
3. Launch the game once
4. Configure features if desired:

```text
BepInEx/config/gamedroit.artisan.cfg
```

## Credits

### Inspiration

Health Bar implementation inspired by:

- XiaohaiMod — HealthBar

Aero damage implementation inspired by:

- ControllerAndHisFriends — AeroWithDamage

Inventory slot implementation inspired by:

- H4Mods — MoreInventorySlots

### Author

Created by **Gamedroit**