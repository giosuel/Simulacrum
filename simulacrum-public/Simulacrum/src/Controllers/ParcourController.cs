using System.Collections;
using System.Linq;
using Simulacrum.Network;
using Simulacrum.Objects;
using Simulacrum.Utils;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace Simulacrum.Controllers;

public class ParcourController : MonoBehaviour
{
    // Platform alpha clip (0 = Revealed, 1 = Hidden)
    private static readonly int ClipThreshold = Shader.PropertyToID("_ClipThreshold");
    private readonly Vector3 platformOrigin = GameController.PlayerPosition(GameController.ParcourPlatformPosition);
    private readonly Vector3 startOrigin = GameController.PlayerPosition(GameController.ParcourStartPosition);

    private const int minRooms = 2;
    private const int maxRooms = 6;
    private const int minSegments = 12;
    private const int maxSegments = 17;
    private const float minDistance = 7.5f;
    private const float maxDistance = 9.5f;
    private const float minVerticalOffset = 2.5f;
    private const float maxVerticalOffset = 6.5f;
    private const float stretch = 26f;
    private const float angleLimit = 40f;
    private const float platformFadeDuration = 0.5f;

    private int roomCount;
    private int currentRoom;
    private ParcourExit parcourExit;
    private ParcourSegment[] segments;
    private bool playerInParcour;
    private Volume parcourVolume;
    private int currentPlatform;

    private float sunLightStartIntensity;
    private float indirectLightStartIntensity;

    private void Awake()
    {
        var parcourVolumeObj = new GameObject("ParcourVolume");
        parcourVolumeObj.transform.SetParent(transform);
        parcourVolume = parcourVolumeObj.AddComponent<Volume>();
        parcourVolume.isGlobal = true;
        parcourVolume.weight = 0;
        parcourVolume.priority = 20;
        parcourVolume.profile = SimAssets.ParcourStartProfile;
    }

    internal void PlayerEnter()
    {
        sunLightStartIntensity = TimeOfDay.Instance.sunDirect.intensity;
        indirectLightStartIntensity = TimeOfDay.Instance.sunIndirect.intensity;
        TimeOfDay.Instance.sunDirect.intensity = 0;
        TimeOfDay.Instance.sunIndirect.intensity = 0;

        parcourVolume.weight = 1;

        currentRoom = 0;
        roomCount = Random.Range(minRooms, maxRooms);

        var startRoom = Instantiate(SimAssets.ParcourStart, transform);
        startRoom.transform.position = startOrigin;

        parcourExit = startRoom.transform.Find("StartDoor/Mesh").gameObject.AddComponent<ParcourExit>();
        parcourExit.Renderer.material.SetFloat(ClipThreshold, 1);
        parcourExit.OnCollision += PlayerAdvance;

        Simulacrum.Players.TeleportPlayer.SendServer(new TeleportPlayerRequest
        {
            PlayerId = Simulacrum.Player.playerClientId,
            Destination = startRoom.transform.Find("PlayerSpawn").transform.position
        });
    }

    private void PlayerAdvance()
    {
        parcourVolume.profile = SimAssets.ParcourProfile;
        
        Simulacrum.Log.LogInfo("PLAYER ADVANCE");
        if (currentRoom >= roomCount)
        {
            PlayerExit();
            return;
        }

        currentRoom++;

        playerInParcour = true;
        currentPlatform = -1;
        BuildSegments();

        Simulacrum.Players.TeleportPlayer.SendServer(new TeleportPlayerRequest
        {
            PlayerId = Simulacrum.Player.playerClientId,
            Destination = platformOrigin
        });
    }

    private IEnumerator playerExitAnimation()
    {
        Simulacrum.Players.WhiteoutFadeIn();

        Simulacrum.Players.TeleportPlayer.SendServer(new TeleportPlayerRequest
        {
            PlayerId = Simulacrum.Player.playerClientId
        });
        TimeOfDay.Instance.sunDirect.intensity = sunLightStartIntensity;
        TimeOfDay.Instance.sunIndirect.intensity = indirectLightStartIntensity;

        parcourVolume.weight = 0;

        yield return new WaitForSeconds(1f);

        Simulacrum.Players.WhiteoutFadeOut();
    }

    private void PlayerExit()
    {
        playerInParcour = false;
        Simulacrum.Player.transform.SetParent(StartOfRound.Instance.playersContainer, true);

        StartCoroutine(playerExitAnimation());

        DestroySegments();
        parcourExit.Destroy();
    }

    private void BuildSegments()
    {
        var pathLength = Random.Range(minSegments, maxSegments);

        segments = GeneratePath(pathLength).Select((segment, index) =>
        {
            var platformObj = Instantiate(SimAssets.PathSegment, transform);
            platformObj.transform.position = segment.Position;
            var platform = platformObj.transform.Find("Mesh").gameObject.AddComponent<ParcourPlatform>();
            platform.OnEnterPlatform += () => SwitchPlatform(index);

            var platformRenderer = platformObj.transform.Find("Mesh").GetComponent<MeshRenderer>();
            // Reveal the first platform, hide all others
            platformRenderer.material.SetFloat(ClipThreshold, index == 0 ? 0 : 1);

            return segment with
            {
                Renderer = platformRenderer
            };
        }).ToArray();

        var parcourExitObj = Instantiate(SimAssets.ParcourExit, segments.Last().Position, quaternion.identity);
        parcourExit = parcourExitObj.transform.Find("Mesh").gameObject.AddComponent<ParcourExit>();
        parcourExit.Renderer.material.SetFloat(ClipThreshold, 1);
        parcourExit.OnCollision += PlayerAdvance;
    }

    private void SwitchPlatform(int platformIndex)
    {
        if (platformIndex == currentPlatform) return;
        currentPlatform = platformIndex;

        Simulacrum.Log.LogInfo("SWITCHING PLATFORM");
        if (platformIndex > 1)
        {
            StartCoroutine(platformFadeOut(segments[platformIndex - 2].Renderer));
        }

        if (platformIndex < segments.Length - 2)
        {
            StartCoroutine(platformFadeIn(segments[platformIndex + 1].Renderer));
        }

        // Fade in exit door if player steps on second last platform
        if (platformIndex == segments.Length - 2)
        {
            StartCoroutine(platformFadeIn(parcourExit.Renderer));
        }
    }

    private static IEnumerator platformFadeIn(MeshRenderer renderer)
    {
        var elapsedTime = 0f;

        while (elapsedTime < platformFadeDuration)
        {
            var t = elapsedTime / platformFadeDuration;

            renderer.material.SetFloat(ClipThreshold, 1 - t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    private static IEnumerator platformFadeOut(MeshRenderer renderer)
    {
        var elapsedTime = 0f;

        while (elapsedTime < platformFadeDuration)
        {
            var t = elapsedTime / platformFadeDuration;

            renderer.material.SetFloat(ClipThreshold, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    private ParcourSegment[] GeneratePath(int length)
    {
        var pathSegments = new ParcourSegment[length];

        for (var i = 0; i < length; i++)
        {
            var angle = Mathf.Round(
                Mathf.Clamp(
                    Mathf.Lerp(-140, 140, Random.Range(0f, 1f)),
                    -90,
                    90
                ) / angleLimit
            ) * stretch;
            var distanceToNext = Random.Range(minDistance, maxDistance);
            var verticalOffset = Random.Range(minVerticalOffset, maxVerticalOffset);
            var localPosition = Quaternion.Euler(0, angle, 0)
                                * new Vector3(0, 0, distanceToNext)
                                + Vector3.down * verticalOffset;

            pathSegments[i] = new ParcourSegment
            {
                Position = i == 0 ? platformOrigin : localPosition + pathSegments[i - 1].Position,
                DistanceToNext = distanceToNext
            };
        }

        return pathSegments;
    }

    private void DestroySegments()
    {
        foreach (var segment in segments) Destroy(segment.Renderer.transform.parent.gameObject);
        segments = null;
    }

    private void Update()
    {
        if (!playerInParcour) return;

        Simulacrum.Log.LogInfo("PLAYER IS IN PARCOUR");

        var playerPosition = Simulacrum.Player.transform.position;

        if (playerPosition.y < platformOrigin.y - 100f)
        {
            Simulacrum.Player.BreakLegsSFXClientRpc();
            Simulacrum.Player.KillPlayer(Vector3.zero, false, CauseOfDeath.Abandoned);
            playerInParcour = false;
        }
    }
}

internal readonly struct ParcourSegment
{
    internal Vector3 Position { get; init; }
    internal float DistanceToNext { get; init; }
    internal MeshRenderer Renderer { get; init; }
}