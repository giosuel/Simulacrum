using Simulacrum.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Simulacrum.Interface;

public class LoadoutUI : MonoBehaviour
{
    private Transform container;

    private GameObject infiniteAmmo;
    private GameObject finiteAmmo;

    private TMP_Text loadedAmmo;
    private TMP_Text storageAmmo;

    private Image weaponImage;

    private void Awake()
    {
        container = transform.Find("Container");

        weaponImage = container.Find("Weapon/Image").GetComponent<Image>();

        infiniteAmmo = container.Find("Ammo/Infinite").gameObject;
        finiteAmmo = container.Find("Ammo/Finite").gameObject;

        loadedAmmo = container.Find("Ammo/Finite/AmmoText").GetComponent<TMP_Text>();
        storageAmmo = container.Find("Ammo/Finite/ClipText").GetComponent<TMP_Text>();

        Simulacrum.Players.OnItemEquipped += OnItemEquipped;
        Simulacrum.Players.OnShotgunShot += OnShotgunShot;
        Simulacrum.Players.OnShotgunReloadStart += OnShotgunReloadStart;
        Simulacrum.Players.OnShotgunReloadEnd += OnShotgunReloadEnd;
    }

    internal void Toggle(bool isShown)
    {
        container.gameObject.SetActive(isShown);
    }

    private void OnItemEquipped(GrabbableObject item)
    {
        switch (item)
        {
            case Shovel:
                weaponImage.sprite = SimAssets.ShovelSprite;
                infiniteAmmo.SetActive(true);
                finiteAmmo.SetActive(false);
                break;
            case ShotgunItem:
                weaponImage.sprite = SimAssets.ShotgunSprite;
                infiniteAmmo.SetActive(false);
                finiteAmmo.SetActive(true);
                break;
            default:
                return;
        }
        Toggle(true);
    }

    private void OnShotgunShot(ShotgunItem gun)
    {
        Toggle(true);
        loadedAmmo.text = gun.shellsLoaded.ToString();
    }

    private void OnShotgunReloadStart(ShotgunItem gun)
    {
        Toggle(true);
        loadedAmmo.text = "..";
    }

    private void OnShotgunReloadEnd(ShotgunItem gun)
    {
        Toggle(true);
        loadedAmmo.text = gun.shellsLoaded.ToString();
    }
}