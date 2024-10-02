using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DunGen;
using DunGen.Graph;
using LCSoundTool;
using UnityEngine;
using UnityEngine.Rendering;

namespace Simulacrum.Utils;

internal static class SimAssets
{
    internal static GameObject PowerupHealth;
    internal static GameObject PowerupOnslaught;
    internal static GameObject PowerupSpeed;
    internal static GameObject PowerupStealth;
    internal static GameObject PowerupResistance;
    internal static GameObject EntitySpawn;
    internal static GameObject EnvironmentSphere;
    internal static GameObject PlayerHologram;
    internal static GameObject ReviveBubble;
    internal static GameObject VoidEssence;

    internal static GameObject PathSegment;
    internal static GameObject ParcourExit;
    internal static GameObject ParcourStart;

    internal static GameObject GlitchPass;

    internal static GameObject DoorBlocker;

    internal static GameObject BrackenRoomRef;
    internal static GameObject PufferCloudRef;

    internal static AudioClip SurroundingDarknessSFX;
    internal static AudioClip ReviveBubbleSFX;
    internal static AudioClip ReviveBubbleWallSFX;
    internal static AudioClip AmbientSFX;

    internal static GameObject LoadoutUIObject;
    internal static GameObject MapUIObject;
    internal static GameObject VitalsUIObject;

    internal static Sprite ShovelSprite;
    internal static Sprite ShotgunSprite;
    internal static Sprite SkullSprite;

    internal static GameObject StatusEffectContainer;
    internal static GameObject StatusEffectStealth;
    internal static GameObject StatusEffectBlindness;
    internal static GameObject StatusEffectOnslaught;
    internal static GameObject StatusEffectResistance;
    internal static GameObject StatusEffectSlowness;
    internal static GameObject StatusEffectSpeed;
    internal static GameObject StatusEffectRegeneration;

    internal static VolumeProfile DefaultSkyProfile;
    internal static VolumeProfile DamageProfile;
    internal static VolumeProfile ParcourProfile;
    internal static VolumeProfile ParcourStartProfile;
    internal static VolumeProfile BlackoutVolume;
    internal static VolumeProfile WhiteoutVolume;

    internal static Texture2D SurroundingDarknessVignette;

    private static readonly Dictionary<string, Sprite> spriteCache = new();
    private static AssetBundle Assets;

    internal static bool Load()
    {
        if (!LoadAssets())
        {
            Simulacrum.Log.LogInfo("[PRELOAD] Failed to load one or more assets from assembly, aborting!");
            return false;
        }

        SurroundingDarknessSFX = SoundTool.GetAudioClip("giosuel-Simulacrum", "surrounding_darkness.wav");
        ReviveBubbleSFX = SoundTool.GetAudioClip("giosuel-Simulacrum", "reviving_bubble.mp3");
        AmbientSFX = SoundTool.GetAudioClip("giosuel-Simulacrum", "ambient.mp3");
        ReviveBubbleWallSFX = SoundTool.GetAudioClip("giosuel-Simulacrum", "reviving_bubble_wall.mp3");

        return true;
    }

    internal static void LoadReferences()
    {
        BrackenRoomRef = Resources.FindObjectsOfTypeAll<Tile>().First(tile => tile.name == "SmallRoom2").gameObject;
        PufferCloudRef = Resources.FindObjectsOfTypeAll<GameObject>().First(obj => obj.name == "PufferEnemySmokeContainer");
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
            LoadAsset(Assets, "Assets/Simulacrum/Prefabs/PowerupSpeed.prefab", out PowerupSpeed),
            LoadAsset(Assets, "Assets/Simulacrum/Prefabs/PowerupStealth.prefab", out PowerupStealth),
            LoadAsset(Assets, "Assets/Simulacrum/Prefabs/PowerupResistance.prefab", out PowerupResistance),
            LoadAsset(Assets, "Assets/Simulacrum/Prefabs/PowerupOnslaught.prefab", out PowerupOnslaught),
            LoadAsset(Assets, "Assets/Simulacrum/Prefabs/EntitySpawn.prefab", out EntitySpawn),
            LoadAsset(Assets, "Assets/Simulacrum/Prefabs/DoorBlocker.prefab", out DoorBlocker),
            LoadAsset(Assets, "Assets/Simulacrum/Prefabs/EnvironmentSphere.prefab", out EnvironmentSphere),
            LoadAsset(Assets, "Assets/Simulacrum/Prefabs/PlayerHologram.prefab", out PlayerHologram),
            LoadAsset(Assets, "Assets/Simulacrum/Prefabs/ReviveBubble.prefab", out ReviveBubble),
            LoadAsset(Assets, "Assets/Simulacrum/Prefabs/Null.prefab", out VoidEssence),
            LoadAsset(Assets, "Assets/Simulacrum/Prefabs/PathSegment.prefab", out PathSegment),
            LoadAsset(Assets, "Assets/Simulacrum/Prefabs/ParcourExit.prefab", out ParcourExit),
            LoadAsset(Assets, "Assets/Simulacrum/Prefabs/ParcourStart.prefab", out ParcourStart),
            LoadAsset(Assets, "Assets/Simulacrum/Prefabs/GlitchPass.prefab", out GlitchPass),
            LoadAsset(Assets, "Assets/Simulacrum/Prefabs/UI/LoadoutUI.prefab", out LoadoutUIObject),
            LoadAsset(Assets, "Assets/Simulacrum/Prefabs/UI/MapUI.prefab", out MapUIObject),
            LoadAsset(Assets, "Assets/Simulacrum/Prefabs/UI/VitalsUI.prefab", out VitalsUIObject),
            LoadAsset(Assets, "Assets/Simulacrum/Prefabs/UI/EffectContainer.prefab", out StatusEffectContainer),
            LoadAsset(Assets, "Assets/Simulacrum/Prefabs/UI/Effects/Stealth.prefab", out StatusEffectStealth),
            LoadAsset(Assets, "Assets/Simulacrum/Prefabs/UI/Effects/Blindness.prefab", out StatusEffectBlindness),
            LoadAsset(Assets, "Assets/Simulacrum/Prefabs/UI/Effects/Onslaught.prefab", out StatusEffectOnslaught),
            LoadAsset(Assets, "Assets/Simulacrum/Prefabs/UI/Effects/Resistance.prefab", out StatusEffectResistance),
            LoadAsset(Assets, "Assets/Simulacrum/Prefabs/UI/Effects/Slowness.prefab", out StatusEffectSlowness),
            LoadAsset(Assets, "Assets/Simulacrum/Prefabs/UI/Effects/Speed.prefab", out StatusEffectSpeed),
            LoadAsset(Assets, "Assets/Simulacrum/Prefabs/UI/Effects/Regeneration.prefab", out StatusEffectRegeneration),
            LoadAsset(Assets, "Assets/Simulacrum/Textures/darkness.png", out SurroundingDarknessVignette),
            LoadAsset(Assets, "Assets/Simulacrum/Volumes/Default.asset", out DefaultSkyProfile),
            LoadAsset(Assets, "Assets/Simulacrum/Volumes/Damage.asset", out DamageProfile),
            LoadAsset(Assets, "Assets/Simulacrum/Volumes/Parcour.asset", out ParcourProfile),
            LoadAsset(Assets, "Assets/Simulacrum/Volumes/ParcourStart.asset", out ParcourStartProfile),
            LoadAsset(Assets, "Assets/Simulacrum/Volumes/BlackoutVolume.asset", out BlackoutVolume),
            LoadAsset(Assets, "Assets/Simulacrum/Volumes/WhiteoutVolume.asset", out WhiteoutVolume),
            LoadAsset(Assets, "Assets/Simulacrum/Sprites/shovel.png", out ShovelSprite),
            LoadAsset(Assets, "Assets/Simulacrum/Sprites/shotgun.png", out ShotgunSprite),
            LoadAsset(Assets, "Assets/Simulacrum/Sprites/skull.png", out SkullSprite),
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