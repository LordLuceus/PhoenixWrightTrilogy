using System.IO.Compression;

namespace Installer.Services;

public class ModInstallerService
{
    private const string ModsFolder = "Mods";
    private const string UserDataFolder = "UserData";
    private const string AccessibilityModFolder = "AccessibilityMod";
    private const string ModDll = "AccessibilityMod.dll";
    private const string MelonAccessibilityLibDll = "MelonAccessibilityLib.dll";
    private const string UniversalSpeechDll = "UniversalSpeech.dll";
    private const string NvdaClientDll = "nvdaControllerClient.dll";
    private const string DataFolder = "Data";

    /// <summary>
    /// Extracts the mod release zip to a temporary directory.
    /// </summary>
    public string ExtractRelease(string zipPath, Action<string>? statusCallback = null)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"PWAATMod_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        statusCallback?.Invoke("Extracting release archive...");

        try
        {
            ZipFile.ExtractToDirectory(zipPath, tempDir);
        }
        catch (Exception ex)
        {
            // Clean up on failure
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch { }
            throw new Exception($"Failed to extract release archive: {ex.Message}", ex);
        }

        return tempDir;
    }

    /// <summary>
    /// Finds the root folder of the extracted release.
    /// </summary>
    public string? FindExtractedRoot(string extractPath)
    {
        // The zip contains a root folder like "PhoenixWrightTrilogyAccessibilityMod-v1.0.0"
        var subdirs = Directory.GetDirectories(extractPath);
        if (subdirs.Length == 1)
        {
            return subdirs[0];
        }

        // Fallback: check if files are directly in the extract path
        if (File.Exists(Path.Combine(extractPath, ModDll)))
        {
            return extractPath;
        }

        return null;
    }

    /// <summary>
    /// Installs the mod files to the game directory.
    /// </summary>
    public void InstallMod(
        string extractedRoot,
        string gamePath,
        Action<string>? statusCallback = null
    )
    {
        // Create necessary directories
        var modsPath = Path.Combine(gamePath, ModsFolder);
        var userDataPath = Path.Combine(gamePath, UserDataFolder, AccessibilityModFolder);

        Directory.CreateDirectory(modsPath);
        Directory.CreateDirectory(userDataPath);

        // Copy AccessibilityMod.dll to Mods folder
        var modDllSource = Path.Combine(extractedRoot, ModDll);
        var modDllDest = Path.Combine(modsPath, ModDll);
        CopyFile(modDllSource, modDllDest, statusCallback);

        // Copy MelonAccessibilityLib.dll to game root
        var melonLibSource = Path.Combine(extractedRoot, MelonAccessibilityLibDll);
        var melonLibDest = Path.Combine(gamePath, MelonAccessibilityLibDll);
        CopyFile(melonLibSource, melonLibDest, statusCallback);

        // Copy UniversalSpeech.dll to game root
        var universalSpeechSource = Path.Combine(extractedRoot, UniversalSpeechDll);
        var universalSpeechDest = Path.Combine(gamePath, UniversalSpeechDll);
        CopyFile(universalSpeechSource, universalSpeechDest, statusCallback);

        // Copy nvdaControllerClient.dll to game root
        var nvdaSource = Path.Combine(extractedRoot, NvdaClientDll);
        var nvdaDest = Path.Combine(gamePath, NvdaClientDll);
        CopyFile(nvdaSource, nvdaDest, statusCallback);

        // Copy Data folder contents to UserData/AccessibilityMod
        var dataSource = Path.Combine(extractedRoot, DataFolder);
        if (Directory.Exists(dataSource))
        {
            statusCallback?.Invoke("Copying localization files...");
            CopyDirectory(dataSource, userDataPath);
        }
    }

    private void CopyFile(string source, string destination, Action<string>? statusCallback = null)
    {
        if (!File.Exists(source))
        {
            return; // Skip if source doesn't exist
        }

        var fileName = Path.GetFileName(source);
        statusCallback?.Invoke($"Copying {fileName}...");

        File.Copy(source, destination, overwrite: true);
    }

    private void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        // Copy files
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite: true);
        }

        // Copy subdirectories recursively
        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
            CopyDirectory(subDir, destSubDir);
        }
    }

    /// <summary>
    /// Cleans up temporary files.
    /// </summary>
    public void Cleanup(string tempPath)
    {
        try
        {
            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
