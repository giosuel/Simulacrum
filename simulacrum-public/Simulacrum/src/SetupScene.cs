using System.Collections;
using System.Linq;
using LethalNetworkAPI;
using Simulacrum.Controllers;
using Simulacrum.Network;
using Simulacrum.Objects;
using Simulacrum.Utils;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Simulacrum;

internal class SetupScene : MonoBehaviour
{
    internal bool IsInSetupScene { get; private set; }

    internal readonly LNetworkEvent onPlayerReady = LNetworkEvent.Connect("SetupPlayerReady");
    private readonly LNetworkEvent onLevelLoading = LNetworkEvent.Connect("SetupLevelLoading");

    private GameObject setupTile;
    private int playersReady;

    private Light lightFront;
    private Light lightBack;

    private float lightFrontIntensity;
    private float lightBackIntensity;

    private void Awake()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            onPlayerReady.OnServerReceived += OnPlayerReady;
        }

        onLevelLoading.OnClientReceived += OnLevelLoading;
    }

    internal void Launch()
    {
        IsInSetupScene = true;

        BuildScene();

        Simulacrum.Players.TeleportPlayer.SendServer(new TeleportPlayerRequest
        {
            Destination = GameController.PlayerPosition(GameController.SetupTilePosition),
            PlayerId = Simulacrum.Player.playerClientId
        });

        Simulacrum.Props.SpawnItem.SendServer(new ItemSpawnRequest
        {
            Name = "Shovel",
            SpawnPosition = GameController.PropPosition(GameController.SetupTilePosition),
            SpawnWithAnimation = false
        });

        StartOfRound.Instance.shipAmbianceAudio.PlayOneShot(SimAssets.AmbientSFX);

        var leNull = Instantiate(SimAssets.VoidEssence, GameController.PlayerPosition(GameController.SetupTilePosition) + Vector3.up, Quaternion.identity);
        leNull.transform.localScale = Vector3.one * 0.2f;
    }

    private void BuildScene()
    {
        GameObject.Find("SpaceProps").SetActive(false);

        setupTile = Instantiate(SimAssets.BrackenRoomRef.gameObject, GameController.SetupTilePosition, Quaternion.identity);
        setupTile.transform.SetParent(transform, worldPositionStays: true);

        setupTile.transform.Find("Spot Light").gameObject.AddComponent<LightFlicker>();
        setupTile.transform.Find("Spot Light (1)").gameObject.AddComponent<LightFlicker>();
        // lightFront = setupTile.transform.Find("Spot Light").GetComponent<Light>();
        // lightFrontIntensity = lightFront.intensity;
        // lightBack = setupTile.transform.Find("Spot Light (1)").GetComponent<Light>();
        // lightBackIntensity = lightBack.intensity;

        var door = Resources.FindObjectsOfTypeAll<GameObject>().First(obj => obj.name == "SteelDoorMapModel");
        var doorPosition = setupTile.transform.Find("Door1 (18)").transform.position;
        var doorObj = Instantiate(door, doorPosition, Quaternion.Euler(0, -90, 0));
        Destroy(doorObj.GetComponent<NetworkObject>());
        Destroy(doorObj.transform.Find("SteelDoor (1)/DoorMesh").gameObject);
        doorObj.transform.SetParent(setupTile.transform, worldPositionStays: true);

        var doorBlocker = Instantiate(
            SimAssets.DoorBlocker, doorPosition + Vector3.up * 3, Quaternion.Euler(0, 90, 0)
        );
        doorBlocker.transform.SetParent(setupTile.transform, worldPositionStays: true);
    }

    internal void ExitSetup()
    {
        IsInSetupScene = false;
        Destroy(setupTile);
    }

    private void OnLevelLoading()
    {
        StartCoroutine(loadLevelCountdown());
    }

    [SimAttributes.HostOnly]
    private void OnPlayerReady(ulong clientId)
    {
        playersReady++;
        HUDManager.Instance.AddTextToChatOnServer($"{Simulacrum.Player.playerUsername} is ready!");

        if (playersReady == GameNetworkManager.Instance.connectedPlayers)
        {
            if (!GameNetworkManager.Instance.gameHasStarted)
            {
                GameNetworkManager.Instance.LeaveLobbyAtGameStart();
                GameNetworkManager.Instance.gameHasStarted = true;
            }

            onLevelLoading.InvokeClients();
            StartCoroutine(loadSceneDelayed());
        }
    }

    /*
     * Loads the first scene once all players are ready.
     */
    [SimAttributes.HostOnly]
    private static IEnumerator loadSceneDelayed()
    {
        yield return new WaitForSeconds(3);

        Simulacrum.Log.LogInfo("LOADING SCENE");

        // NetworkManager.Singleton.SceneManager.LoadScene(
        //     EnvironmentController.Levels[Random.Range(0, EnvironmentController.Levels.Length)].SelectableLevel.sceneName,
        //     LoadSceneMode.Additive
        // );
        NetworkManager.Singleton.SceneManager.LoadScene(
            "Level1Experimentation",
            LoadSceneMode.Additive
        );
    }

    private static IEnumerator loadLevelCountdown()
    {
        Simulacrum.Interface.ShowLargeMessage("Teleportation in 3..", 28, 1);
        yield return new WaitForSeconds(1);
        Simulacrum.Interface.ShowLargeMessage("Teleportation in 2..", 28, 1);
        yield return new WaitForSeconds(1);
        Simulacrum.Interface.ShowLargeMessage("Teleportation in 1..", 28, 1);
        yield return new WaitForSeconds(0.6f);

        Simulacrum.Players.BlackoutFadeIn();
    }

    private void Update()
    {
        if (!IsInSetupScene) return;

        // lightFront.intensity = Random.Range(0f, 1f) > 0.98f ? 0 : lightFrontIntensity;
        // lightBack.intensity = Random.Range(0f, 1f) > 0.98f ? 0 : lightBackIntensity;
    }
}