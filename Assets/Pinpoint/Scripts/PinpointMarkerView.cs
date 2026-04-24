using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PinpointMarkerView : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material selectedMaterial;

    [Header("Severity Colors")]
    [SerializeField] private Color lowSeverityColor = new(0.18f, 0.72f, 0.32f, 1f);
    [SerializeField] private Color mediumSeverityColor = new(1f, 0.86f, 0.2f, 1f);
    [SerializeField] private Color highSeverityColor = new(1f, 0.5f, 0.12f, 1f);
    [SerializeField] private Color criticalSeverityColor = new(1f, 0.16f, 0.12f, 1f);

    [Header("Status Badge")]
    [SerializeField] private bool hideStatusBadge;
    [SerializeField] private Color statusBadgeBackgroundColor = new(1f, 1f, 1f, 1f);
    [SerializeField] private Color statusBadgeTextColor = Color.black;
    [SerializeField] private float statusBadgeSize = 0.18f;
    [SerializeField] private float statusBadgeSurfaceLift = 0.04f;
    [SerializeField] private float statusBadgeVerticalOffset = 0.06f;

    [Header("Selection")]
    [SerializeField] private Color selectedMarkerColor = Color.white;

    private const float DefaultStatusBadgeSize = 0.18f;
    private const float DefaultStatusBadgeSurfaceLift = 0.04f;
    private const float DefaultStatusBadgeVerticalOffset = 0.06f;
    private const int BadgeTextureSize = 128;
    private const float BadgePixelsPerUnit = 128f;

    private static readonly Color DefaultLowSeverityColor = new(0.18f, 0.72f, 0.32f, 1f);
    private static readonly Color DefaultMediumSeverityColor = new(1f, 0.86f, 0.2f, 1f);
    private static readonly Color DefaultHighSeverityColor = new(1f, 0.5f, 0.12f, 1f);
    private static readonly Color DefaultCriticalSeverityColor = new(1f, 0.16f, 0.12f, 1f);
    private static readonly Color DefaultStatusBadgeBackgroundColor = Color.white;
    private static readonly Color DefaultStatusBadgeTextColor = Color.black;
    private static readonly Color DefaultSelectedMarkerColor = Color.white;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock _propertyBlock;
    private bool _isSelected;
    private SpriteRenderer _statusBadgeRenderer;
    private readonly Dictionary<string, Sprite> _badgeSprites = new();

    private void Awake()
    {
        EnsurePropertyBlock();

        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        DestroyLegacyStatusVisuals();
        RefreshVisuals();
    }

    private void Reset()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();
    }

    private void LateUpdate()
    {
        UpdateStatusBadgeBillboard();
    }

    private void OnDestroy()
    {
        DestroyBadgeSprites();
    }

    public void SetSelected(bool isSelected)
    {
        _isSelected = isSelected;
        RefreshVisuals();
    }

    public void RefreshVisuals()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        var model = GetComponent<PinpointMarkerModel>();
        MarkerSeverity severity = model != null ? model.Severity : MarkerSeverity.Medium;
        MarkerStatus status = model != null ? model.Status : MarkerStatus.Open;

        ApplySeverityColor(GetSeverityColor(severity));
        RefreshStatusBadge(GetStatusLetter(status));
    }

    private void ApplySeverityColor(Color color)
    {
        if (targetRenderer == null)
            return;

        EnsurePropertyBlock();

        Material targetMaterial = _isSelected && selectedMaterial != null ? selectedMaterial : normalMaterial;
        if (targetMaterial != null && targetRenderer.sharedMaterial != targetMaterial)
            targetRenderer.sharedMaterial = targetMaterial;

        if (_isSelected)
            color = VisibleOrDefault(selectedMarkerColor, DefaultSelectedMarkerColor);

        targetRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor(BaseColorId, color);
        _propertyBlock.SetColor(ColorId, color);
        targetRenderer.SetPropertyBlock(_propertyBlock);
    }

    private void EnsurePropertyBlock()
    {
        _propertyBlock ??= new MaterialPropertyBlock();
    }

    private void RefreshStatusBadge(string statusLetter)
    {
        if (hideStatusBadge)
        {
            SetStatusBadgeVisible(false);
            return;
        }

        _statusBadgeRenderer = EnsureStatusBadgeRenderer();
        _statusBadgeRenderer.sprite = GetBadgeSprite(statusLetter);
        _statusBadgeRenderer.color = Color.white;
        SetStatusBadgeVisible(true);
        UpdateStatusBadgeBillboard();
    }

    private SpriteRenderer EnsureStatusBadgeRenderer()
    {
        if (_statusBadgeRenderer != null)
            return _statusBadgeRenderer;

        Transform existingBadge = transform.Find("StatusBadge");
        if (existingBadge != null)
            _statusBadgeRenderer = existingBadge.GetComponent<SpriteRenderer>();

        if (_statusBadgeRenderer == null)
        {
            var badgeObject = new GameObject("StatusBadge");
            badgeObject.transform.SetParent(transform, false);
            _statusBadgeRenderer = badgeObject.AddComponent<SpriteRenderer>();
        }

        _statusBadgeRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _statusBadgeRenderer.receiveShadows = false;
        _statusBadgeRenderer.sortingOrder = 20;

        return _statusBadgeRenderer;
    }

    private Sprite GetBadgeSprite(string statusLetter)
    {
        if (_badgeSprites.TryGetValue(statusLetter, out Sprite sprite))
            return sprite;

        Texture2D texture = CreateBadgeTexture(statusLetter);
        sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            BadgePixelsPerUnit);
        sprite.name = $"StatusBadge_{statusLetter}";
        _badgeSprites[statusLetter] = sprite;
        return sprite;
    }

    private Texture2D CreateBadgeTexture(string statusLetter)
    {
        Color background = VisibleOrDefault(statusBadgeBackgroundColor, DefaultStatusBadgeBackgroundColor);
        Color text = VisibleOrDefault(statusBadgeTextColor, DefaultStatusBadgeTextColor);

        var texture = new Texture2D(BadgeTextureSize, BadgeTextureSize, TextureFormat.RGBA32, false)
        {
            name = $"StatusBadgeTexture_{statusLetter}",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color32[BadgeTextureSize * BadgeTextureSize];
        DrawBadgeBackground(pixels, background);
        DrawBadgeLetter(pixels, statusLetter, text);
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private static void DrawBadgeBackground(Color32[] pixels, Color color)
    {
        Color32 fill = color;
        Color32 clear = new(0, 0, 0, 0);
        float center = (BadgeTextureSize - 1) * 0.5f;
        float radius = BadgeTextureSize * 0.43f;
        float softEdge = 1.5f;

        for (int y = 0; y < BadgeTextureSize; y++)
        {
            for (int x = 0; x < BadgeTextureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01((radius - distance) / softEdge);
                Color32 pixel = alpha > 0f ? fill : clear;
                pixel.a = (byte)Mathf.RoundToInt(pixel.a * alpha);
                pixels[y * BadgeTextureSize + x] = pixel;
            }
        }
    }

    private static void DrawBadgeLetter(Color32[] pixels, string statusLetter, Color color)
    {
        string[] pattern = GetLetterPattern(statusLetter);
        Color32 fill = color;
        int cellSize = 11;
        int letterWidth = pattern[0].Length * cellSize;
        int letterHeight = pattern.Length * cellSize;
        int startX = (BadgeTextureSize - letterWidth) / 2;
        int startY = (BadgeTextureSize - letterHeight) / 2 + 2;

        for (int row = 0; row < pattern.Length; row++)
        {
            for (int column = 0; column < pattern[row].Length; column++)
            {
                if (pattern[row][column] != '1')
                    continue;

                int x = startX + column * cellSize;
                int y = BadgeTextureSize - startY - (row + 1) * cellSize;
                FillRect(pixels, x, y, cellSize, cellSize, fill);
            }
        }
    }

    private static void FillRect(Color32[] pixels, int startX, int startY, int width, int height, Color32 color)
    {
        for (int y = startY; y < startY + height; y++)
        {
            if (y < 0 || y >= BadgeTextureSize)
                continue;

            for (int x = startX; x < startX + width; x++)
            {
                if (x < 0 || x >= BadgeTextureSize)
                    continue;

                pixels[y * BadgeTextureSize + x] = color;
            }
        }
    }

    private static string[] GetLetterPattern(string statusLetter)
    {
        return statusLetter switch
        {
            "I" => new[]
            {
                "11111",
                "00100",
                "00100",
                "00100",
                "00100",
                "00100",
                "11111"
            },
            "B" => new[]
            {
                "11110",
                "10001",
                "10001",
                "11110",
                "10001",
                "10001",
                "11110"
            },
            "C" => new[]
            {
                "01111",
                "10000",
                "10000",
                "10000",
                "10000",
                "10000",
                "01111"
            },
            _ => new[]
            {
                "01110",
                "10001",
                "10001",
                "10001",
                "10001",
                "10001",
                "01110"
            }
        };
    }

    private void UpdateStatusBadgeBillboard()
    {
        if (_statusBadgeRenderer == null || !_statusBadgeRenderer.gameObject.activeSelf)
            return;

        Camera activeCamera = GetActiveCamera();
        if (activeCamera == null)
            return;

        Vector3 markerCenter = targetRenderer != null ? targetRenderer.bounds.center : transform.position;
        Vector3 toCamera = activeCamera.transform.position - markerCenter;
        if (toCamera.sqrMagnitude < 0.0001f)
            return;

        Vector3 badgeDirection = toCamera.normalized;
        float markerRadius = GetMarkerRadius();
        float surfaceLift = PositiveOrDefault(statusBadgeSurfaceLift, DefaultStatusBadgeSurfaceLift);
        float verticalOffset = PositiveOrDefault(statusBadgeVerticalOffset, DefaultStatusBadgeVerticalOffset);

        Transform badgeTransform = _statusBadgeRenderer.transform;
        badgeTransform.position = markerCenter
            + badgeDirection * (markerRadius + surfaceLift)
            + activeCamera.transform.up * verticalOffset;
        badgeTransform.rotation = Quaternion.LookRotation(-badgeDirection, activeCamera.transform.up);
        badgeTransform.localScale = GetInverseParentScale() * PositiveOrDefault(statusBadgeSize, DefaultStatusBadgeSize);
    }

    private float GetMarkerRadius()
    {
        if (targetRenderer == null)
            return Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z) * 0.5f;

        Vector3 extents = targetRenderer.bounds.extents;
        return Mathf.Max(extents.x, extents.y, extents.z);
    }

    private Vector3 GetInverseParentScale()
    {
        Vector3 scale = transform.lossyScale;
        return new Vector3(SafeInverse(scale.x), SafeInverse(scale.y), SafeInverse(scale.z));
    }

    private void DestroyLegacyStatusVisuals()
    {
        DestroyChildIfPresent("StatusAccentRing");
        DestroyChildIfPresent("SelectionRing");
        DestroyChildIfPresent("StatusLabel");
        DestroyChildIfPresent("StatusGlyph");
    }

    private void DestroyChildIfPresent(string childName)
    {
        Transform child = transform.Find(childName);
        if (child == null)
            return;

        DestroyChild(child);
    }

    private static void DestroyChild(Transform child)
    {
        if (Application.isPlaying)
            Destroy(child.gameObject);
        else
            DestroyImmediate(child.gameObject);
    }

    private void SetStatusBadgeVisible(bool visible)
    {
        if (_statusBadgeRenderer != null)
            _statusBadgeRenderer.gameObject.SetActive(visible);
    }

    private void DestroyBadgeSprites()
    {
        foreach (Sprite sprite in _badgeSprites.Values)
        {
            if (sprite == null)
                continue;

            Texture2D texture = sprite.texture;

            if (Application.isPlaying)
            {
                Destroy(sprite);
                Destroy(texture);
            }
            else
            {
                DestroyImmediate(sprite);
                DestroyImmediate(texture);
            }
        }

        _badgeSprites.Clear();
    }

    private Color GetSeverityColor(MarkerSeverity severity)
    {
        return severity switch
        {
            MarkerSeverity.Low => VisibleOrDefault(lowSeverityColor, DefaultLowSeverityColor),
            MarkerSeverity.Medium => VisibleOrDefault(mediumSeverityColor, DefaultMediumSeverityColor),
            MarkerSeverity.High => VisibleOrDefault(highSeverityColor, DefaultHighSeverityColor),
            MarkerSeverity.Critical => VisibleOrDefault(criticalSeverityColor, DefaultCriticalSeverityColor),
            _ => DefaultMediumSeverityColor
        };
    }

    private static string GetStatusLetter(MarkerStatus status)
    {
        return status switch
        {
            MarkerStatus.Open => "O",
            MarkerStatus.InProgress => "I",
            MarkerStatus.Blocked => "B",
            MarkerStatus.Closed => "C",
            _ => "O"
        };
    }

    private static Camera GetActiveCamera()
    {
        if (Camera.main != null)
            return Camera.main;

        int cameraCount = Camera.allCamerasCount;
        if (cameraCount == 0)
            return null;

        var cameras = new Camera[cameraCount];
        Camera.GetAllCameras(cameras);
        return cameras[0];
    }

    private static Color VisibleOrDefault(Color color, Color fallback)
    {
        return color.a > 0.001f ? color : fallback;
    }

    private static float PositiveOrDefault(float value, float fallback)
    {
        return value > 0.001f ? value : fallback;
    }

    private static float SafeInverse(float value)
    {
        return Mathf.Abs(value) > 0.0001f ? 1f / value : 1f;
    }
}
