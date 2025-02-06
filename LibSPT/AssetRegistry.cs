using System.IO;
using System.Reflection;
using UnityEngine;

namespace HollywoodFX
{
    internal static class AssetRegistry
    {
        public static AssetBundle ImpactsBundle;

        public static void LoadBundles()
        {
            var bundleDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var impactsBundlePath = $"{bundleDirectory}\\hfx impacts";
            Plugin.Log.LogInfo($"Loading Impacts Bundle: {impactsBundlePath}");
            ImpactsBundle = AssetBundle.LoadFromFile(impactsBundlePath);
        }
    }
}
