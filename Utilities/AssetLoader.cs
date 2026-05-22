using System.Collections.Generic;
using UnityEngine;

namespace Artisan.Assets
{
    public static class AssetLoader
    {
        internal static Dictionary<string, AssetBundle> LoadedBundles = new Dictionary<string, AssetBundle>();

        public static AssetBundle LoadAssetBundle(string path)
        {
            if (LoadedBundles.TryGetValue(path, out AssetBundle bundle))
            {
                return bundle;
            }

            bundle = AssetBundle.LoadFromFile(path);
            LoadedBundles.Add(path, bundle);
            return bundle;
        }

        public static T LoadAsset<T>(string bundlePath, string assetName) where T : UnityEngine.Object
        {
            AssetBundle bundle = LoadAssetBundle(bundlePath);
            if (bundle == null)
            {
                ArtisanMod.Logger.LogError($"Failed to load AssetBundle from path: {bundlePath}");
                return null;
            }
            T asset = bundle.LoadAsset<T>(assetName);
            if (asset == null)
            {
                ArtisanMod.Logger.LogError($"Failed to load asset '{assetName}' from bundle '{bundlePath}'");
                return null;
            }
            return asset;
        }

        public static T InstantiatePrefab<T>(string bundlePath, string assetName) where T : Component
        {
            T prefab = LoadAsset<T>(bundlePath, assetName);
            if (prefab == null)
            {
                ArtisanMod.Logger.LogError($"Failed to load prefab '{assetName}' from bundle '{bundlePath}'");
                return null;
            }
            return GameObject.Instantiate(prefab);
        }

        public static void UnloadAssetBundle(string path, bool unloadAllLoadedObjects = false)
        {
            if (LoadedBundles.TryGetValue(path, out AssetBundle bundle))
            {
                bundle.Unload(unloadAllLoadedObjects);
                LoadedBundles.Remove(path);
            }
        }
    }
}

/*
public static class AssetLoader
{
    private static AssetBundle _bundle;

    public static GameObject AstralThornsVfx;

    public static void Load()
    {
        if (_bundle != null)
            return;

        string path = Path.Combine(
            Paths.PluginPath,
            "BetterYapper",
            "BetterYapper.assets"
        );

        _bundle = AssetBundle.LoadFromFile(path);

        if (_bundle == null)
        {
            ArtisanMod.Logger.LogError(
                "Failed to load AssetBundle"
            );

            return;
        }

        AstralThornsVfx =
            _bundle.LoadAsset<GameObject>(
                "VfxAstralThorns"
            );

        ArtisanMod.Logger.LogInfo(
            "Loaded AstralThornsVFX"
        );
    }
}*/