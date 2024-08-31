using System.Collections;
using System.Linq;
using DunGen;
using LethalNetworkAPI;
using Simulacrum.Network;
using Simulacrum.Utils;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Simulacrum;

internal class SetupScene : MonoBehaviour
{
    internal bool IsInSetupScene { get; private set; }
    internal bool PickedUpWeapon { get; set; }

    internal readonly LNetworkEvent onPlayerReady = LNetworkEvent.Connect("SetupPlayerReady");
    internal readonly LNetworkEvent onLevelLoading = LNetworkEvent.Connect("SetupLevelLoading");

    private int playersReady;

    private GameObject setupTile;

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
        var startingTile = Resources.FindObjectsOfTypeAll<Tile>().First(tile => tile.name == "SmallRoom2");
        setupTile = Instantiate(startingTile.gameObject, new Vector3(100, 0, 100), Quaternion.identity);

        Simulacrum.Players.TeleportPlayer.SendServer(new TeleportPlayerRequest
        {
            Destination = new Vector3(100, 1, 100),
            PlayerId = Simulacrum.Player.playerClientId
        });

        Simulacrum.Props.SpawnItem.SendServer(new ItemSpawnRequest
        {
            Name = "Shovel",
            SpawnPosition = new Vector3(100, 1, 90),
            SpawnWithAnimation = false
        });
    }

    internal void Exit()
    {
        Destroy(setupTile);
        IsInSetupScene = false;
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

    [SimAttributes.HostOnly]
    private static IEnumerator loadSceneDelayed()
    {
        yield return new WaitForSeconds(3);

        NetworkManager.Singleton.SceneManager.LoadScene(
            Simulacrum.Environment.Levels[Random.Range(0, Simulacrum.Environment.Levels.Length)].sceneName,
            LoadSceneMode.Additive
        );
    }

    private static IEnumerator loadLevelCountdown()
    {
        Simulacrum.Players.ShowLargeMessage("Teleportation in 3..", 28, 1);
        yield return new WaitForSeconds(1);
        Simulacrum.Players.ShowLargeMessage("Teleportation in 2..", 28, 1);
        yield return new WaitForSeconds(1);
        Simulacrum.Players.ShowLargeMessage("Teleportation in 1..", 28, 1);
        yield return new WaitForSeconds(0.6f);

        Simulacrum.Players.BlackoutFadeIn();
    }
}