using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Simulacrum.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Simulacrum.Interface;

public class MapUI : MonoBehaviour
{
    private GameObject markerTemplate;
    private Transform markerContainer;
    private RectTransform grid;
    private Canvas canvas;

    private Transform container;

    private Dictionary<EnemyAI, RectTransform> markers = [];
    private EnemyAI[] entitiesOnMap = [];

    private readonly SimTimer entityUpdateTimer = SimTimer.ForInterval(1f);

    private const float mapSize = 80f;
    private const float mapGridTileSize = 25f;
    private float mapPanelSize;
    private float scaleFactor;

    private void Awake()
    {
        container = transform.Find("Container");

        canvas = transform.parent.parent.GetComponent<Canvas>();
        markerContainer = container.Find("Circle/Markers");
        grid = container.Find("Circle/Grid").GetComponent<RectTransform>();

        markerTemplate = markerContainer.Find("Marker").gameObject;
        markerTemplate.SetActive(false);

        mapPanelSize = 130f * canvas.scaleFactor;
        scaleFactor = mapPanelSize / mapSize;
    }

    internal void Toggle(bool isShown)
    {
        container.gameObject.SetActive(isShown);
    }

    private void Update()
    {
        if (entityUpdateTimer.Tick())
        {
            entitiesOnMap = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
            var markersUpdated = new Dictionary<EnemyAI, RectTransform>();
            markers.Do(entry =>
            {
                if (!entry.Key) return;
                markersUpdated[entry.Key] = entry.Value;

                if (entry.Key.isEnemyDead)
                {
                    var image = entry.Value.GetComponent<Image>();
                    image.sprite = SimAssets.SkullSprite;
                    image.color = new Color(0.5f, 0.5f, 0.5f);
                    entry.Value.sizeDelta = Vector2.one * 7f;
                }
                else if (!entry.Key.enemyType.canDie ||
                         Simulacrum.GameConfig.Entities.IsUnkillable(entry.Key.enemyType.enemyName))
                {
                    var image = entry.Value.GetComponent<Image>();
                    image.color = new Color(1f, 0.64f, 0f);
                }
            });

            markers = markersUpdated;
        }

        if (entitiesOnMap.Length <= 0) return;

        foreach (var entity in entitiesOnMap.Where(entity => entity))
        {
            var entityOffset = (entity.transform.position - Simulacrum.Player.transform.position) * scaleFactor;
            var anchorPosition = new Vector2(entityOffset.x, entityOffset.z);

            var marker = GetOrCreateMarker(entity);
            marker.anchoredPosition = Quaternion.AngleAxis(
                Simulacrum.Player.transform.rotation.eulerAngles.y,
                new Vector3(0, 0, 1)
            ) * anchorPosition;
        }
    }

    private void LateUpdate()
    {
        var rawPosition = Simulacrum.Player.transform.position * scaleFactor;
        var tiledPosition = new Vector2(rawPosition.x % mapGridTileSize, rawPosition.z % mapGridTileSize);

        grid.anchoredPosition = Quaternion.AngleAxis(
            Simulacrum.Player.transform.rotation.eulerAngles.y,
            new Vector3(0, 0, 1)
        ) * new Vector3(-tiledPosition.x, -tiledPosition.y, 0);

        grid.rotation = Quaternion.Euler(0, 0, Simulacrum.Player.gameplayCamera.transform.rotation.eulerAngles.y);
    }

    private RectTransform GetOrCreateMarker(EnemyAI entity)
    {
        if (markers.TryGetValue(entity, out var entityMarker)) return entityMarker;

        var marker = Instantiate(markerTemplate, markerContainer).GetComponent<RectTransform>();
        marker.gameObject.SetActive(true);
        markers[entity] = marker;

        return marker;
    }
}