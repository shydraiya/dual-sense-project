using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MinimapController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public string enemyTag = "Enemy";

    [Header("Camera")]
    public float minimapHeight = 22f;
    public float minimapSize = 18f;
    public LayerMask minimapCullingMask = ~0;
    public Color cameraBackgroundColor = new Color(0.05f, 0.1f, 0.12f, 1f);

    [Header("UI")]
    public Vector2 panelSize = new Vector2(220f, 220f);
    public Vector2 screenOffset = new Vector2(-24f, -24f);
    public Color panelColor = new Color(0f, 0f, 0f, 0.4f);
    public Color borderColor = new Color(1f, 1f, 1f, 0.9f);
    public float borderThickness = 3f;

    [Header("Markers")]
    public Color playerMarkerColor = new Color(0.2f, 1f, 0.45f, 1f);
    public Color enemyMarkerColor = new Color(1f, 0.25f, 0.25f, 1f);
    public Vector2 playerMarkerSize = new Vector2(18f, 18f);
    public Vector2 enemyMarkerSize = new Vector2(12f, 12f);
    public float edgePadding = 10f;

    private Camera minimapCamera;
    private RenderTexture minimapTexture;
    private Canvas minimapCanvas;
    private RectTransform mapRect;
    private RectTransform markerLayer;
    private RectTransform playerMarker;
    private Sprite markerSprite;
    private Texture2D markerTexture;
    private readonly Dictionary<Transform, RectTransform> enemyMarkers = new Dictionary<Transform, RectTransform>();

    private void Awake()
    {
        if (target == null)
        {
            target = transform;
        }
    }

    private void Start()
    {
        CreateMarkerSprite();
        BuildMinimapCamera();
        BuildMinimapUI();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        UpdateMinimapCamera();
        UpdateEnemyMarkers();
    }

    private void OnDestroy()
    {
        if (minimapTexture != null)
        {
            minimapTexture.Release();
        }

        if (Application.isPlaying)
        {
            Destroy(markerSprite);
            Destroy(markerTexture);
            Destroy(minimapTexture);
            Destroy(minimapCamera != null ? minimapCamera.gameObject : null);
            Destroy(minimapCanvas != null ? minimapCanvas.gameObject : null);
        }
    }

    private void CreateMarkerSprite()
    {
        markerTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        markerTexture.SetPixel(0, 0, Color.white);
        markerTexture.Apply();
        markerSprite = Sprite.Create(markerTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
    }

    private void BuildMinimapCamera()
    {
        GameObject cameraObject = new GameObject("MinimapCamera");
        cameraObject.transform.SetParent(transform, false);

        minimapCamera = cameraObject.AddComponent<Camera>();
        minimapCamera.orthographic = true;
        minimapCamera.orthographicSize = minimapSize;
        minimapCamera.nearClipPlane = 0.1f;
        minimapCamera.farClipPlane = minimapHeight + 50f;
        minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        minimapCamera.backgroundColor = cameraBackgroundColor;
        minimapCamera.cullingMask = minimapCullingMask;
        minimapCamera.depth = 10f;

        minimapTexture = new RenderTexture(512, 512, 16)
        {
            name = "MinimapTexture"
        };

        minimapCamera.targetTexture = minimapTexture;
        UpdateMinimapCamera();
    }

    private void BuildMinimapUI()
    {
        GameObject canvasObject = new GameObject("MinimapCanvas");
        minimapCanvas = canvasObject.AddComponent<Canvas>();
        minimapCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        minimapCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = minimapCanvas.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        GameObject frameObject = CreateUIObject("MinimapFrame", canvasObject.transform);
        RectTransform frameRect = frameObject.AddComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(1f, 1f);
        frameRect.anchorMax = new Vector2(1f, 1f);
        frameRect.pivot = new Vector2(1f, 1f);
        frameRect.sizeDelta = panelSize + Vector2.one * borderThickness * 2f;
        frameRect.anchoredPosition = screenOffset;

        Image frameImage = frameObject.AddComponent<Image>();
        frameImage.color = borderColor;
        frameImage.sprite = markerSprite;
        frameImage.type = Image.Type.Sliced;

        GameObject mapObject = CreateUIObject("Minimap", frameObject.transform);
        mapRect = mapObject.AddComponent<RectTransform>();
        mapRect.anchorMin = new Vector2(0.5f, 0.5f);
        mapRect.anchorMax = new Vector2(0.5f, 0.5f);
        mapRect.pivot = new Vector2(0.5f, 0.5f);
        mapRect.sizeDelta = panelSize;
        mapRect.anchoredPosition = Vector2.zero;

        Image backgroundImage = mapObject.AddComponent<Image>();
        backgroundImage.color = panelColor;
        backgroundImage.sprite = markerSprite;

        GameObject textureObject = CreateUIObject("MinimapTexture", mapObject.transform);
        RectTransform textureRect = textureObject.AddComponent<RectTransform>();
        textureRect.anchorMin = Vector2.zero;
        textureRect.anchorMax = Vector2.one;
        textureRect.offsetMin = Vector2.zero;
        textureRect.offsetMax = Vector2.zero;

        RawImage rawImage = textureObject.AddComponent<RawImage>();
        rawImage.texture = minimapTexture;
        rawImage.color = Color.white;

        GameObject markerLayerObject = CreateUIObject("MarkerLayer", mapObject.transform);
        markerLayer = markerLayerObject.AddComponent<RectTransform>();
        markerLayer.anchorMin = Vector2.zero;
        markerLayer.anchorMax = Vector2.one;
        markerLayer.offsetMin = Vector2.zero;
        markerLayer.offsetMax = Vector2.zero;

        GameObject playerMarkerObject = CreateUIObject("PlayerMarker", markerLayer);
        playerMarker = playerMarkerObject.AddComponent<RectTransform>();
        playerMarker.anchorMin = new Vector2(0.5f, 0.5f);
        playerMarker.anchorMax = new Vector2(0.5f, 0.5f);
        playerMarker.pivot = new Vector2(0.5f, 0.5f);
        playerMarker.sizeDelta = playerMarkerSize;
        playerMarker.anchoredPosition = Vector2.zero;

        Image playerImage = playerMarkerObject.AddComponent<Image>();
        playerImage.color = playerMarkerColor;
        playerImage.sprite = markerSprite;
        playerImage.type = Image.Type.Simple;
    }

    private void UpdateMinimapCamera()
    {
        if (minimapCamera == null || target == null)
        {
            return;
        }

        Vector3 targetPosition = target.position;
        minimapCamera.transform.position = targetPosition + Vector3.up * minimapHeight;
        minimapCamera.transform.rotation = Quaternion.Euler(90f, target.eulerAngles.y, 0f);
    }

    private void UpdateEnemyMarkers()
    {
        if (markerLayer == null || target == null)
        {
            return;
        }

        HashSet<Transform> activeEnemies = new HashSet<Transform>();
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        float usableHalfWidth = (mapRect.rect.width * 0.5f) - edgePadding;
        float usableHalfHeight = (mapRect.rect.height * 0.5f) - edgePadding;
        float worldRange = Mathf.Max(0.01f, minimapSize);

        foreach (GameObject enemyObject in enemies)
        {
            if (enemyObject == null)
            {
                continue;
            }

            Transform enemy = enemyObject.transform;
            activeEnemies.Add(enemy);

            if (!enemyMarkers.TryGetValue(enemy, out RectTransform marker))
            {
                marker = CreateEnemyMarker(enemy);
            }

            Vector3 localOffset = target.InverseTransformPoint(enemy.position);
            Vector2 mapPosition = new Vector2(
                (localOffset.x / worldRange) * usableHalfWidth,
                (localOffset.z / worldRange) * usableHalfHeight);

            bool isWithinRange =
                Mathf.Abs(mapPosition.x) <= usableHalfWidth &&
                Mathf.Abs(mapPosition.y) <= usableHalfHeight;

            marker.anchoredPosition = mapPosition;
            marker.gameObject.SetActive(enemy.gameObject.activeInHierarchy && isWithinRange);
        }

        RemoveMissingMarkers(activeEnemies);
    }

    private RectTransform CreateEnemyMarker(Transform enemy)
    {
        GameObject markerObject = CreateUIObject(enemy.name + "_Marker", markerLayer);
        RectTransform markerRect = markerObject.AddComponent<RectTransform>();
        markerRect.anchorMin = new Vector2(0.5f, 0.5f);
        markerRect.anchorMax = new Vector2(0.5f, 0.5f);
        markerRect.pivot = new Vector2(0.5f, 0.5f);
        markerRect.sizeDelta = enemyMarkerSize;

        Image markerImage = markerObject.AddComponent<Image>();
        markerImage.color = enemyMarkerColor;
        markerImage.sprite = markerSprite;

        enemyMarkers.Add(enemy, markerRect);
        return markerRect;
    }

    private void RemoveMissingMarkers(HashSet<Transform> activeEnemies)
    {
        List<Transform> removedEnemies = null;

        foreach (KeyValuePair<Transform, RectTransform> entry in enemyMarkers)
        {
            if (entry.Key != null && activeEnemies.Contains(entry.Key))
            {
                continue;
            }

            if (entry.Value != null)
            {
                Destroy(entry.Value.gameObject);
            }

            removedEnemies ??= new List<Transform>();
            removedEnemies.Add(entry.Key);
        }

        if (removedEnemies == null)
        {
            return;
        }

        foreach (Transform removedEnemy in removedEnemies)
        {
            enemyMarkers.Remove(removedEnemy);
        }
    }

    private GameObject CreateUIObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName);
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }
}
