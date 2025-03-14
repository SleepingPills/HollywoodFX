using System.IO;
using System.Reflection;
using UnityEngine;

namespace HollywoodFX;

internal static class AssetRegistry
{
    // ReSharper disable once NotAccessedField.Global
    public static AssetBundle ShaderBundle;
    public static AssetBundle AssetBundle;

    public static void LoadBundles()
    {
        var bundleDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        
        var shaderBundlePath = $"{bundleDirectory}\\hollywoodfx_shaders";
        Plugin.Log.LogInfo($"Loading Shader Bundle: {shaderBundlePath}");
        ShaderBundle = AssetBundle.LoadFromFile(shaderBundlePath);

        var assetBundlePath = $"{bundleDirectory}\\hollywoodfx";
        Plugin.Log.LogInfo($"Loading Impacts Bundle: {assetBundlePath}");
        AssetBundle = AssetBundle.LoadFromFile(assetBundlePath);
    }
}