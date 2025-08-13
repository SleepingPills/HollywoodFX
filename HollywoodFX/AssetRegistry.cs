using System.IO;
using System.Reflection;
using UnityEngine;

namespace HollywoodFX;

internal static class AssetRegistry
{
    // ReSharper disable once NotAccessedField.Global
    public static AssetBundle AssetBundle;

    public static void LoadBundles()
    {
        var bundleDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        
        var assetBundlePath = $"{bundleDirectory}\\hollywoodfx";
        Plugin.Log.LogInfo($"Loading Impacts Bundle: {assetBundlePath}");
        AssetBundle = AssetBundle.LoadFromFile(assetBundlePath);
    }
}