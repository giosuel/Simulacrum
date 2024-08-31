using System.Linq;
using LethalNetworkAPI;
using Simulacrum.Network;
using Simulacrum.Utils;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using WeatherRegistry;
using Random = UnityEngine.Random;

namespace Simulacrum;

internal class EnvironmentController : MonoBehaviour
{
    internal SelectableLevel[] Levels = [];

    private string previouslyLoadedScene;
    private string currentLoadedScene;

    // Whether the teleporters are currently disabled
    internal bool TeleportersDisabled { get; private set; }

    private Sprite teleportDefaultSprite;

    /*
     * State to make sure the dungeon and outdoor scene is only refreshed once per iteration.
     */
    private int environmentIteration = 1;
    private int currentIndoorEnvironmentIteration;
    private int currentOutdoorEnvironmentIteration;

    private readonly LNetworkEvent onPlayerEntersBuilding = LNetworkEvent.Connect("PlayerEnter");
    private readonly LNetworkEvent onPlayerLeavesBuilding = LNetworkEvent.Connect("PlayerLeave");

    private readonly LNetworkMessage<WeatherChangeMessage> onWeatherChanged = LNetworkMessage<WeatherChangeMessage>.Connect(
        "WeatherChange"
    );

    private readonly LevelWeatherType[] possibleWeathers =
    [
        LevelWeatherType.Stormy,
        // LevelWeatherType.Foggy,
        LevelWeatherType.Eclipsed,
        LevelWeatherType.Rainy,
    ];

    private void Awake()
    {
        Levels = Resources.FindObjectsOfTypeAll<SelectableLevel>()
            .Where(level => level.sceneName != "CompanyBuilding" && level.sceneName != "Level12Liquidation" &&
                            level.sceneName != "IntroScene2")
            .ToArray();

        NetworkManager.Singleton.SceneManager.OnUnloadComplete += OnSceneUnloaded;
        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoaded;

        if (NetworkManager.Singleton.IsHost)
        {
            onPlayerEntersBuilding.OnServerReceived += OnPlayerEnterBuildingServer;
            onPlayerLeavesBuilding.OnServerReceived += OnPlayerLeaveBuildingServer;
        }

        onWeatherChanged.OnClientReceived += OnWeatherChangedClient;
    }

    internal void AdvanceIteration()
    {
        environmentIteration++;
        EnableTeleporters();
    }

    internal void OnPlayerEnterBuilding() => onPlayerEntersBuilding.InvokeServer();
    internal void OnPlayerLeaveBuilding() => onPlayerLeavesBuilding.InvokeServer();

    [SimAttributes.HostOnly]
    private void OnPlayerEnterBuildingServer(ulong clientId)
    {
        if (currentIndoorEnvironmentIteration == environmentIteration) return;
        currentIndoorEnvironmentIteration = environmentIteration;

        // Disable current environment
        GameObject.Find("Environment").SetActive(false);

        previouslyLoadedScene = string.IsNullOrEmpty(currentLoadedScene)
            ? RoundManager.Instance.currentLevel.sceneName
            : currentLoadedScene;

        var pickableLevels = Simulacrum.Environment.Levels
            .Where(level => level.sceneName != RoundManager.Instance.currentLevel.sceneName)
            .ToList();
        currentLoadedScene = pickableLevels[Random.Range(0, pickableLevels.Count)].sceneName;

        WaveController.DespawnAllEntities();

        NetworkManager.Singleton.SceneManager.LoadScene(currentLoadedScene, LoadSceneMode.Additive);
    }

    [SimAttributes.HostOnly]
    private void OnPlayerLeaveBuildingServer(ulong clientId)
    {
        if (currentOutdoorEnvironmentIteration == environmentIteration) return;
        currentOutdoorEnvironmentIteration = environmentIteration;

        WaveController.DespawnAllEntities();

        NetworkManager.Singleton.SceneManager.UnloadScene(SceneManager.GetSceneByName(previouslyLoadedScene));
    }

    private void DisableTeleporters()
    {
        TeleportersDisabled = true;
        foreach (var teleporter in FindObjectsOfType<EntranceTeleport>())
        {
            var interactTrigger = teleporter.GetComponent<InteractTrigger>();
            teleportDefaultSprite = interactTrigger.hoverIcon;
            interactTrigger.hoverTip = "Nuh uh!";
            interactTrigger.hoverIcon = SimAssets.LoadSpriteFromResources("troll.png");
        }
    }

    private void EnableTeleporters()
    {
        TeleportersDisabled = false;
        foreach (var teleporter in FindObjectsOfType<EntranceTeleport>())
        {
            var interactTrigger = teleporter.GetComponent<InteractTrigger>();
            interactTrigger.hoverTip = "Good luck!";
            interactTrigger.hoverIcon = teleportDefaultSprite ?? interactTrigger.hoverIcon;
        }
    }

    /*
     * Outdoor scene available, dungeon not finished yet. Executed on every client.
     */
    private void OnLevelPreload()
    {
        DisableTeleporters();

        if (NetworkManager.Singleton.IsHost)
        {
            TimeOfDay.Instance.globalTime = Random.Range(0, TimeOfDay.Instance.globalTimeAtEndOfDay);

            // Change the weather and send update to clients
            onWeatherChanged.SendClients(new WeatherChangeMessage
            {
                Type = possibleWeathers[Random.Range(0, possibleWeathers.Length)],
                WeatherSeed = Random.Range(int.MinValue, int.MaxValue)
            });

            RoundManager.Instance.outsideAINodes = (
                from x in GameObject.FindGameObjectsWithTag("OutsideAINode")
                orderby Vector3.Distance(x.transform.position, Vector3.zero)
                select x
            ).ToArray();

            // Teleport all players to unique nodes.
            var availableNodes = RoundManager.Instance.outsideAINodes.ToList();
            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                if (!player.isPlayerControlled) return;

                var node = availableNodes[Random.Range(0, availableNodes.Count)];
                Simulacrum.Players.TeleportPlayer.SendServer(new TeleportPlayerRequest
                {
                    Destination = node.transform.position,
                    PlayerId = player.playerClientId
                });

                availableNodes.Remove(node);
            }
        }
    }

    /*
     * Outdoor and indoor scene available. Executed on every client.
     */
    internal void OnLevelPostload()
    {
        Simulacrum.Players.ShowLargeMessage("FIGHT OR DIE!", 40, 3);

        // Exit startup scene if loaded for the first time
        if (Simulacrum.SetupScene.IsInSetupScene) Simulacrum.SetupScene.Exit();

        if (NetworkManager.Singleton.IsHost) Simulacrum.Waves.StartIteration();

        Simulacrum.Players.BlackoutFadeOut();
    }

    private static void OnWeatherChangedClient(WeatherChangeMessage message)
    {
        WeatherController.ChangeCurrentWeather(message.Type);
        StartOfRound.Instance.SetMapScreenInfoToCurrentLevel();
        RoundManager.Instance.SetToCurrentLevelWeather();
        TimeOfDay.Instance.foggyWeather.parameters.meanFreePath = new System.Random(message.WeatherSeed).Next(120, 250);
        for (var i = 0; i < TimeOfDay.Instance.effects.Length; i++)
        {
            var weatherEffect = TimeOfDay.Instance.effects[i];
            var isEnabled = (int)StartOfRound.Instance.currentLevel.currentWeather == i;
            weatherEffect.effectEnabled = isEnabled;
            if (weatherEffect.effectPermanentObject)
            {
                weatherEffect.effectPermanentObject.SetActive(value: isEnabled);
            }

            if (weatherEffect.effectObject)
            {
                weatherEffect.effectObject.SetActive(value: isEnabled);
            }

            if (TimeOfDay.Instance.sunAnimator)
            {
                if (isEnabled && !string.IsNullOrEmpty(weatherEffect.sunAnimatorBool))
                {
                    TimeOfDay.Instance.sunAnimator.SetBool(weatherEffect.sunAnimatorBool, value: true);
                }
                else
                {
                    TimeOfDay.Instance.sunAnimator.Rebind();
                    TimeOfDay.Instance.sunAnimator.Update(0);
                }
            }
        }
    }

    /*
     * Called when the old scene has been unloaded and the new scene has already been loaded.
     */
    private static void OnSceneUnloaded(ulong clientId, string sceneName)
    {
        Simulacrum.Players.BlackoutFadeOut();

        Simulacrum.Environment.OnLevelPreload();

        if (NetworkManager.Singleton.IsHost)
        {
            RoundManager.Instance.LoadNewLevel(
                Random.Range(int.MinValue, int.MaxValue),
                RoundManager.Instance.currentLevel
            );
        }
    }

    /*
     * Called when a new scene is loaded.
     */
    private static void OnSceneLoaded(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        RoundManager.Instance.currentLevel = StartOfRound.Instance.levels.First(level => level.sceneName == sceneName);

        // Only executed when the scene is loaded for the first time
        if (Simulacrum.SetupScene.IsInSetupScene)
        {
            Simulacrum.Environment.OnLevelPreload();

            if (NetworkManager.Singleton.IsHost)
            {
                RoundManager.Instance.LoadNewLevel(
                    Random.Range(int.MinValue, int.MaxValue),
                    RoundManager.Instance.currentLevel
                );
            }
        }
    }
}