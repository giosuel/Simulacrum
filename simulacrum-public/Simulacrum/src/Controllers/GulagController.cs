using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DunGen;
using DunGen.Adapters;
using DunGen.Graph;
using Simulacrum.Network;
using Simulacrum.Utils;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Simulacrum.Controllers;

public class GulagController : MonoBehaviour
{
    internal event Action onPlayerEnterGulag;
    internal event Action onPlayerExitGulag;

    private GameObject gulagRoot;
    private DungeonFlow gulagFlow;

    private DungeonGenerator gulagGenerator;
    private bool gulagFinishedGenerating;

    private Vector3 gulagSpawnPosition;
    private readonly HashSet<JesterAI> gulagEntities = [];

    private int timesEnteredGulag;

    internal bool IsPlayerInGulag { get; private set; }
    internal bool PlayerDiedIndefinitely { get; private set; }

    private readonly Func<float, float> GulagMapSizeMultiplier = SimUtils.Quadratic(
        0.03f, 0.2f * Simulacrum.GameConfig.General.DifficultyMultiplier.Value, 1
    );

    private void Awake()
    {
        gulagFlow = GenerateGulagFlow();
    }

    internal void EnterGulag()
    {
        timesEnteredGulag++;

        Simulacrum.Props.OnEntitySpawned.OnClientReceived += OnEntitySpawned;

        IsPlayerInGulag = true;
        Simulacrum.Player.isPlayerDead = true;
        Simulacrum.Events.EndEvents();
        GameNetworkManager.Instance.gameHasStarted = true;
        Simulacrum.Player.SwitchToItemSlot(1);

        onPlayerEnterGulag?.Invoke();
        Simulacrum.Interface.SetInfo("");

        StartCoroutine(enterGulagAnimation());
    }

    internal void ExitGulag()
    {
        IsPlayerInGulag = false;

        Simulacrum.Props.OnEntitySpawned.OnClientReceived -= OnEntitySpawned;

        Simulacrum.Players.TeleportPlayer.SendServer(new TeleportPlayerRequest
        {
            PlayerId = Simulacrum.Player.playerClientId
        });

        Simulacrum.Player.health = 100;
        Simulacrum.Player.sprintMeter = 1;
        Simulacrum.Player.insanityLevel = 0;
        HUDManager.Instance.UpdateHealthUI(100, hurtPlayer: false);

        foreach (var gulagEntity in gulagEntities) Destroy(gulagEntity.gameObject);
        gulagEntities.Clear();

        onPlayerExitGulag?.Invoke();

        // Simulacrum.Player.DespawnHeldObject();
        Simulacrum.Player.SwitchToItemSlot(0);
    }

    private static DungeonFlow GenerateGulagFlow()
    {
        var customFlow = Instantiate(RoundManager.Instance.dungeonFlowTypes[0].dungeonFlow);
        customFlow.Length.Min = 6;
        customFlow.Length.Max = 10;
        customFlow.GlobalProps =
        [
            customFlow.GlobalProps[1], customFlow.GlobalProps[2], customFlow.GlobalProps[3],
            customFlow.GlobalProps[4], customFlow.GlobalProps[5], customFlow.GlobalProps[10]
        ];

        var gulagStartTileSet = Instantiate(customFlow.Nodes[0].TileSets[0]);
        gulagStartTileSet.TileWeights.Weights[0].TileSet = gulagStartTileSet;
        gulagStartTileSet.TileWeights.Weights[0].Value = SimAssets.BrackenRoomRef;

        customFlow.Nodes[1] = new GraphNode(customFlow)
        {
            Position = 1,
            Label = "End",
            NodeType = NodeType.Goal,
            TileSets = customFlow.Nodes[0].TileSets
        };

        customFlow.Nodes[0] = new GraphNode(customFlow)
        {
            Position = 0,
            Label = "Start",
            NodeType = NodeType.Start,
            TileSets = [gulagStartTileSet]
        };

        return customFlow;
    }

    internal void DieInGulag()
    {
        if (Simulacrum.Player.health <= 34)
        {
            IsPlayerInGulag = false;
            PlayerDiedIndefinitely = true;
            Simulacrum.Player.KillPlayer(Vector3.zero, spawnBody: false);
            return;
        }

        if (gulagEntities.Count > 0) Simulacrum.Player.movementAudio.PlayOneShot(gulagEntities.ToList()[0].killPlayerSFX);
        Simulacrum.Player.DamagePlayer(33, hasDamageSFX: true);
        Simulacrum.Player.sprintMeter = 1;

        ResetGulagEntities();
        SpawnGulagEntity();
        StartCoroutine(dieInGulagAnimation());
    }

    private IEnumerator enterGulagAnimation()
    {
        Simulacrum.Players.BlackoutFadeIn();

        yield return new WaitForSeconds(0.8f);

        gulagFinishedGenerating = false;

        if (gulagRoot) Destroy(gulagRoot);
        gulagRoot = new GameObject("GulagRoot")
        {
            transform =
            {
                position = GameController.GulagRootPosition
            }
        };
        var runtimeDungeon = Instantiate(GameObject.Find("DungeonGenerator")).GetComponent<RuntimeDungeon>();

        gulagGenerator = runtimeDungeon.Generator;
        runtimeDungeon.Root = gulagRoot;
        runtimeDungeon.Generator.DungeonFlow = gulagFlow;
        runtimeDungeon.Generator.Seed = Random.Range(int.MinValue, int.MaxValue);
        runtimeDungeon.Generator.LengthMultiplier = GulagMapSizeMultiplier(timesEnteredGulag) * 0.6f;
        runtimeDungeon.Generator.OnGenerationStatusChanged += OnGeneratorStatusChanged;
        runtimeDungeon.Generate();

        yield return new WaitUntil(() => gulagFinishedGenerating);

        runtimeDungeon.gameObject.GetComponent<UnityNavMeshAdapter>().BakeFullDungeon(gulagGenerator.CurrentDungeon);

        // Setup exit door interact trigger
        var exitDoor = runtimeDungeon.Generator.CurrentDungeon.allTiles
            .First(tile => tile.gameObject.name == "StartRoom(Clone)")
            .transform.Find("SpawnEntranceTrigger");
        var exitDoorObj = Instantiate(
            exitDoor.GetComponent<SpawnSyncedObject>().spawnPrefab,
            exitDoor.position,
            exitDoor.rotation
        );
        var exitDoorTrigger = exitDoorObj.GetComponent<InteractTrigger>();
        exitDoorTrigger.hoverTip = "Exit the Gulag";

        gulagSpawnPosition = runtimeDungeon.Generator.CurrentDungeon.allTiles
            .First(tile => tile.gameObject.name == "SmallRoom2(Clone)").transform.position;

        SpawnGulagEntity();
        TeleportPlayerToSpawn();

        Simulacrum.Props.SpawnItem.SendServer(new ItemSpawnRequest
        {
            Name = "Pro-flashlight",
            SpawnPosition = GameController.PropPosition(gulagSpawnPosition) + new Vector3(0, 0, 2)
        });

        Simulacrum.Player.HealClientRpc();
        Simulacrum.Player.playerBodyAnimator.SetBool("Limp", value: false);
        Simulacrum.Player.health = 100;
        Simulacrum.Player.sprintMeter = 1;
        Simulacrum.Player.insanityLevel = Simulacrum.Player.maxInsanityLevel * 10f;
        Simulacrum.Player.isPlayerDead = false;
        Simulacrum.Player.criticallyInjured = false;
        Simulacrum.Player.inSpecialInteractAnimation = false;
        HUDManager.Instance.UpdateHealthUI(100, hurtPlayer: false);

        Simulacrum.Players.BlackoutFadeOut();
        yield return new WaitForSeconds(0.5f);
        Simulacrum.Interface.ShowLargeMessage("Welcome to the Gulag", 28, 2);
        ToggleGulagEntities(true);
    }

    private void ResetGulagEntities()
    {
        foreach (var gulagEntity in gulagEntities) {gulagEntity.SwitchToBehaviourState(0);}
    }

    private void OnEntitySpawned(EntitySpawnResponse entitySpawnResponse)
    {
        if (!entitySpawnResponse.Reference.TryGet(out var entityNetObj)) return;
        var entity = entityNetObj.GetComponent<EnemyAI>();
        if (entity is not JesterAI jester) return;

        jester.GetComponentInChildren<Animator>().SetFloat("CrankSpeedMultiplier", gulagEntities.Count);

        jester.enabled = false;
        jester.GetComponentInChildren<NavMeshAgent>().enabled = false;

        jester.farAudio.maxDistance = 25f;
        jester.creatureSFX.maxDistance = 25f;

        gulagEntities.Add(jester);
    }

    private IEnumerator dieInGulagAnimation()
    {
        Simulacrum.Players.BlackoutFadeIn();
        yield return new WaitForSeconds(0.8f);
        TeleportPlayerToSpawn();
        Simulacrum.Players.BlackoutFadeOut();
        yield return new WaitForSeconds(0.8f);

        switch (Simulacrum.Player.health)
        {
            case <= 34:
                Simulacrum.Interface.ShowWarningMessage("YOU DIED!", "You have 1 try left!");
                RoundManager.Instance.PowerSwitchOffClientRpc();
                break;
            case <= 67:
                Simulacrum.Interface.ShowWarningMessage("YOU DIED!", "You have 2 tries left!");
                RoundManager.Instance.FlickerLights(flickerFlashlights: true, disableFlashlights: true);
                break;
        }

        yield return new WaitForSeconds(1.5f);
        ToggleGulagEntities(true);
    }

    private void ToggleGulagEntities(bool isOn)
    {
        foreach (var entity in gulagEntities)
        {
            entity.enabled = isOn;
            entity.GetComponentInChildren<NavMeshAgent>().enabled = isOn;
        }
    }

    private void SpawnGulagEntity()
    {
        Simulacrum.Props.SpawnEntity.SendServer(new EntitySpawnRequest
        {
            Name = "Jester",
            SpawnPosition = GameController.PropPosition(gulagSpawnPosition)
        });
    }

    private void TeleportPlayerToSpawn()
    {
        Simulacrum.Players.TeleportPlayer.SendServer(new TeleportPlayerRequest
        {
            PlayerId = Simulacrum.Player.playerClientId,
            Destination = GameController.PlayerPosition(gulagSpawnPosition)
        });
        Simulacrum.Player.transform.rotation = Quaternion.Euler(0, 180, 0);
    }

    private void OnGeneratorStatusChanged(DungeonGenerator generator, GenerationStatus status)
    {
        if (status == GenerationStatus.Complete)
        {
            gulagFinishedGenerating = true;
            gulagGenerator.OnGenerationStatusChanged -= OnGeneratorStatusChanged;
        }
    }
}