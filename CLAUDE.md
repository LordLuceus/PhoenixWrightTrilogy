# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a screen reader accessibility mod for Phoenix Wright: Ace Attorney Trilogy using MelonLoader. The mod outputs game text (dialogue, menus, UI elements) to the clipboard for screen reader software to read aloud.

## Build Commands

```bash
# Build the mod (output goes to game's Mods folder automatically)
cd AccessibilityMod
dotnet build -c Release

# The post-build target copies AccessibilityMod.dll to $(GamePath)\Mods
```

## Key Configuration

- **Target Framework**: `net35` (must match MelonLoader runtime)
- **GamePath**: `C:\Program Files (x86)\Steam\steamapps\common\Phoenix Wright Ace Attorney Trilogy`
- **MelonLoader References**: Use `net35` folder, not `net6`

## Architecture

### Core Components

- **AccessibilityMod.Core.AccessibilityMod**: Main MelonMod entry point. Handles initialization, keyboard input (R=repeat, I=state, [/]=hotspots, etc.)
- **ClipboardManager**: Queue-based text output system with duplicate prevention. Uses `CoroutineRunner` to write to `GUIUtility.systemCopyBuffer`
- **Net35Extensions**: Polyfills for .NET 3.5 compatibility (`IsNullOrWhiteSpace`, etc.)

### Patches (Harmony)

All patches use `[HarmonyPostfix]` to hook into game methods:
- **DialoguePatches**: Hooks `messageBoardCtrl.arrow()` to capture complete dialogue when the advance arrow appears
- **MenuPatches**: Hooks `tanteiMenu`, `selectPlateCtrl` for detective menu and choice navigation
- **InvestigationPatches**: Hooks `inspectCtrl` for investigation mode cursor feedback
- **TrialPatches**: Hooks `lifeGaugeCtrl.ActionLifeGauge()` for health/penalty announcements
- **SaveLoadPatches**: Hooks `SaveLoadUICtrl`, `optionCtrl` for save/load and options menus

### Services

- **CharacterNameService**: Maps sprite IDs to character names. Each game (GS1, GS2, GS3) has different sprite indices
- **HotspotNavigator**: Parses `GSStatic.inspect_data_` to enable list-based navigation of examination points
- **AccessibilityState**: Tracks current game mode (Investigation, Trial, Menu)

## Important Patterns

### Harmony Patches
When adding new patches, verify method names exist in `./Decompiled/` first. The game's API differs from typical Unity conventions. Common issues:
- Method names are often lowercase (`arrow`, `board`, `name_plate`)
- Enum parameters must match exactly (e.g., `lifeGaugeCtrl.Gauge_State`)
- Private methods can be patched but require correct signatures

### Character Name Mapping
Speaker sprite IDs differ per game. GS3 uses `name_id_tbl` remapping. When names are wrong, check which game is active via `GSStatic.global_work_.title`.

### .NET 3.5 Limitations
- No `string.IsNullOrWhiteSpace` - use `Net35Extensions.IsNullOrWhiteSpace`
- No `StringBuilder.Clear()` - use `sb.Length = 0`
- No `string.Join(IEnumerable)` - use `.ToArray()` first

## Decompiled Reference

The `./Decompiled/` folder contains decompiled game code for reference. Key files:
- `messageBoardCtrl.cs` - Dialogue display system
- `inspectCtrl.cs` - Investigation cursor and hotspots
- `GSStatic.cs` - Global game state
- `lifeGaugeCtrl.cs` - Trial health gauge
- `tanteiMenu.cs`, `selectPlateCtrl.cs` - Menu systems
