# Artisan

A quality-of-life and gameplay enhancement mod for YAPYAP focused on improving multiplayer compatibility, performance, and customization.

Artisan combines multiple gameplay improvements while addressing limitations, bugs, and performance issues found in previous implementations.

## Why Artisan Exists

Artisan was created after discovering useful community mods that introduced interesting features but also presented limitations, multiplayer issues, or performance problems.

Normally, contributing fixes directly to those projects would be the preferred approach. However, some referenced projects did not provide public repositories or issue trackers where bugs and improvements could be reported.

Because of this, Artisan was developed as an independent project focused on improving, reimplementing, and expanding those ideas while maintaining configurability, compatibility, and performance.

This project exists as a complement to community work and not as criticism of the original authors whose work served as inspiration.

## Features

### Monster Health Bars + Multiplayer Damage Indicators

Adds health bars and floating damage indicators to monsters.

Improvements:

* Proper multiplayer synchronization
* Visible to all players with Artisan installed
* More stable implementation

### Aero & Teleblast Damage

Adds configurable damage support for:

* Aero
* Teleblast

Features:

* Individual spell damage values
* Can be configured independently
* Can be disabled completely

### Extended Inventory Slots

Adds additional inventory slots to increase carrying capacity.

Features:

* Improved performance compared to previous implementations
* Avoids lag spikes when:

  * Switching selected inventory slots
  * Moving items between slots
  * Changing held items
* Added additional hotkeys:

  * Slots 4–8
* Improved inventory UI behavior
* Can be disabled completely

### Backpack Upgrade System

Adds optional progression-based inventory upgrades.

Players start with the default inventory size and can purchase additional inventory slots during the lobby phase.

Features:

* Individual progression per player
* Shared room gold economy
* Configurable base upgrade price
* Configurable price multiplier
* Dedicated upgrade station in the lobby
* Multiplayer-safe synchronization
* Locked slot visual indicators
* Locked slot interaction protection

Locked slots include:

* Visual overlay
* Tele-lock themed lock icon
* Pickup validation
* Hotkey protection
* Scroll selection protection
* Drag/drop protection

## Configuration

Every Artisan feature is configurable.

Available options include:

### Gameplay

* Enable/disable health bars
* Enable/disable damage indicators
* Enable/disable Aero damage
* Enable/disable Teleblast damage
* Enable/disable extra inventory slots
* Enable/disable backpack upgrades

### Backpack Upgrade Settings

* Base upgrade price
* Upgrade price multiplier

Configuration file:

```text
BepInEx/config/gamedroit.artisan.cfg
```

Changes can be made without modifying the mod itself.

## Goals

Artisan aims to provide:

* Better multiplayer support
* Higher stability
* Better performance
* Modular features
* Full configurability
* Compatibility-focused implementations

## Installation

1. Install BepInEx
2. Install Artisan
3. Launch the game once
4. Configure features if desired

Configuration path:

```text
BepInEx/config/gamedroit.artisan.cfg
```

## Credits

### Inspiration

- Health Bar: https://thunderstore.io/c/yapyap/p/XiaohaiMod/HealthBar/
- Aero With Damage: https://thunderstore.io/c/yapyap/p/ControllerAndHisFriends/AeroWithDamage/
- More Inventory Slots: https://thunderstore.io/c/yapyap/p/H4Mods/MoreInventorySlots/

### Author

Created by **Gamedroit** also known as **Hadaward**
