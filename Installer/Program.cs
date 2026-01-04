using Installer.Services;

namespace Installer;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("Phoenix Wright: Ace Attorney Trilogy Accessibility Mod Installer");
        Console.WriteLine("================================================================");
        Console.WriteLine();

        // Parse arguments
        var options = ParseArguments(args);

        if (options.ShowHelp)
        {
            PrintHelp();
            return 0;
        }

        var gamePathFinder = new GamePathFinder();
        var githubService = new GitHubReleaseService();
        var melonLoaderService = new MelonLoaderService();
        var modInstallerService = new ModInstallerService();

        string? tempDir = null;

        try
        {
            // Step 1: Find game path
            Console.WriteLine("Step 1: Finding game installation...");
            var gamePath = options.GamePath ?? gamePathFinder.FindGamePath();

            if (gamePath != null)
            {
                Console.WriteLine($"Found: {gamePath}");

                if (!options.NonInteractive)
                {
                    Console.Write("Is this correct? (Y/n): ");
                    var response = Console.ReadLine()?.Trim().ToLowerInvariant();
                    if (response == "n" || response == "no")
                    {
                        gamePath = null;
                    }
                }
            }

            if (gamePath == null)
            {
                if (options.NonInteractive)
                {
                    Console.WriteLine(
                        "Error: Game not found. Use --game-path to specify the location."
                    );
                    return 1;
                }

                Console.WriteLine("Game not found automatically.");
                Console.Write("Enter the game installation path: ");
                gamePath = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(gamePath))
                {
                    Console.WriteLine("Error: No path provided.");
                    return 1;
                }
            }

            // Validate the path
            if (!gamePathFinder.ValidateGamePath(gamePath))
            {
                Console.WriteLine($"Error: Invalid game path. PWAAT.exe not found in: {gamePath}");
                return 1;
            }

            Console.WriteLine();

            // Step 2: Download latest release
            Console.WriteLine("Step 2: Downloading latest mod release...");
            if (options.Prerelease)
            {
                Console.WriteLine(
                    "Fetching release information from GitHub (including prereleases)..."
                );
            }
            else
            {
                Console.WriteLine("Fetching release information from GitHub...");
            }

            var release = await githubService.GetLatestReleaseAsync(options.Prerelease);
            if (release == null)
            {
                Console.WriteLine("Error: Could not fetch release information from GitHub.");
                return 1;
            }

            var versionLabel = release.Prerelease
                ? $"{release.TagName} (prerelease)"
                : release.TagName;
            Console.WriteLine($"Latest version: {versionLabel}");

            var asset = githubService.FindModAsset(release);
            if (asset == null)
            {
                Console.WriteLine("Error: Could not find mod download in release.");
                return 1;
            }

            var sizeMb = asset.Size / (1024.0 * 1024.0);
            Console.WriteLine($"Downloading {asset.Name} ({sizeMb:F1} MB)...");

            var zipPath = Path.Combine(Path.GetTempPath(), asset.Name);
            var lastProgress = -1;

            await githubService.DownloadAssetAsync(
                asset,
                zipPath,
                progress =>
                {
                    // Only report at 25% intervals to avoid spam
                    if (progress >= lastProgress + 25 || progress == 100)
                    {
                        Console.WriteLine($"Progress: {progress}%");
                        lastProgress = progress;
                    }
                }
            );

            Console.WriteLine("Download complete.");
            Console.WriteLine();

            // Step 3: Extract release
            Console.WriteLine("Step 3: Extracting release...");
            tempDir = modInstallerService.ExtractRelease(zipPath, Console.WriteLine);

            var extractedRoot = modInstallerService.FindExtractedRoot(tempDir);
            if (extractedRoot == null)
            {
                Console.WriteLine("Error: Could not find mod files in release archive.");
                return 1;
            }

            Console.WriteLine("Extraction complete.");
            Console.WriteLine();

            // Step 4: Check MelonLoader
            if (!options.SkipMelonLoader)
            {
                Console.WriteLine("Step 4: Checking MelonLoader...");

                if (melonLoaderService.IsMelonLoaderInstalled(gamePath))
                {
                    Console.WriteLine("MelonLoader is already installed.");
                }
                else
                {
                    Console.WriteLine("MelonLoader is not installed.");

                    if (!options.NonInteractive)
                    {
                        Console.Write("Install MelonLoader now? (Y/n): ");
                        var response = Console.ReadLine()?.Trim().ToLowerInvariant();
                        if (response == "n" || response == "no")
                        {
                            Console.WriteLine("Skipping MelonLoader installation.");
                            Console.WriteLine("Note: The mod requires MelonLoader to function.");
                        }
                        else
                        {
                            await melonLoaderService.InstallMelonLoaderAsync(
                                gamePath,
                                Console.WriteLine
                            );
                        }
                    }
                    else
                    {
                        await melonLoaderService.InstallMelonLoaderAsync(
                            gamePath,
                            Console.WriteLine
                        );
                    }
                }

                Console.WriteLine();
            }

            // Step 5: Install mod files
            var stepNum = options.SkipMelonLoader ? 4 : 5;
            Console.WriteLine($"Step {stepNum}: Installing mod files...");
            modInstallerService.InstallMod(extractedRoot, gamePath, Console.WriteLine);
            Console.WriteLine("Mod files installed.");
            Console.WriteLine();

            // Success
            Console.WriteLine("Installation complete!");
            Console.WriteLine("The accessibility mod has been installed successfully.");
            Console.WriteLine("Launch the game to use the mod.");
            Console.WriteLine();

            // Clean up downloaded zip
            try
            {
                File.Delete(zipPath);
            }
            catch { }

            if (!options.NonInteractive)
            {
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"Error: {ex.Message}");

            if (!options.NonInteractive)
            {
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }

            return 1;
        }
        finally
        {
            // Clean up temp directory
            if (tempDir != null)
            {
                modInstallerService.Cleanup(tempDir);
            }

            githubService.Dispose();
        }
    }

    static Options ParseArguments(string[] args)
    {
        var options = new Options();

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg == "--help" || arg == "-h")
            {
                options.ShowHelp = true;
            }
            else if (arg == "--game-path" || arg == "-g")
            {
                if (i + 1 < args.Length)
                {
                    options.GamePath = args[++i];
                }
            }
            else if (arg == "--skip-melonloader" || arg == "-s")
            {
                options.SkipMelonLoader = true;
            }
            else if (arg == "--force" || arg == "-f")
            {
                options.Force = true;
            }
            else if (arg == "--non-interactive" || arg == "-n")
            {
                options.NonInteractive = true;
            }
            else if (arg == "--prerelease" || arg == "-p")
            {
                options.Prerelease = true;
            }
        }

        return options;
    }

    static void PrintHelp()
    {
        Console.WriteLine("Installs the Phoenix Wright: Ace Attorney Trilogy Accessibility Mod.");
        Console.WriteLine();
        Console.WriteLine("Usage: PWAATAccessibilityInstaller [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --game-path, -g <path>   Path to the game installation directory");
        Console.WriteLine("  --skip-melonloader, -s   Skip MelonLoader installation check");
        Console.WriteLine("  --force, -f              Overwrite existing files without prompting");
        Console.WriteLine("  --non-interactive, -n    Run without user prompts (for automation)");
        Console.WriteLine(
            "  --prerelease, -p         Include prerelease versions when downloading"
        );
        Console.WriteLine("  --help, -h               Show this help");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  PWAATAccessibilityInstaller");
        Console.WriteLine("  PWAATAccessibilityInstaller --game-path \"C:\\Games\\PWAAT\"");
        Console.WriteLine("  PWAATAccessibilityInstaller --non-interactive --force");
        Console.WriteLine("  PWAATAccessibilityInstaller --prerelease");
    }

    class Options
    {
        public bool ShowHelp { get; set; }
        public string? GamePath { get; set; }
        public bool SkipMelonLoader { get; set; }
        public bool Force { get; set; }
        public bool NonInteractive { get; set; }
        public bool Prerelease { get; set; }
    }
}
