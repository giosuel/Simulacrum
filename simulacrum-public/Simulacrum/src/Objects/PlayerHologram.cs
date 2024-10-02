using GameNetcodeStuff;
using Simulacrum.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.VFX;

namespace Simulacrum.Objects;

public class PlayerHologram : MonoBehaviour
{
    private InteractTrigger trigger;
    private PlayerControllerB hologramPlayer;
    private VisualEffect disappearEffect;

    private GameObject hologramObj;
    private GameObject hologramBeams;
    private GameObject hologramName;

    internal void Init(PlayerControllerB player)
    {
        hologramPlayer = player;

        trigger = transform.Find("Trigger").GetComponent<InteractTrigger>();
        trigger.onInteract.AddListener(this, GetType().GetMethod("OnInteract"));

        hologramName = transform.Find("PlayerUsernameCanvas/Text").gameObject;
        hologramName.GetComponent<TMP_Text>().text = player.playerUsername;
        trigger.hoverTip = $"Revive {player.playerUsername}";

        hologramObj = transform.Find("ScavengerModel/Mesh").gameObject;
        hologramBeams = transform.Find("Beams").gameObject;
        disappearEffect = transform.Find("Particles").GetComponent<VisualEffect>();
    }

    public void OnInteract(PlayerControllerB playerTriggered)
    {
        Simulacrum.Log.LogInfo("Player interacted with hologram");
        Simulacrum.Player.statusEffectAudio.PlayOneShot(SimAssets.ReviveBubbleSFX);

        var reviveBubble = Instantiate(SimAssets.ReviveBubble, transform.position, Quaternion.identity);
        reviveBubble.SetActive(true);
        reviveBubble.AddComponent<ReviveBubble>();

        hologramObj.SetActive(false);
        hologramBeams.SetActive(false);
        hologramName.transform.parent.gameObject.SetActive(false);
        disappearEffect.Play();
    }

    private void Update()
    {
        transform.LookAt(Simulacrum.Player.gameplayCamera.transform);
        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
    }
}