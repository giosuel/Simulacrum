using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Simulacrum.Utils;

internal static class SimAssets
{
    internal static GameObject PowerupHealth;
    internal static GameObject PowerupOnslaught;
    internal static GameObject EntitySpawn;

    private static readonly Dictionary<string, Sprite> spriteCache = new();
    private static AssetBundle Assets;

    internal static bool Load()
    {
        if (!LoadAssets())
        {
            Simulacrum.Log.LogInfo("[PRELOAD] Failed to load one or more assets from assembly, aborting!");
            return false;
        }

        return true;
    }

    internal static Sprite LoadSpriteFromResources(string spriteName)
    {
        if (spriteCache.TryGetValue(spriteName, out var sprite)) return sprite;

        var resourcePath = "Simulacrum.resources." + spriteName;
        using var resFilestream = LoadResource(resourcePath);
        if (resFilestream == null) return null;

        var memoryStream = new MemoryStream();
        resFilestream.CopyTo(memoryStream);
        var spriteBytes = memoryStream.ToArray();

        var texture = new Texture2D(2, 2);
        if (texture.LoadImage(spriteBytes))
        {
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
            spriteCache[spriteName] = sprite;
            return sprite;
        }

        return null;
    }

    private static bool LoadAssets()
    {
        using (var assetBundleStream = LoadResource("Simulacrum.resources.assets.simulacrum_assets"))
        {
            Assets = AssetBundle.LoadFromStream(assetBundleStream);
        }

        if (Assets == null)
        {
            Simulacrum.Log.LogError("[PRELOAD] Failed to load assets from assembly, aborting!");
            return false;
        }

        List<bool> loadResults =
        [
            LoadAsset(Assets, "Assets/Simulacrum/Prefabs/PowerupHealth.prefab", out PowerupHealth),
            LoadAsset(Assets, "Assets/Simulacrum/Prefabs/PowerupOnslaught.prefab", out PowerupOnslaught),
            LoadAsset(Assets, "Assets/Simulacrum/Prefabs/EntitySpawn.prefab", out EntitySpawn),
        ];

        return loadResults.All(result => result);
    }

    private static bool LoadAsset<T>(AssetBundle assets, string path, out T loadedObject) where T : Object
    {
        loadedObject = assets.LoadAsset<T>(path);
        if (!loadedObject)
        {
            Simulacrum.Log.LogError($"[PRELOAD] Failed to load '{path}' from assets.");
            return false;
        }

        return true;
    }

    private static Stream LoadResource(string name) => Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
}