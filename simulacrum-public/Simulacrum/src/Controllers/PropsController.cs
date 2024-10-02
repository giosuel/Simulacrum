using System.Collections.Generic;
using System.Linq;
using LethalNetworkAPI;
using Simulacrum.Network;
using Simulacrum.Utils;
using Unity.Netcode;
using UnityEngine;

namespace Simulacrum.Controllers;

internal class PropsController : MonoBehaviour
{
    internal LNetworkMessage<ItemSpawnRequest> SpawnItem { get; private set; }
    internal LNetworkMessage<EntitySpawnRequest> SpawnEntity { get; private set; }
    private LNetworkMessage<ItemSpawnResponse> OnItemSpawned;
    internal LNetworkMessage<EntitySpawnResponse> OnEntitySpawned;

    private HashSet<EnemyType> loadedEntities;

    private void Awake()
    {
        SpawnItem = LNetworkMessage<ItemSpawnRequest>.Connect("SpawnItem");
        SpawnEntity = LNetworkMessage<EntitySpawnRequest>.Connect("SpawnEntity");
        OnItemSpawned = LNetworkMessage<ItemSpawnResponse>.Connect("OnItemSpawned");
        OnEntitySpawned = LNetworkMessage<EntitySpawnResponse>.Connect("OnEntitySpawned");

        if (NetworkManager.Singleton.IsHost)
        {
            SpawnItem.OnServerReceived += OnSpawnItem;
            SpawnEntity.OnServerReceived += OnSpawnEntity;
        }

        OnItemSpawned.OnClientReceived += OnSpawnItemClient;

        loadedEntities = Resources.FindObjectsOfTypeAll<EnemyType>().ToHashSet();
        Simulacrum.GameConfig.Sheets.CheckEntitySheets(loadedEntities);
    }

    [SimAttributes.HostOnly]
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

    [SimAttributes.HostOnly]
    private void OnSpawnEntity(EntitySpawnRequest request, ulong clientId)
    {
        var enemyPrefab = loadedEntities.First(entity => entity.enemyName == request.Name).enemyPrefab;

        for (var i = 0; i < request.Amount; i++)
        {
            var entityObj = Instantiate(enemyPrefab, request.SpawnPosition, Quaternion.identity);
            var netObject = entityObj.gameObject.GetComponentInChildren<NetworkObject>();
            netObject.Spawn(destroyWithScene: true);
            OnEntitySpawned.SendClients(new EntitySpawnResponse
            {
                Reference = netObject
            });
        }
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