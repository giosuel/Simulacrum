using System.Collections;
using LethalNetworkAPI;
using Simulacrum.Network;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Simulacrum;

public class PlayerController : MonoBehaviour
{
    internal LNetworkMessage<TeleportPlayerRequest> TeleportPlayer { get; private set; }

    private Coroutine showLargeMessageCoroutine;
    private static GameObject largeMessageContainer;
    private static TMP_Text largeMessageText;

    private static Animator completionAnimator;
    private static TMP_Text completionTitle;
    private static TMP_Text completionText;

    private Volume blackoutVolume;
    private Coroutine blackoutCoroutine;
    private const float blackoutFadeDuration = 0.8f;

    private void Awake()
    {
        TeleportPlayer = LNetworkMessage<TeleportPlayerRequest>.Connect("TeleportPlayer");

        if (NetworkManager.Singleton.IsHost)
        {
            TeleportPlayer.OnServerReceived += OnTeleportPlayerServer;
        }

        TeleportPlayer.OnClientReceived += OnTeleportPlayerClient;

        var systemsOnline = GameObject.Find("IngamePlayerHUD").transform.Find("BottomMiddle/SystemsOnline").gameObject;
        largeMessageContainer = Instantiate(systemsOnline, systemsOnline.transform.parent);
        largeMessageContainer.SetActive(false);
        largeMessageText = largeMessageContainer.transform.Find("TipLeft1").GetComponent<TMP_Text>();

        completionAnimator = HUDManager.Instance.reachedProfitQuotaAnimator;
        completionTitle = HUDManager.Instance.reachedProfitQuotaAnimator.transform.Find("MetQuota/Image/MetQuotaText").GetComponent<TMP_Text>();
        completionText = HUDManager.Instance.reachedProfitQuotaAnimator.transform.Find("MetQuota/OvertimeBonus").GetComponent<TMP_Text>();

        CreateBlackoutVolume();
    }

    private void CreateBlackoutVolume()
    {
        var blackoutVolumeObj = Instantiate(GameObject.Find("IngamePlayerHUD").transform.Find("Global Volume (1)"));
        blackoutVolume = blackoutVolumeObj.GetComponent<Volume>();
        blackoutVolume.weight = 0;
        if (blackoutVolume.profile.TryGet<Bloom>(out var bloom))
        {
            bloom.active = false;
        }

        if (blackoutVolume.profile.TryGet<ColorAdjustments>(out var colorAdjustments))
        {
            colorAdjustments.active = true;
            colorAdjustments.postExposure.overrideState = true;
            colorAdjustments.postExposure.value = -20f;
        }
    }

    internal void BlackoutFadeIn()
    {
        if (blackoutCoroutine != null) StopCoroutine(blackoutCoroutine);
        blackoutCoroutine = StartCoroutine(blackoutFadeIn());
    }

    internal void BlackoutFadeOut()
    {
        if (blackoutCoroutine != null) StopCoroutine(blackoutCoroutine);
        blackoutCoroutine = StartCoroutine(blackoutFadeOut());
    }

    private IEnumerator blackoutFadeOut()
    {
        var elapsedTime = 0f;

        while (elapsedTime < blackoutFadeDuration)
        {
            blackoutVolume.weight = 1 - elapsedTime / blackoutFadeDuration;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator blackoutFadeIn()
    {
        var elapsedTime = 0f;

        while (elapsedTime < blackoutFadeDuration)
        {
            blackoutVolume.weight = elapsedTime / blackoutFadeDuration;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    private void OnTeleportPlayerServer(TeleportPlayerRequest request, ulong _) => TeleportPlayer.SendClients(request);

    private static void OnTeleportPlayerClient(TeleportPlayerRequest request)
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

    internal void ShowLargeMessage(string message, int size, int duration)
    {
        if (showLargeMessageCoroutine != null) StopCoroutine(showLargeMessageCoroutine);
        showLargeMessageCoroutine = StartCoroutine(showLargeMessage(message, size, duration));
    }

    internal void ShowCompletionMessage(string title, string message, int size, int duration)
    {
        if (showLargeMessageCoroutine != null) StopCoroutine(showLargeMessageCoroutine);
        showLargeMessageCoroutine = StartCoroutine(showCompletionMessage(title, message, size, duration));
    }

    private static IEnumerator showLargeMessage(string message, int size, int duration)
    {
        largeMessageText.text = message;
        largeMessageText.fontSize = size;
        largeMessageContainer.SetActive(true);
        yield return new WaitForSeconds(duration);
        largeMessageContainer.SetActive(false);
    }

    private static IEnumerator showCompletionMessage(string title, string text, int size, int duration)
    {
        completionTitle.text = title;
        completionText.text = text;
        completionTitle.fontSize = size;
        completionAnimator.SetBool("display", true);
        yield return new WaitForSeconds(duration);
        completionAnimator.SetBool("display", false);
    }
}