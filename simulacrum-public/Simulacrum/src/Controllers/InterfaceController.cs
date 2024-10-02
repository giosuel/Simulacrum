using System.Collections;
using Simulacrum.Interface;
using Simulacrum.Network;
using Simulacrum.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Simulacrum.Controllers;

public class InterfaceController : MonoBehaviour
{
    private Coroutine showLargeMessageCoroutine;
    private GameObject largeMessageContainer;
    private TMP_Text largeMessageText;

    private Animator quotaAnimator;

    private TMP_Text completionTitle;
    private TMP_Text completionText;

    private TMP_Text warningTitle1;
    private TMP_Text warningTitle2;
    private TMP_Text warningText1;
    private TMP_Text warningText2;

    private TMP_Text statusText;

    private readonly Color backgroundColor = new(0f, 0.14f, 0.2f, 0.9f);
    private readonly Color primaryColor = new(0f, 0.92f, 0.71f, 1f);

    private Volume damageVolume;

    /*
     * Simulacrum Interfaces
     */
    private MapUI mapUI;
    private LoadoutUI loadoutUI;
    private VitalsUI vitalsUI;

    /*
     * Damage volume settings
     */
    private bool isCriticalHealth;

    private float damageAnimationElapsedTime;
    private bool damageAnimationInFadeOutPhase;
    private Coroutine damageAnimationCoroutine;

    private const float damageAnimationFadeInTime = 0.3f;
    private const float damageAnimationFadeOutTime = 0.8f;
    private const float damageAnimationPulseTimeMin = 4f;
    private const float damageAnimationPulseTimeMax = 8f;

    private const float criticalDamageThreshold = 20f;
    private const float criticalDamagePermanentWeight = 0.4f;
    private const float heartbeatFadeOutSpeed = 1.5f;

    private static readonly int CriticalHit = Animator.StringToHash("CriticalHit");

    private void Awake()
    {
        var systemsOnline = GameObject.Find("IngamePlayerHUD").transform.Find("BottomMiddle/SystemsOnline").gameObject;
        largeMessageContainer = Instantiate(systemsOnline, systemsOnline.transform.parent);
        largeMessageContainer.SetActive(false);
        largeMessageText = largeMessageContainer.transform.Find("TipLeft1").GetComponent<TMP_Text>();

        quotaAnimator = HUDManager.Instance.reachedProfitQuotaAnimator;

        completionTitle = HUDManager.Instance.reachedProfitQuotaAnimator.transform
            .Find("MetQuota/Image/MetQuotaText").GetComponent<TMP_Text>();
        completionText = HUDManager.Instance.reachedProfitQuotaAnimator.transform
            .Find("MetQuota/OvertimeBonus").GetComponent<TMP_Text>();

        warningTitle1 = HUDManager.Instance.reachedProfitQuotaAnimator.transform
            .Find("DaysLeft/MaskImage (1)/Image/MetQuotaText (1)").GetComponent<TMP_Text>();
        warningTitle2 = HUDManager.Instance.reachedProfitQuotaAnimator.transform
            .Find("DaysLeft/MaskImage (1)/Image/MetQuotaText (3)").GetComponent<TMP_Text>();
        warningText1 = HUDManager.Instance.reachedProfitQuotaAnimator.transform
            .Find("DaysLeft/MaskImage (1)/Image/MetQuotaText").GetComponent<TMP_Text>();
        warningText2 = HUDManager.Instance.reachedProfitQuotaAnimator.transform
            .Find("DaysLeft/MaskImage (1)/Image/MetQuotaText (2)").GetComponent<TMP_Text>();

        // Disable new quota indicator because we don't need it in the animation
        HUDManager.Instance.reachedProfitQuotaAnimator.transform.Find("NewQuota/MaskImage").gameObject.SetActive(false);

        // Reparent weight UI and reuse as status (Unused because we use EladsHUD for weight)
        var weightUI = GameObject.Find("TopLeftCorner").transform.Find("WeightUI");
        weightUI.SetParent(weightUI.parent.parent, true);
        weightUI.GetComponent<RectTransform>().anchoredPosition = new Vector2(20, -245);
        statusText = weightUI.Find("Weight").GetComponent<TMP_Text>();
        var statusTextRect = statusText.GetComponent<RectTransform>();
        statusTextRect.pivot = Vector2.zero;
        statusTextRect.anchoredPosition = Vector2.zero;
        statusTextRect.sizeDelta = new Vector2(400, 50);
        statusText.fontSize = 20;

        var topRight = GameObject.Find("TopRightCorner").transform;
        topRight.Find("ControlTip1").gameObject.SetActive(false);
        topRight.Find("ControlTip2").gameObject.SetActive(false);
        topRight.Find("ControlTip3").gameObject.SetActive(false);
        topRight.Find("ControlTip4").gameObject.SetActive(false);

        // Hide inventory, chat, player info and tooltips
        for (var i = 0; i < 4; i++)
        {
            HUDManager.Instance.HUDElements[i].canvasGroup.alpha = 0;
            HUDManager.Instance.HUDElements[i].targetAlpha = 0;
        }

        // Disable critical injury overlay and remove volume
        GameObject.Find("CriticalHealthVolume").GetComponent<Volume>().profile = null;
        GameObject.Find("CriticalInjury").gameObject.SetActive(false);

        // Init our own damage volume
        var damageVolumeObj = new GameObject("DamageVolume");
        damageVolumeObj.transform.SetParent(transform);
        damageVolumeObj.layer = LayerMask.NameToLayer("UI");
        damageVolume = damageVolumeObj.AddComponent<Volume>();
        damageVolume.isGlobal = true;
        damageVolume.weight = 0;
        damageVolume.priority = 10;
        damageVolume.profile = SimAssets.DamageProfile;

        // Reference dummy object so weight isn't updated
        HUDManager.Instance.weightCounter = gameObject.AddComponent<TextMeshProUGUI>();
        SetInfo("");

        RepaintVanillaInterface();
        InitSimulacrumInterface();
    }

    private void InitSimulacrumInterface()
    {
        Simulacrum.State.onIterationStartClients += OnIterationStartClient;
        Simulacrum.State.onIterationEndClients += OnIterationEndClient;
        Simulacrum.Gulag.onPlayerEnterGulag += OnPlayerEnterGulag;
        Simulacrum.Gulag.onPlayerExitGulag += OnPlayerExitGulag;
        Simulacrum.Players.OnTakeDamage += OnTakeDamage;
        Simulacrum.Players.OnHealDamage += OnHealDamage;
        Simulacrum.Gulag.onPlayerEnterGulag += () => OnHealDamage(100);

        var container = GameObject.Find("IngamePlayerHUD").transform;
        var hudConstraintSource = new ConstraintSource
        {
            sourceTransform = GameObject.Find("TopLeftCorner").transform
        };

        var loadoutUIObj = Instantiate(SimAssets.LoadoutUIObject, container);
        var loadoutUIRect = loadoutUIObj.GetComponent<RectTransform>();
        loadoutUIObj.transform.Find("Container").GetComponent<PositionConstraint>().AddSource(hudConstraintSource);
        loadoutUIRect.anchorMin = new Vector2(1, 0);
        loadoutUIRect.anchorMax = new Vector2(1, 0);
        loadoutUIRect.localScale = Vector3.one * 0.75f;
        loadoutUI = loadoutUIRect.gameObject.AddComponent<LoadoutUI>();
        loadoutUI.Toggle(false);

        var mapUIObj = Instantiate(SimAssets.MapUIObject, container);
        var mapUIRect = mapUIObj.GetComponent<RectTransform>();
        mapUIObj.transform.Find("Container").GetComponent<PositionConstraint>().AddSource(hudConstraintSource);
        mapUIRect.anchorMin = new Vector2(0, 1);
        mapUIRect.anchorMax = new Vector2(0, 1);
        mapUIRect.localScale = Vector3.one * 0.75f;
        mapUI = mapUIRect.gameObject.AddComponent<MapUI>();
        mapUI.Toggle(false);

        var vitalsUIObj = Instantiate(SimAssets.VitalsUIObject, container);
        var vitalsUIRect = vitalsUIObj.GetComponent<RectTransform>();
        vitalsUIObj.transform.Find("Container").GetComponent<PositionConstraint>().AddSource(hudConstraintSource);
        vitalsUIRect.anchorMin = Vector2.zero;
        vitalsUIRect.anchorMax = Vector2.zero;
        vitalsUIRect.localScale = Vector3.one * 0.75f;
        vitalsUI = vitalsUIRect.gameObject.AddComponent<VitalsUI>();
        vitalsUI.Toggle(false);
    }

    private void OnTakeDamage(int damageTaken)
    {
        // Trigger critical hit animation for HUD shake only
        HUDManager.Instance.HUDAnimator.SetTrigger(CriticalHit);

        if (Simulacrum.Player.health < criticalDamageThreshold) isCriticalHealth = true;

        // If damage animation is already running and is not in healing phase, reset the timer to 0, otherwise start new coroutine
        if (damageAnimationCoroutine != null)
        {
            if (!damageAnimationInFadeOutPhase)
            {
                damageAnimationElapsedTime = 0;
                return;
            }

            StopCoroutine(damageAnimationCoroutine);
        }

        damageAnimationCoroutine = StartCoroutine(damageAnimation(damageTaken));
    }

    private void OnHealDamage(int healthRestored)
    {
        if (Simulacrum.Player.health > criticalDamageThreshold && isCriticalHealth)
        {
            isCriticalHealth = false;

            // Stop current damage coroutine and start healing coroutine
            if (damageAnimationCoroutine != null) StopCoroutine(damageAnimationCoroutine);
            StartCoroutine(healFromCriticalAnimation());
        }
    }

    private IEnumerator damageAnimation(int damageTaken)
    {
        damageAnimationInFadeOutPhase = false;
        damageAnimationElapsedTime = 0;

        /*
         * Intensity is used to determine the max weight and how long it will take to return back to 0 weight
         *
         * The intensity determined by the amount of damage taken or the player's current HP if its below 50.
         */
        var intensity = Mathf.Max(
            Mathf.Clamp(damageTaken / 60f, 0.2f, 0.8f),
            Mathf.Clamp((50 - Simulacrum.Player.health) / 50f, 0.2f, 0.8f)
        );

        while (damageAnimationElapsedTime < damageAnimationFadeInTime)
        {
            var t = damageAnimationElapsedTime / damageAnimationFadeInTime;

            damageVolume.weight = Mathf.Lerp(0, intensity, t);

            damageAnimationElapsedTime += Time.deltaTime;
            yield return null;
        }

        var damageAnimationPulseTime = Mathf.Lerp(
            damageAnimationPulseTimeMin,
            damageAnimationPulseTimeMax,
            intensity
        );

        damageAnimationElapsedTime = 0;
        while (damageAnimationElapsedTime < damageAnimationPulseTime)
        {
            var t = damageAnimationElapsedTime / damageAnimationFadeOutTime;

            var heartbeatIntensity = Mathf.Lerp(0.1f, 0.4f, 1 - Simulacrum.Player.health / 50f);
            var heartbeatCooldown = Mathf.Lerp(0.2f, 0.8f, Simulacrum.Player.health / 50f);

            damageVolume.weight = intensity + Simulacrum.Player.health < 50 ? HeartbeatValue(
                heartbeatIntensity,
                heartbeatCooldown,
                damageAnimationElapsedTime
            ) : 0;

            damageAnimationElapsedTime += Time.deltaTime;
            yield return null;
        }

        var elapsedTime = 0f;
        damageAnimationInFadeOutPhase = true;
        var fadeOutStartWeight = damageVolume.weight;

        while (elapsedTime < damageAnimationFadeOutTime)
        {
            var t = elapsedTime / damageAnimationFadeOutTime;

            damageVolume.weight = Mathf.Lerp(fadeOutStartWeight, isCriticalHealth ? criticalDamagePermanentWeight : 0, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator healFromCriticalAnimation()
    {
        var elapsedTime = 0f;

        while (elapsedTime < damageAnimationFadeOutTime)
        {
            damageVolume.weight = Mathf.Lerp(
                criticalDamagePermanentWeight,
                0,
                elapsedTime / damageAnimationFadeOutTime
            );

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    private static float HeartbeatValue(float heartbeatIntensity, float heartbeatCooldown, float time)
    {
        var normalizedTime = Mathf.Repeat(time, heartbeatCooldown);

        var firstBeat = Mathf.Exp(-heartbeatFadeOutSpeed * Mathf.Pow(normalizedTime, 2)) * heartbeatIntensity;
        var secondBeatTime = normalizedTime - 0.1f;
        var secondBeat = Mathf.Exp(-heartbeatFadeOutSpeed * Mathf.Pow(secondBeatTime, 2)) * heartbeatIntensity;

        return Mathf.Max(firstBeat, secondBeat);
    }

    private void RepaintVanillaInterface()
    {
        var canvas = GameObject.Find("UI").transform.Find("Canvas");
        canvas.transform.Find("QuickMenu/Panel (2)").GetComponent<Image>().color = backgroundColor;
        canvas.transform.Find("QuickMenu/MainButtons/Panel (1)").GetComponent<Image>().color = primaryColor;
        canvas.transform.Find("QuickMenu/PlayerList/Image").GetComponent<Image>().color = primaryColor;
        canvas.transform.Find("QuickMenu/PlayerList/Image/Header").GetComponent<TMP_Text>().color = primaryColor;
    }

    internal void SetInfo(string infoString) => statusText.text = infoString;

    internal void ShowLargeMessage(string message, int size, int duration)
    {
        if (showLargeMessageCoroutine != null) StopCoroutine(showLargeMessageCoroutine);
        showLargeMessageCoroutine = StartCoroutine(showLargeMessage(message, size, duration));
    }

    internal void ShowCompletionMessage(string title, string message, int duration)
    {
        if (showLargeMessageCoroutine != null) StopCoroutine(showLargeMessageCoroutine);
        showLargeMessageCoroutine = StartCoroutine(showCompletionMessage(title, message, duration));
    }

    internal void ShowWarningMessage(string title, string message)
    {
        warningTitle1.text = title;
        warningTitle2.text = title;
        warningText1.text = message;
        warningText2.text = message;
        quotaAnimator.SetTrigger("displayDaysLeft");
        HUDManager.Instance.UIAudio.PlayOneShot(HUDManager.Instance.OneDayToMeetQuotaSFX);
    }

    private IEnumerator showLargeMessage(string message, int size, int duration)
    {
        largeMessageText.text = message;
        largeMessageText.fontSize = size;
        largeMessageContainer.SetActive(true);
        yield return new WaitForSeconds(duration);
        largeMessageContainer.SetActive(false);
    }

    private IEnumerator showCompletionMessage(string title, string text, int duration)
    {
        completionTitle.text = title;
        completionText.text = text;
        quotaAnimator.SetBool("display", true);
        yield return new WaitForSeconds(duration);
        quotaAnimator.SetBool("display", false);
    }

    private void OnPlayerEnterGulag() => ToggleInterfaces(false);
    private void OnPlayerExitGulag() => ToggleInterfaces(true);

    private void OnIterationStartClient(IterationStartMessage message) => ToggleInterfaces(true);
    private void OnIterationEndClient(IterationEndMessage message) => ToggleInterfaces(false);

    private void ToggleInterfaces(bool isOn)
    {
        loadoutUI.Toggle(isOn);
        mapUI.Toggle(isOn);
        vitalsUI.Toggle(isOn);
    }
}