using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LethalLevelLoader;
using LethalNetworkAPI;
using Simulacrum.Network;
using Simulacrum.Objects;
using Simulacrum.Utils;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using WeatherRegistry;
using Random = UnityEngine.Random;

namespace Simulacrum.Controllers;

internal class EnvironmentController : MonoBehaviour
{
    private string previouslyLoadedScene;
    private string currentLoadedScene;

    // Whether the teleporters are currently disabled
    internal bool TeleportersDisabled { get; private set; }

    private Sprite teleportDefaultSprite;

    /*
     * State to make sure the dungeon and outdoor scene is only refreshed once per iteration.
     */
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

    internal static ExtendedLevel[] Levels;

    private List<Vector3> spawnNodes;
    private List<Vector3> spawnNodesHistory;
    private readonly HashSet<Vector3> usedNodes = [];

    internal static readonly LayerMask GroundMask = LayerMask.GetMask("Room", "Terrain", "Railing");

    private void Awake()
    {
        NetworkManager.Singleton.SceneManager.OnUnloadComplete += OnSceneUnloaded;
        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoaded;

        onWeatherChanged.OnClientReceived += OnWeatherChangedClient;

        Simulacrum.State.onIterationStartClients += OnIterationStartClient;

        if (NetworkManager.Singleton.IsHost)
        {
            onPlayerEntersBuilding.OnServerReceived += OnPlayerEnterBuildingServer;
            onPlayerLeavesBuilding.OnServerReceived += OnPlayerLeaveBuildingServer;

            Simulacrum.State.onIterationStartServer += OnIterationStartServerServer;
        }
    }

    internal void UnlockNextArea()
    {
        EnableTeleporters();
    }

    internal void OnPlayerEnterBuildingClient() => onPlayerEntersBuilding.InvokeServer();
    internal void OnPlayerLeaveBuildingClient() => onPlayerLeavesBuilding.InvokeServer();

    [SimAttributes.HostOnly]
    private void OnPlayerEnterBuildingServer(ulong clientId)
    {
        if (currentIndoorEnvironmentIteration == Simulacrum.State.CurrentIteration + 1) return;
        currentIndoorEnvironmentIteration = Simulacrum.State.CurrentIteration + 1;

        // Disable current environment
        GameObject.Find("Environment").SetActive(false);

        previouslyLoadedScene = string.IsNullOrEmpty(currentLoadedScene)
            ? RoundManager.Instance.currentLevel.sceneName
            : currentLoadedScene;

        var pickableLevels = Levels
            .Where(level => level.NumberlessPlanetName != LevelManager.CurrentExtendedLevel.NumberlessPlanetName)
            .ToList();
        currentLoadedScene = pickableLevels[Random.Range(0, pickableLevels.Count)].SelectableLevel.sceneName;

        WaveController.DespawnAllEntities();

        NetworkManager.Singleton.SceneManager.LoadScene(currentLoadedScene, LoadSceneMode.Additive);
    }

    [SimAttributes.HostOnly]
    private void OnPlayerLeaveBuildingServer(ulong clientId)
    {
        if (currentOutdoorEnvironmentIteration == Simulacrum.State.CurrentIteration + 1) return;
        currentOutdoorEnvironmentIteration = Simulacrum.State.CurrentIteration + 1;

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
        // TODO(giosuel): DEBUG
        // DisableTeleporters();

        var sunContainer = GameObject.Find("SunAnimContainer")?.transform;
        if (sunContainer)
        {
            sunContainer.Find("Sky and Fog Global Volume").GetComponent<Volume>().profile = SimAssets.DefaultSkyProfile;
            sunContainer.Find("Sky and Fog Global Volume (1)").gameObject.SetActive(false);
            sunContainer.Find("SunTexture").gameObject.SetActive(false);
            sunContainer.Find("EclipseObject").gameObject.SetActive(false);
        }
        else
        {
            Simulacrum.Log.LogInfo("UNABLE TO FIND SunAnimContainer");
        }

        // GameObject.Find("QuickSand").SetActive(false);

        var environmentSphere = Instantiate(SimAssets.EnvironmentSphere, Vector3.zero, Quaternion.identity);
        environmentSphere.SetActive(true);

        // Simulacrum.Player.gameplayCamera.GetComponent<HDAdditionalCameraData>().stopNaNs = true;

        if (NetworkManager.Singleton.IsHost)
        {
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
    internal void OnLevelPreinit()
    {
        StartOfRound.Instance.shipAnimator.enabled = false;
        StartOfRound.Instance.shipDoorsAnimator.enabled = false;
        StartOfRound.Instance.speakerAudioSource.mute = true;
        StartOfRound.Instance.shipAmbianceAudio.mute = true;
        StartOfRound.Instance.ship3DAudio.mute = true;
        HUDManager.Instance.quotaAnimator.enabled = false;
        HUDManager.Instance.planetIntroAnimator.enabled = false;

        StartOfRound.Instance.shipAnimator.transform.Translate(new Vector3(0, -20, 0));

        // Exit startup scene if loaded for the first time
        if (Simulacrum.SetupScene.IsInSetupScene) Simulacrum.SetupScene.ExitSetup();

        if (NetworkManager.Singleton.IsHost)
        {
            Simulacrum.Log.LogInfo("EXEUCTE POSTINI");
            OnLevelPostinit();
        }
    }

    [SimAttributes.HostOnly]
    internal void OnLevelPostinit()
    {
        TimeOfDay.Instance.globalTime = SimUtils.GenerateRandomDistributedValue(
            min: 0,
            max: TimeOfDay.Instance.lengthOfHours * TimeOfDay.Instance.numberOfHours,
            distribution: Distribution.Normal,
            normalMean: Simulacrum.GameConfig.Environment.DaytimeDistributionMean.Value,
            normalStdDev: Simulacrum.GameConfig.Environment.DaytimeDistributionStdDev.Value,
            randomGenerator: SimUtils.UniformRandomNumber
        );

        var currentDayTime = TimeOfDay.Instance.CalculatePlanetTime(StartOfRound.Instance.currentLevel);

        TimeOfDay.Instance.sunAnimator.SetFloat("timeOfDay", Mathf.Clamp(currentDayTime / TimeOfDay.Instance.totalTime, 0f, 0.99f));
        TimeOfDay.Instance.sunAnimator.speed = 0;

        Simulacrum.Log.LogError($"GLOBAL TIME: {TimeOfDay.Instance.globalTime}");

        // Change the weather and send update to clients
        onWeatherChanged.SendClients(new WeatherChangeMessage
        {
            Type = possibleWeathers[Random.Range(0, possibleWeathers.Length)],
            WeatherSeed = Random.Range(int.MinValue, int.MaxValue)
        });

        Simulacrum.Waves.StartIteration();
    }

    [SimAttributes.HostOnly]
    private void OnIterationStartServerServer(int waveCount)
    {
        ResetNodes();
    }

    private void OnIterationStartClient(IterationStartMessage message)
    {
        Simulacrum.Log.LogInfo("ITERATION STARTING RN");

        if (!GameNetworkManager.Instance.disableSteam)
        {
            GameNetworkManager.Instance.SetSteamFriendGrouping(
                GameNetworkManager.Instance.steamLobbyName,
                StartOfRound.Instance.connectedPlayersAmount + 1,
                "Fighting in the Simulacrum"
            );
        }

        var playerHologram = Instantiate(SimAssets.PlayerHologram, Simulacrum.Environment.FindFreeNode(), Quaternion.identity);
        playerHologram.SetActive(true);
        playerHologram.AddComponent<PlayerHologram>().Init(Simulacrum.Player);

        /*
         * Disable ship and ship objects
         */
        // GameObject.Find("HangarShip").SetActive(false);
        // foreach (var autoParentToShip in FindObjectsByType<AutoParentToShip>(FindObjectsSortMode.None))
        // {
        //     autoParentToShip.gameObject.SetActive(false);
        // }

        Simulacrum.Log.LogInfo("BLACKOUT FADE OUT");
        StartCoroutine(iterationStartAnimation());
    }

    private IEnumerator iterationStartAnimation()
    {
        yield return new WaitForSeconds(0.8f);
        Simulacrum.Players.BlackoutFadeOut();
        Simulacrum.Interface.ShowLargeMessage("FIGHT OR DIE!", 40, 2);
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
        Simulacrum.Log.LogInfo("SCENE IS LOADED");

        RoundManager.Instance.currentLevel = StartOfRound.Instance.levels.First(level => level.sceneName == sceneName);

        // Only executed when the scene is loaded for the first time
        if (Simulacrum.SetupScene.IsInSetupScene)
        {
            Simulacrum.Log.LogInfo("WAS FIRST SCENE, LOADING NEW LEVEL");
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

    [SimAttributes.HostOnly]
    internal Vector3 FindFreeNode()
    {
        spawnNodes ??= FindAllNodes();

        var node = SimUtils.PickRandomWeightedItem(
            spawnNodes.Where(node => !usedNodes.Contains(node)).ToList(),
            spawnNodesHistory
        );
        usedNodes.Add(node);

        var castToGround = Physics.Raycast(
            new Ray(node, Vector3.down),
            out var groundInfo, 2, GroundMask
        );

        return castToGround ? groundInfo.point : node;
    }

    [SimAttributes.HostOnly]
    internal void FreeNode(Vector3 node) => usedNodes.Remove(node);

    [SimAttributes.HostOnly]
    private void ResetNodes()
    {
        usedNodes.Clear();

        spawnNodes = FindAllNodes();
        spawnNodesHistory = [];
    }

    [SimAttributes.HostOnly]
    private static List<Vector3> FindAllNodes()
    {
        // TODO(giosuel): Debug
        // return (
        //     from x in GameObject.FindGameObjectsWithTag(
        //         Simulacrum.State.CurrentIteration % 2 == 0 ? "AINode" : "OutsideAINode"
        //     )
        //     select x.transform.position
        // ).ToList();
        return (
            from x in GameObject.FindGameObjectsWithTag("OutsideAINode")
            select x.transform.position
        ).ToList();
    }
}