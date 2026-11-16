using UnityEngine;
using UnityEngine.UI;

public class CubeColorIndicator : MonoBehaviour
{
    public CubeController target;
    public Image fillCircle;
    public Image outerRing;
    public float smoothSpeed = 10f;
    public bool pulseOnChange = true;

    Color targetColor = Color.cyan;
    Color currentColor = Color.cyan;
    Vector3 baseScale;
    float pulseTimer;

    void Awake()
    {
        baseScale = transform.localScale;
        if (fillCircle == null) fillCircle = GetComponent<Image>();
    }

    void Start()
    {
        if (target == null)
            target = FindAnyObjectByType<CubeController>();

        if (target != null)
        {
            target.OnColorChanged += HandleColorChanged;
            targetColor = target.CurrentColor;
            currentColor = targetColor;
            ApplyColor(currentColor);
        }
    }

    void OnDestroy()
    {
        if (target != null)
        {
            target.OnColorChanged -= HandleColorChanged;
        }
    }

    void HandleColorChanged(Color newColor)
    {
        targetColor = newColor;
        if (pulseOnChange)
        {
            pulseTimer = 0.25f;
        }
    }

    void Update()
    {
        // Smoothly interpolate color
        currentColor = Color.Lerp(currentColor, targetColor, Time.unscaledDeltaTime * smoothSpeed);
        ApplyColor(currentColor);

        // Subtle punch/pulse animation when color changes
        if (pulseTimer > 0f)
        {
            pulseTimer -= Time.unscaledDeltaTime;
            float scaleMultiplier = 1f + Mathf.Sin((0.25f - pulseTimer) / 0.25f * Mathf.PI) * 0.18f;
            transform.localScale = baseScale * scaleMultiplier;
        }
        else
        {
            transform.localScale = baseScale;
        }
    }

    void ApplyColor(Color c)
    {
        if (fillCircle != null)
        {
            fillCircle.color = new Color(c.r, c.g, c.b, 0.95f);
        }
        if (outerRing != null)
        {
            // Outer ring has a brighter/subtler alpha rim
            outerRing.color = new Color(Mathf.Min(c.r + 0.2f, 1f), Mathf.Min(c.g + 0.2f, 1f), Mathf.Min(c.b + 0.2f, 1f), 0.9f);
        }
    }
}

