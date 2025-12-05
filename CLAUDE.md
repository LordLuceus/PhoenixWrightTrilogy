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

- **AccessibilityMod.Core.AccessibilityMod**: Main MelonMod entry point. Handles initialization and keyboard input
- **ClipboardManager**: Queue-based text output system with duplicate prevention. Uses `CoroutineRunner` to write to `GUIUtility.systemCopyBuffer`
- **Net35Extensions**: Polyfills for .NET 3.5 compatibility (`IsNullOrWhiteSpace`, etc.)
- **TextCleaner**: Strips formatting tags and normalizes text for screen reader output

### Keyboard Shortcuts

| Key | Context | Action |
|-----|---------|--------|
| R | Global (except vase puzzle) | Repeat last output |
| I | Global | Announce current state/context |
| [ / ] | Investigation | Navigate hotspots |
| U | Investigation | Jump to next unexamined hotspot |
| H | Investigation | List all hotspots |
| [ / ] | Pointing mode | Navigate target areas |
| H | Pointing mode | List all target areas |
| [ / ] | Luminol mode | Navigate blood evidence |
| [ / ] | 3D Evidence | Navigate examination points |
| [ / ] | Fingerprint mode | Navigate fingerprint locations |
| H | Fingerprint mode | Get hint for current phase |
| [ / ] | Video tape mode | Navigate to targets when paused |
| H | Video tape mode | Get hint |
| H | Vase puzzle | Get hint for current step |
| H | Trial (not pointing) | Announce life gauge |

### Patches (Harmony)

All patches use `[HarmonyPostfix]` to hook into game methods:
- **DialoguePatches**: Hooks `messageBoardCtrl.arrow()` to capture complete dialogue when the advance arrow appears
- **MenuPatches**: Hooks `tanteiMenu`, `selectPlateCtrl` for detective menu and choice navigation
- **InvestigationPatches**: Hooks `inspectCtrl` for investigation mode cursor feedback
- **TrialPatches**: Hooks `lifeGaugeCtrl.ActionLifeGauge()` for health/penalty announcements
- **SaveLoadPatches**: Hooks `SaveLoadUICtrl`, `optionCtrl` for save/load and options menus
- **CourtRecordPatches**: Hooks court record navigation for evidence/profiles
- **CutscenePatches**: Handles cutscene dialogue capture
- **LuminolPatches**: Hooks luminol spray minigame for blood detection
- **VasePuzzlePatches**: Hooks vase puzzle state for accessibility hints
- **FingerprintPatches**: Hooks fingerprint dusting minigame
- **ScienceInvestigationPatches**: Hooks science investigation minigames

### Services

- **CharacterNameService**: Maps sprite IDs to character names. Each game (GS1, GS2, GS3) has different sprite indices
- **HotspotNavigator**: Parses `GSStatic.inspect_data_` to enable list-based navigation of examination points
- **AccessibilityState**: Tracks current game mode (Investigation, Trial, Menu, Luminol, VasePuzzle, Fingerprint, VideoTape, Pointing, 3DEvidence)
- **PointingNavigator**: Navigation for court map and pointing minigames
- **LuminolNavigator**: Blood evidence detection in luminol spray mode
- **VasePuzzleNavigator**: Step-by-step hints for the vase puzzle minigame
- **FingerprintNavigator**: Fingerprint dusting minigame navigation
- **VideoTapeNavigator**: Video tape examination mode (scrubbing/targeting)
- **Evidence3DNavigator**: 3D evidence examination hotspot navigation
- **EvidenceDetailService**: Extracts evidence/profile details from court record

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
