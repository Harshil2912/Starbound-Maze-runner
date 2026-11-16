using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    RectTransform rectTransform;
    Rect lastSafeArea = Rect.zero;
    Vector2Int lastScreenSize = Vector2Int.zero;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    void OnEnable()
    {
        ApplySafeArea();
    }

    void Update()
    {
        if (lastSafeArea != Screen.safeArea || lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height)
        {
            ApplySafeArea();
        }
    }

    public void ApplySafeArea()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (Screen.width <= 0 || Screen.height <= 0) return;

        Rect safeArea = Screen.safeArea;
        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);

        // Convert safe area rectangle from screen pixels to normalized anchor coordinates (0..1)
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        // Ensure valid range
        anchorMin.x = Mathf.Clamp01(anchorMin.x);
        anchorMin.y = Mathf.Clamp01(anchorMin.y);
        anchorMax.x = Mathf.Clamp01(anchorMax.x);
        anchorMax.y = Mathf.Clamp01(anchorMax.y);

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}

