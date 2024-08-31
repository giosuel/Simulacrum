using System.Linq;
using LethalNetworkAPI;
using Simulacrum.Network;
using Unity.Netcode;
using UnityEngine;

namespace Simulacrum;

internal class PropsController : MonoBehaviour
{
    internal LNetworkMessage<ItemSpawnRequest> SpawnItem { get; private set; }
    private LNetworkMessage<ItemSpawnResponse> OnItemSpawned;

    private void Awake()
    {
        SpawnItem = LNetworkMessage<ItemSpawnRequest>.Connect("SpawnItem");
        OnItemSpawned = LNetworkMessage<ItemSpawnResponse>.Connect("OnItemSpawned");

        if (NetworkManager.Singleton.IsHost)
        {
            SpawnItem.OnServerReceived += OnSpawnItem;
        }

        OnItemSpawned.OnClientReceived += OnSpawnItemClient;
    }

    private void OnSpawnItem(ItemSpawnRequest request, ulong clientId)
    {
        var spawningItem = Resources.FindObjectsOfTypeAll<Item>().First(item => item.itemName == request.Name);
        var itemPrefab = spawningItem.spawnPrefab;

        var itemObj = Instantiate(
            itemPrefab,
            request.SpawnPosition,
            Quaternion.identity
        );

        var netObject = itemObj.gameObject.GetComponentInChildren<NetworkObject>();
        netObject.Spawn(destroyWithScene: true);

        OnItemSpawned.SendClients(new ItemSpawnResponse
        {
            Reference = netObject,
            SpawnWithAnimation = request.SpawnWithAnimation
        });
    }

    private static void OnSpawnItemClient(ItemSpawnResponse response)
    {
        if (!response.Reference.TryGet(out var itemNetObj)) return;
        var grabbableItem = itemNetObj.GetComponent<GrabbableObject>();
        grabbableItem.transform.rotation = Quaternion.Euler(grabbableItem.itemProperties.restingRotation);

        if (response.SpawnWithAnimation)
        {
            var itemTransform = grabbableItem.transform;
            itemTransform.position = grabbableItem.transform.position + Vector3.up;
            grabbableItem.startFallingPosition = itemTransform.position;
            if (grabbableItem.transform.parent)
            {
                grabbableItem.startFallingPosition = grabbableItem.transform.parent.InverseTransformPoint(
                    grabbableItem.startFallingPosition
                );
            }

            grabbableItem.FallToGround();

            if (grabbableItem.itemProperties.dropSFX)
            {
                Simulacrum.Player.itemAudio.PlayOneShot(grabbableItem.itemProperties.dropSFX);
            }
        }

        if (Simulacrum.SetupScene.IsInSetupScene)
        {
            grabbableItem.itemProperties.canBeGrabbedBeforeGameStart = true;
        }
    }
}