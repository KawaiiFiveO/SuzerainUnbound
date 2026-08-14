using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SuzerainUnbound;

/// <summary>
/// A single replacement bundle: where it lives, and which asset mod it came from.
/// </summary>
internal readonly struct AssetOverride
{
    internal readonly string FilePath;
    internal readonly string ModName;

    internal AssetOverride(string filePath, string modName)
    {
        FilePath = filePath;
        ModName = modName;
    }
}

/// <summary>
/// Indexes the AssetMods folder for the Custom Asset Loader patch. Asset mods replace
/// vanilla Addressables bundles under the exact same filenames, so the index is just a
/// filename -> replacement map.
/// </summary>
internal static class AssetModIndex
{
    private const string AssetModsFolderName = "AssetMods";
    private const string BundlePattern = "*.bundle";
    private const int MaxUnmatchedToList = 10;

    private static Dictionary<string, AssetOverride> _overrides = new(StringComparer.OrdinalIgnoreCase);
    private static Dictionary<string, int> _bundlesPerMod = new(StringComparer.OrdinalIgnoreCase);
    private static bool _loggedFirstRedirect = false;

    internal static int Count => _overrides.Count;
    internal static int ModCount => _bundlesPerMod.Count;
    internal static string RootPath { get; private set; }

    /// <summary>
    /// Scans for replacement bundles. Returns true when at least one was found, meaning
    /// it is worth applying the Harmony patch at all.
    /// </summary>
    internal static bool Build()
    {
        _overrides = new Dictionary<string, AssetOverride>(StringComparer.OrdinalIgnoreCase);
        _bundlesPerMod = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        RootPath = ResolveRoot();
        if (RootPath == null)
        {
            Plugin.Log.LogWarning("[CustomAssetLoader] Could not work out where the AssetMods folder should be.");
            return false;
        }

        if (!Directory.Exists(RootPath))
        {
            Plugin.Log.LogInfo($"[CustomAssetLoader] No AssetMods folder at '{RootPath}'. Nothing to load.");
            return false;
        }

        string[] files;
        try
        {
            // Recursive, so an asset mod can be dropped in exactly as it was downloaded
            // (most ship a Suzerain_Data/StreamingAssets/aa/... tree) without flattening.
            files = Directory.GetFiles(RootPath, BundlePattern, SearchOption.AllDirectories);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[CustomAssetLoader] Could not read '{RootPath}': {ex.Message}");
            return false;
        }

        if (files.Length == 0)
        {
            Plugin.Log.LogInfo($"[CustomAssetLoader] No .bundle files under '{RootPath}'. Nothing to load.");
            return false;
        }

        // Sorting makes load order (and therefore conflict resolution) deterministic and
        // match what the user sees in Explorer, rather than whatever order the OS returns.
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);

        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);
            string modName = GetModName(file);

            // Register the mod even if every one of its bundles loses a conflict, so it
            // still shows up in the breakdown (with a count of 0) rather than vanishing.
            if (!_bundlesPerMod.ContainsKey(modName))
            {
                _bundlesPerMod[modName] = 0;
            }

            if (_overrides.TryGetValue(fileName, out AssetOverride existing))
            {
                Plugin.Log.LogWarning($"[CustomAssetLoader] Two asset mods replace '{fileName}'. Keeping '{existing.ModName}', ignoring '{modName}'.");
                continue;
            }

            _overrides[fileName] = new AssetOverride(file, modName);
            _bundlesPerMod[modName]++;
        }

        WarnOnUnmatchedOverrides();
        return _overrides.Count > 0;
    }

    internal static bool TryGetOverride(string fileName, out AssetOverride assetOverride)
    {
        return _overrides.TryGetValue(fileName, out assetOverride);
    }

    /// <summary>
    /// Lists each asset mod and how many bundles it actually contributes. Called after the
    /// patch-applied line so the summary reads top-down.
    /// </summary>
    internal static void LogModBreakdown()
    {
        var modNames = new List<string>(_bundlesPerMod.Keys);
        modNames.Sort(StringComparer.OrdinalIgnoreCase);

        foreach (string modName in modNames)
        {
            Plugin.Log.LogInfo($"[CustomAssetLoader]   {modName} — {_bundlesPerMod[modName]} bundle(s)");
        }
    }

    /// <summary>
    /// Every redirect goes to the Debug log, which BepInEx gates behind the user's own log
    /// level — a full load can redirect hundreds of bundles and would otherwise flood the
    /// console. The first one is also logged at Info, so there is always visible proof the
    /// loader is actually working.
    /// </summary>
    internal static void LogRedirect(string fileName, AssetOverride assetOverride)
    {
        Plugin.Log.LogDebug($"[CustomAssetLoader] Redirected '{fileName}' from '{assetOverride.ModName}' -> '{assetOverride.FilePath}'.");

        if (!_loggedFirstRedirect)
        {
            _loggedFirstRedirect = true;
            Plugin.Log.LogInfo($"[CustomAssetLoader] Redirected '{fileName}' from '{assetOverride.ModName}', asset mods are live. View all redirects by setting log level to Debug.");
        }
    }

    /// <summary>
    /// The mod a replacement belongs to, i.e. its first folder under AssetMods.
    /// </summary>
    private static string GetModName(string filePath)
    {
        string relative = filePath.Substring(RootPath.Length)
                                  .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        int separator = relative.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
        if (separator < 0)
        {
            return "(AssetMods root)";
        }

        return relative.Substring(0, separator);
    }

    /// <summary>
    /// Warns about replacements whose filename matches nothing this game version ships.
    /// Those are never requested, so without this check the mod would simply do nothing
    /// with no explanation — the one silent failure mode this design has.
    /// </summary>
    private static void WarnOnUnmatchedOverrides()
    {
        HashSet<string> vanilla = GetVanillaBundleNames();
        if (vanilla == null)
        {
            return;
        }

        var unmatched = new List<string>();
        foreach (string fileName in _overrides.Keys)
        {
            if (!vanilla.Contains(fileName))
            {
                unmatched.Add(fileName);
            }
        }

        if (unmatched.Count == 0)
        {
            return;
        }

        Plugin.Log.LogWarning($"[CustomAssetLoader] {unmatched.Count} of {_overrides.Count} replacement bundle(s) match no bundle this version of Suzerain ships, so they will never load:");
        for (int i = 0; i < unmatched.Count; i++)
        {
            if (i >= MaxUnmatchedToList)
            {
                Plugin.Log.LogWarning($"[CustomAssetLoader]   ...and {unmatched.Count - MaxUnmatchedToList} more.");
                break;
            }

            Plugin.Log.LogWarning($"[CustomAssetLoader]   {unmatched[i]}");
        }
        Plugin.Log.LogWarning("[CustomAssetLoader] This usually means the asset mod was built for a different version of the game.");
    }

    private static HashSet<string> GetVanillaBundleNames()
    {
        string aa = ResolveVanillaBundleRoot();
        if (aa == null)
        {
            Plugin.Log.LogDebug("[CustomAssetLoader] Could not find the game's bundle folder; skipping validation.");
            return null;
        }

        try
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string file in Directory.GetFiles(aa, BundlePattern, SearchOption.AllDirectories))
            {
                names.Add(Path.GetFileName(file));
            }
            return names.Count > 0 ? names : null;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogDebug($"[CustomAssetLoader] Skipped validation, could not read '{aa}': {ex.Message}");
            return null;
        }
    }

    private static string ResolveVanillaBundleRoot()
    {
        // Prefer the managed path so we don't call into Unity this early in startup.
        string fromPaths = Path.Combine(Paths.GameRootPath, $"{Paths.ProcessName}_Data", "StreamingAssets", "aa");
        if (Directory.Exists(fromPaths))
        {
            return fromPaths;
        }

        try
        {
            string fromUnity = Path.Combine(Application.streamingAssetsPath, "aa");
            if (Directory.Exists(fromUnity))
            {
                return fromUnity;
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogDebug($"[CustomAssetLoader] Application.streamingAssetsPath unavailable: {ex.Message}");
        }

        return null;
    }

    private static string ResolveRoot()
    {
        string configured = Plugin.AssetModsPath.Value;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            try
            {
                return Normalize(Path.GetFullPath(configured));
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[CustomAssetLoader] AssetModsPath '{configured}' is not a usable path ({ex.Message}). Using the default location instead.");
            }
        }

        string pluginDir = null;
        try
        {
            pluginDir = Path.GetDirectoryName(typeof(AssetModIndex).Assembly.Location);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogDebug($"[CustomAssetLoader] Could not read the plugin's own location: {ex.Message}");
        }

        if (string.IsNullOrEmpty(pluginDir))
        {
            pluginDir = Paths.PluginPath;
        }

        if (string.IsNullOrEmpty(pluginDir))
        {
            return null;
        }

        return Normalize(Path.Combine(pluginDir, AssetModsFolderName));
    }

    private static string Normalize(string path)
    {
        return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
