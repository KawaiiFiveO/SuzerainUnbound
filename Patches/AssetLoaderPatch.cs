using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace SuzerainUnbound;

/// <summary>
/// Redirects Addressables bundle loads to replacement files in the AssetMods folder, so
/// asset mods work without overwriting anything inside the game directory.
/// </summary>
/// <remarks>
/// Every asset bundle file load funnels through
/// <c>AssetBundle.LoadFromFileAsync_Internal</c>: the two public LoadFromFileAsync
/// overloads are 8-byte tail-call thunks into it, and nothing inlines it, so a single
/// prefix here covers them all. (The synchronous LoadFromFile overloads were stripped
/// from this build entirely.) Addressables arrives here from
/// AssetBundleResource.BeginOperation, which passes the bundle path alongside the
/// catalog's CRC for that bundle.
/// </remarks>
[HarmonyPatch]
public class CustomAssetLoaderPatch
{
    static MethodBase TargetMethod()
    {
        MethodInfo target = AccessTools.Method(
            typeof(AssetBundle),
            "LoadFromFileAsync_Internal",
            new[] { typeof(string), typeof(uint), typeof(ulong) });

        if (target == null)
        {
            throw new MissingMethodException(
                "UnityEngine.AssetBundle.LoadFromFileAsync_Internal(string, uint, ulong) was not found. The game's Unity version may have changed.");
        }

        return target;
    }

    static void Prefix(ref string path, ref uint crc)
    {
        string requested = path;
        try
        {
            if (string.IsNullOrEmpty(requested))
            {
                return;
            }

            string fileName = Path.GetFileName(requested);
            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            if (!AssetModIndex.TryGetOverride(fileName, out AssetOverride assetOverride))
            {
                return;
            }

            path = assetOverride.FilePath;

            // The catalog's CRC describes the vanilla bundle, so a replacement can never
            // match it. Clear it for this load only — bundles we don't redirect keep
            // whatever validation the catalog asked for.
            crc = 0;

            AssetModIndex.LogRedirect(fileName, assetOverride);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[CustomAssetLoader] Could not redirect '{requested}': {ex.Message}");
        }
    }
}
