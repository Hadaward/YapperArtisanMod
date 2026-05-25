# Changelog

## 1.0.2

### Added
- Added configurable backpack inventory slot upgrades.
- Added per-player inventory progression system.
- Added lobby upgrade chest for purchasing additional inventory slots.
- Added support for shared gold economy when purchasing inventory upgrades.
- Added locked slot visual overlays with tele-lock mesh rendering.
- Added automatic slot locking/unlocking synchronization.
- Added multiplayer-safe inventory upgrade networking.
- Added localized upgrade text support.
- Added English and Brazilian Portuguese localization support.
- Added shopkeeper purchase feedback integration.
- Added purchase sound effects and NPC reactions after successful upgrades.
- Added configurable upgrade pricing:
  - Base slot upgrade price
  - Upgrade price multiplier
- Added support for disabling the upgrade system entirely while keeping all slots unlocked.
- Added inventory full handling for potion ingredients and custom interactables.
- Added runtime UI refresh for slot overlays and upgrade states.

### Changed
- Completely refactored extended inventory UI layout system.
- Reworked slot positioning to use deterministic absolute positioning.
- Improved inventory frame alignment and spacing.
- Improved offhand slot alignment and scaling.
- Improved locked slot visuals and overlay rendering.
- Improved inventory interaction validation.
- Improved multiplayer synchronization for inventory upgrades.
- Improved tooltip behavior for locked/full inventory states.

### Fixed
- Fixed players being able to store items in locked inventory slots.
- Fixed mouse scroll selecting locked slots.
- Fixed locked slots accepting drag/drop interactions.
- Fixed inventory pickup validation when extra slots are locked.
- Fixed inventory full tooltip inconsistencies.
- Fixed potion ingredient tooltip not displaying "Inventory Full".
- Fixed upgrade chest not respawning after returning to the title screen.
- Fixed locked slot overlays not refreshing immediately after upgrades.
- Fixed several inventory UI alignment issues.
- Fixed frame positioning inconsistencies between regular slots and offhand slot.
- Fixed purchase SFX playing when upgrades failed.
- Fixed multiplayer purchase confirmation behavior.
- Fixed several UI layering and overlay positioning issues.

### Technical
- Added runtime mesh-to-sprite rendering for tele-lock slot icons.
- Added server-authoritative inventory upgrade purchases.
- Added custom Mirror networking handlers for upgrade synchronization.
- Added helper utilities for locked slot validation and inventory state checks.
- Added automatic UI rebuild and refresh logic for inventory overlays.