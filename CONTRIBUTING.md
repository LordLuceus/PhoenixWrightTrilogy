# Contributing to Phoenix Wright Accessibility Mod

Thank you for your interest in making Phoenix Wright: Ace Attorney Trilogy more accessible. This guide will help you get started.

## Development Setup

### Requirements

- Visual Studio 2019+, Visual Studio Code, or another compatible IDE with .NET support
- .NET Framework 3.5 targeting pack
- Phoenix Wright: Ace Attorney Trilogy (Steam)
- MelonLoader v0.7.1 installed in the game directory

### Building

```bash
cd AccessibilityMod
dotnet build -c Release
```

The build output is automatically copied to the game's `Mods` folder.

### Testing

1. Build the mod
2. Launch the game through Steam
3. Check `[Game Directory]/MelonLoader/Latest.log` for debug output

Use `AccessibilityMod.Logger.Msg()` to add debug logging.

### Decompiling the Game

To create Harmony patches, you'll need to inspect the game's code. Use [ILSpy](https://github.com/icsharpcode/ILSpy), [dnSpy](https://github.com/dnSpy/dnSpy), or a similar .NET decompiler to examine the game assemblies in `[Game Directory]/PWAAT_Data/Managed/`. Key assemblies:

- `Assembly-CSharp.dll` - Main game code (dialogue, menus, minigames)

## Project Structure

```
AccessibilityMod/
├── Core/                   # Main mod entry point and managers
├── Patches/                # Harmony patches for game hooks
├── Services/               # Localization, character names, etc.
├── Navigators/             # Mode-specific navigation (hotspots, evidence, etc.)
└── Data/                   # Default configuration files
```

## Code Guidelines

### .NET 3.5 Compatibility

The mod targets .NET 3.5 to match MelonLoader. Avoid modern C# features:

- Use `Net35Extensions.IsNullOrWhiteSpace()` instead of `string.IsNullOrWhiteSpace()`
- Use `sb.Length = 0` instead of `StringBuilder.Clear()`
- Use `.ToArray()` before `string.Join()` on enumerables

### Harmony Patches

- Verify method names exist in the decompiled code before patching
- Game methods often use lowercase names (`arrow`, `board`, `name_plate`)
- Use `[HarmonyPostfix]` unless you need to intercept before the original runs

### Adding New Patches

1. Find the target method in the decompiled code
2. Create a patch class in `Patches/`
3. Use `[HarmonyPatch]` attributes with exact method signatures
4. Test thoroughly - incorrect patches can crash the game

### Navigator Pattern

For new game modes, follow the existing navigator structure:

```csharp
public static class MyNavigator {
    private static List<ItemInfo> _items;
    private static int _currentIndex;

    public static bool IsActive() { /* check game state */ }
    public static void Update() { /* called each frame */ }
    public static void NavigateNext() { /* [ key */ }
    public static void NavigatePrevious() { /* ] key */ }
    public static void AnnounceHint() { /* H key */ }
}
```

Register the navigator in `InputManager` with appropriate priority.

### Localization

- Use `L.Get("key")` for all user-facing strings
- Add new strings to `Data/en/strings.json`
- Use placeholders like `{0}`, `{1}` for dynamic values

## Submitting Changes

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Make your changes
4. Test with the game
5. Submit a pull request

### Commit Messages

- Use clear, descriptive commit messages
- Start with a verb (Add, Fix, Update, Remove)
- Reference issues if applicable

### Pull Request Guidelines

- Describe what the change does and why
- Include testing steps
- Keep changes focused - one feature or fix per PR

## Reporting Issues

When reporting bugs, include:

- Game version and episode/case where the issue occurs
- Steps to reproduce
- Expected vs actual behavior
- Relevant log output from `MelonLoader/Latest.log`

## Questions?

Open an issue for questions about contributing or the codebase.
