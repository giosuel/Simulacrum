using UnityEngine;

namespace Simulacrum.Controllers;

internal class GameController : MonoBehaviour
{
    internal static Vector3 SetupTilePosition => new(200 + 40 * Simulacrum.Player.playerClientId, -50, 200);
    internal static Vector3 ParcourPlatformPosition => new(400 + 40 * Simulacrum.Player.playerClientId, -500, 400);
    internal static Vector3 ParcourStartPosition => new(400 + 40 * Simulacrum.Player.playerClientId, -400, 400);
    internal static Vector3 GulagRootPosition => new(-300, -200f - 50f * Simulacrum.Player.playerClientId, 300);
    internal static Vector3 PropPosition(Vector3 tilePosition) => tilePosition - new Vector3(0, 0, 12);
    internal static Vector3 PlayerPosition(Vector3 tilePosition) => tilePosition - new Vector3(0, 0, 1);
}