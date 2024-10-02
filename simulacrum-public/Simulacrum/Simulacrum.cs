using System;
using BepInEx;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using Loadstone.Config;
using Simulacrum.API.Types;
using Simulacrum.Controllers;
using Simulacrum.Objects.Events;
using Simulacrum.Objects.StatusEffects;
using Simulacrum.Registries;
using Simulacrum.Utils;
using UnityEngine;

namespace Simulacrum;

[BepInDependency("LethalNetworkAPI")]
[BepInDependency("mrov.WeatherRegistry")]
[BepInDependency("com.adibtw.loadstone")]
[BepInDependency("AudioKnight.StarlancerAIFix")]
[BepInDependency("imabatby.lethallevelloader")]
[BepInDependency("LCSoundTool")]
[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
public class Simulacrum : BaseUnityPlugin
{
    public const string PLUGIN_GUID = "giosuel.Simulacrum";
    public const string PLUGIN_NAME = "Simulacrum";
    public const string PLUGIN_VERSION = "0.0.1";

    internal static Harmony Harmony;

    internal static ManualLogSource Log;

    internal static PlayerControllerB Player;

    internal static GameConfig GameConfig;

    internal static EventRegistry EventRegistry;
    internal static PowerupRegistry PowerupRegistry;
    internal static StatusEffectRegistry StatusEffectRegistry;

    internal static WaveController Waves;
    internal static EnvironmentController Environment;
    internal static PlayerController Players;
    internal static InterfaceController Interface;
    internal static ParcourController Parcour;
    internal static PropsController Props;
    internal static GameController Game;
    internal static SetupScene SetupScene;
    internal static GulagController Gulag;
    internal static EventController Events;
    internal static StatusEffectController StatusEffects;
    internal static PowerupController Powerups;

    internal static GameState State;

    private static GameObject simulacrumController;

    internal static bool IsInitialized;

    private void Awake()
    {
        Log = Logger;

        GameConfig = new GameConfig(Config);

        if (!SimAssets.Load()) return;

        PowerupRegistry = new PowerupRegistry();
        EventRegistry = new EventRegistry();
        StatusEffectRegistry = new StatusEffectRegistry();

        RegisterVanillaEvents();
        RegisterVanillaPowerups();
        RegisterVanillaStatusEffects();

        Harmony = new Harmony(PLUGIN_GUID);
        Harmony.PatchAll();

        LoadstoneConfig.SeedDisplayConfig.Value = LoadstoneConfig.SeedDisplayType.JustLog;

        IsInitialized = true;
        Log.LogInfo("[OK] Simulacrum is ready!");
    }

    internal static void Launch(PlayerControllerB player)
    {
        Player = player;

        if (!simulacrumController)
        {
            SimAssets.LoadReferences();
        }
        else
        {
            simulacrumController.SetActive(false);
            Destroy(simulacrumController);
        }

        State = new GameState();

        simulacrumController = new GameObject("SimulacrumController");
        Game = simulacrumController.AddComponent<GameController>();
        Environment = simulacrumController.AddComponent<EnvironmentController>();
        Players = simulacrumController.AddComponent<PlayerController>();
        Waves = simulacrumController.AddComponent<WaveController>();
        Props = simulacrumController.AddComponent<PropsController>();
        SetupScene = simulacrumController.AddComponent<SetupScene>();
        Gulag = simulacrumController.AddComponent<GulagController>();
        Interface = simulacrumController.AddComponent<InterfaceController>();
        Events = simulacrumController.AddComponent<EventController>();
        Powerups = simulacrumController.AddComponent<PowerupController>();
        StatusEffects = simulacrumController.AddComponent<StatusEffectController>();
        Parcour = simulacrumController.AddComponent<ParcourController>();

        Log.LogInfo("[OK] Simulacrum has started!");

        SetupScene.Launch();
    }

    private static void RegisterVanillaEvents()
    {
        EventRegistry.RegisterEvent<SurroundingDarkness>(new EventDefinition
        {
            Name = "Surrounding Darkness",
            IsMutuallyExclusive = false,
            AdvanceIsCancel = false,
            Weight = 1000
        });

        // EventRegistry.RegisterEvent<PufferInvasion>(new EventDefinition
        // {
        //     Name = "Puffer Invasion",
        //     IsMutuallyExclusive = false,
        //     AdvanceIsCancel = false,
        //     Weight = 1000
        // });
    }

    private static void RegisterVanillaStatusEffects()
    {
        StatusEffectRegistry.RegisterStatusEffect<Resistance>(new StatusEffectDefinition
        {
            Name = "Resistance",
            Bubble = SimAssets.StatusEffectResistance,
            Duration = 20f
        });

        StatusEffectRegistry.RegisterStatusEffect<Stealth>(new StatusEffectDefinition
        {
            Name = "Stealth",
            Bubble = SimAssets.StatusEffectStealth,
            Duration = 15f
        });

        StatusEffectRegistry.RegisterStatusEffect<Speed>(new StatusEffectDefinition
        {
            Name = "Speed",
            Bubble = SimAssets.StatusEffectSpeed,
            Duration = 7f
        });

        StatusEffectRegistry.RegisterStatusEffect<Slowness>(new StatusEffectDefinition
        {
            Name = "Slowness",
            Bubble = SimAssets.StatusEffectSlowness,
            Duration = 5f
        });

        StatusEffectRegistry.RegisterStatusEffect<Regeneration>(new StatusEffectDefinition
        {
            Name = "Regeneration",
            Bubble = SimAssets.StatusEffectRegeneration,
            Duration = 6f
        });

        StatusEffectRegistry.RegisterStatusEffect<Blindness>(new StatusEffectDefinition
        {
            Name = "Blindness",
            Bubble = SimAssets.StatusEffectBlindness,
            Duration = 8f
        });

        StatusEffectRegistry.RegisterStatusEffect<Onslaught>(new StatusEffectDefinition
        {
            Name = "Onslaught",
            Bubble = SimAssets.StatusEffectOnslaught,
            Duration = 8f
        });
    }

    private static void RegisterVanillaPowerups()
    {
        PowerupRegistry.RegisterPowerup(new PowerupDefinition
        {
            Name = "Regeneration",
            Prefab = SimAssets.PowerupHealth,
            OnConsume = () => StatusEffects.Apply("Regeneration"),
            LookingAtPlayer = false,
            Weight = 50
        });

        PowerupRegistry.RegisterPowerup(new PowerupDefinition
        {
            Name = "SpeedBoost",
            Prefab = SimAssets.PowerupSpeed,
            OnConsume = () => StatusEffects.Apply("Speed"),
            LookingAtPlayer = true,
            Weight = 50
        });

        PowerupRegistry.RegisterPowerup(new PowerupDefinition
        {
            Name = "Resistance",
            Prefab = SimAssets.PowerupResistance,
            OnConsume = () => StatusEffects.Apply("Resistance"),
            LookingAtPlayer = false,
            Weight = 50
        });

        PowerupRegistry.RegisterPowerup(new PowerupDefinition
        {
            Name = "Stealth",
            Prefab = SimAssets.PowerupStealth,
            OnConsume = () => StatusEffects.Apply("Stealth"),
            LookingAtPlayer = true,
            Weight = 50
        });

        PowerupRegistry.RegisterPowerup(new PowerupDefinition
        {
            Name = "Onslaught",
            Prefab = SimAssets.PowerupOnslaught,
            OnConsume = () => StatusEffects.Apply("Onslaught"),
            LookingAtPlayer = true,
            Weight = 50
        });
    }
}