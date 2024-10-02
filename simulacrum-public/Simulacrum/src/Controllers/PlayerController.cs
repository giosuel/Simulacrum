using System;
using System.Collections;
using LethalNetworkAPI;
using Simulacrum.Network;
using Simulacrum.Utils;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Simulacrum.Controllers;

public class PlayerController : MonoBehaviour
{
    internal LNetworkMessage<TeleportPlayerRequest> TeleportPlayer { get; private set; }
    private LNetworkMessage<TeleportPlayerResponse> OnTeleportPlayer { get; set; }

    private float startingVolume;

    private Volume blackoutVolume;
    private Volume whiteoutVolume;
    private Coroutine overlayCoroutine;
    private const float overlayFadeDuration = 0.8f;

    internal int CurrentRegenerationAmount;

    internal event Action<int> OnTakeDamage;
    internal event Action<int> OnHealDamage;

    internal event Action<GrabbableObject> OnItemEquipped;
    internal event Action<ShotgunItem> OnShotgunShot;
    internal event Action<ShotgunItem> OnShotgunReloadStart;
    internal event Action<ShotgunItem> OnShotgunReloadEnd;

    private void Awake()
    {
        TeleportPlayer = LNetworkMessage<TeleportPlayerRequest>.Connect("TeleportPlayer");
        OnTeleportPlayer = LNetworkMessage<TeleportPlayerResponse>.Connect("OnTeleportPlayer");

        if (NetworkManager.Singleton.IsHost)
        {
            TeleportPlayer.OnServerReceived += OnTeleportPlayerServer;
        }

        OnTeleportPlayer.OnClientReceived += OnTeleportPlayerResponseClient;

        blackoutVolume = CreateOverlayVolume(SimAssets.BlackoutVolume);
        whiteoutVolume = CreateOverlayVolume(SimAssets.WhiteoutVolume);
    }

    internal void GrabItem(GrabbableObject item) => OnItemEquipped?.Invoke(item);
    internal void ShootGun(ShotgunItem gun) => OnShotgunShot?.Invoke(gun);
    internal void StartReloadingGun(ShotgunItem gun) => OnShotgunReloadStart?.Invoke(gun);
    internal void EndReloadingGun(ShotgunItem gun) => OnShotgunReloadEnd?.Invoke(gun);

    internal void TakeDamage(int damageTaken) => OnTakeDamage?.Invoke(damageTaken);

    internal void HealDamage(int healthRestored)
    {
        Simulacrum.Player.health += healthRestored;
        OnHealDamage?.Invoke(healthRestored);
    }

    internal void WhiteoutFadeIn()
    {
        startingVolume = AudioListener.volume;

        if (overlayCoroutine != null) StopCoroutine(overlayCoroutine);
        overlayCoroutine = StartCoroutine(overlayFadeIn(whiteoutVolume));
    }

    internal void WhiteoutFadeOut()
    {
        if (overlayCoroutine != null) StopCoroutine(overlayCoroutine);
        overlayCoroutine = StartCoroutine(overlayFadeOut(whiteoutVolume));
    }

    internal void BlackoutFadeIn()
    {
        startingVolume = AudioListener.volume;

        if (overlayCoroutine != null) StopCoroutine(overlayCoroutine);
        overlayCoroutine = StartCoroutine(overlayFadeIn(blackoutVolume));
    }

    internal void BlackoutFadeOut()
    {
        if (overlayCoroutine != null) StopCoroutine(overlayCoroutine);
        overlayCoroutine = StartCoroutine(overlayFadeOut(blackoutVolume));
    }

    private IEnumerator overlayFadeIn(Volume volume)
    {
        var elapsedTime = 0f;

        while (elapsedTime < overlayFadeDuration)
        {
            var t = elapsedTime / overlayFadeDuration;

            volume.weight = t;
            AudioListener.volume = Mathf.Lerp(startingVolume, 0, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator overlayFadeOut(Volume volume)
    {
        var elapsedTime = 0f;

        while (elapsedTime < overlayFadeDuration)
        {
            var t = elapsedTime / overlayFadeDuration;

            volume.weight = 1 - t;
            AudioListener.volume = Mathf.Lerp(0, startingVolume, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    private static Volume CreateOverlayVolume(VolumeProfile profile)
    {
        var overlayVolumeObj = Instantiate(GameObject.Find("IngamePlayerHUD").transform.Find("Global Volume (1)"));
        var overlayVolume = overlayVolumeObj.gameObject.GetComponent<Volume>();
        overlayVolume.profile = profile;
        overlayVolume.weight = 0;

        return overlayVolume;
    }

    private void OnTeleportPlayerServer(TeleportPlayerRequest request, ulong _)
    {
        OnTeleportPlayer.SendClients(new TeleportPlayerResponse
        {
            PlayerId = request.PlayerId,
            Destination = request.Destination ?? Simulacrum.Environment.FindFreeNode()
        });
    }

    private static void OnTeleportPlayerResponseClient(TeleportPlayerResponse request)
    {
        var player = StartOfRound.Instance.allPlayerScripts[request.PlayerId];

        player.TeleportPlayer(request.Destination);
        var isInFactory = request.Destination.y < -100;
        player.isInsideFactory = isInFactory;

        // There is no easy way to check this, so it will just be off by default for now
        var isInElevator = StartOfRound.Instance.shipBounds.bounds.Contains(request.Destination);
        player.isInElevator = isInElevator;

        var isInShip = StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(request.Destination);
        player.isInHangarShipRoom = isInShip;

        foreach (var heldItem in player.ItemSlots)
        {
            if (!heldItem) continue;
            heldItem.isInFactory = isInFactory;
            heldItem.isInShipRoom = isInShip;
            heldItem.isInFactory = isInFactory;
        }

        if (request.PlayerId == NetworkManager.Singleton.LocalClientId) TimeOfDay.Instance.DisableAllWeather();
    }
}