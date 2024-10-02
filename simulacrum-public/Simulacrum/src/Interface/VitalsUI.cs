using System;
using LethalLib.Modules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Simulacrum.Interface;

public class VitalsUI : MonoBehaviour
{
    private Transform container;

    private const float healthMinRevealWidth = 32.7f;
    private const float healthMaxRevealWidth = 151f;

    private const float staminaMinRevealWidth = 20f;
    private const float staminaMaxRevealWidth = 129f;

    private RectTransform healthBarFill;
    private RectTransform healthRegenBarFill;
    private RectTransform staminaBarFill;

    private void Awake()
    {
        container = transform.Find("Container");

        healthBarFill = container.Find("Health/Bar/Fill").GetComponent<RectTransform>();
        healthRegenBarFill = container.Find("Health/Bar/Regeneration").GetComponent<RectTransform>();
        staminaBarFill = container.Find("Stamina/Bar/Fill").GetComponent<RectTransform>();
    }

    internal void Toggle(bool isShown)
    {
        container.gameObject.SetActive(isShown);
    }

    private void Update()
    {
        healthBarFill.sizeDelta = healthBarFill.sizeDelta with
        {
            x = Mathf.Lerp(healthMinRevealWidth, healthMaxRevealWidth, Simulacrum.Player.health / 100f)
        };

        staminaBarFill.sizeDelta = staminaBarFill.sizeDelta with
        {
            x = Mathf.Lerp(staminaMinRevealWidth, staminaMaxRevealWidth, Simulacrum.Player.sprintMeter)
        };

        if (Simulacrum.Players.CurrentRegenerationAmount > 0)
        {
            healthRegenBarFill.gameObject.SetActive(true);
            healthRegenBarFill.sizeDelta = healthRegenBarFill.sizeDelta with
            {
                x = Mathf.Lerp(
                    healthMinRevealWidth, healthMaxRevealWidth,
                    (Simulacrum.Player.health + Simulacrum.Players.CurrentRegenerationAmount) / 100f
                )
            };
        }
        else
        {
            healthRegenBarFill.gameObject.SetActive(false);
        }
    }
}