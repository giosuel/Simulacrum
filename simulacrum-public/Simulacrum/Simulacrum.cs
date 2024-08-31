using System;
using System.Security.Cryptography;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using Loadstone.Config;
using Simulacrum.Utils;
using UnityEngine;

namespace Simulacrum;

[BepInDependency("LethalNetworkAPI")]
[BepInDependency("mrov.WeatherRegistry")]
[BepInDependency("com.adibtw.loadstone")]
[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
public class Simulacrum : BaseUnityPlugin
{
    public const string PLUGIN_GUID = "giosuel.Simulacrum";
    public const string PLUGIN_NAME = "Simulacrum";
    public const string PLUGIN_VERSION = "0.0.1";

    internal static ManualLogSource Log;
    internal static ConfigFile ConfigFile;

    internal static PlayerControllerB Player;

    internal static event Action OnShipLanded;

    internal static WaveController Waves;
    internal static EnvironmentController Environment;
    internal static PlayerController Players;
    internal static PropsController Props;
    internal static GameController Game;
    internal static SetupScene SetupScene;

    private static GameObject simulacrumController;

    private void Awake()
    {
        Log = Logger;
        ConfigFile = Config;

        if (!SimAssets.Load()) return;

        new Harmony(PLUGIN_GUID).PatchAll();
        LoadstoneConfig.SeedDisplayConfig.Value = LoadstoneConfig.SeedDisplayType.JustLog;

        Log.LogInfo("[OK] Simulacrum is ready!");
    }

    internal static void Launch(PlayerControllerB player)
    {
        Player = player;

        if (simulacrumController) Destroy(simulacrumController);

        simulacrumController = new GameObject("SimulacrumController");
        Players = simulacrumController.AddComponent<PlayerController>();
        Waves = simulacrumController.AddComponent<WaveController>();
        Environment = simulacrumController.AddComponent<EnvironmentController>();
        Props = simulacrumController.AddComponent<PropsController>();
        SetupScene = simulacrumController.AddComponent<SetupScene>();
        Game = simulacrumController.AddComponent<GameController>();

        Log.LogInfo("[OK] Simulacrum has started!");

        SetupScene.Launch();
    }

    internal static void OnShipLand() => OnShipLanded?.Invoke();
}