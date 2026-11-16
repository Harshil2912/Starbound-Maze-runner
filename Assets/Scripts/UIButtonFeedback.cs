using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Uniform button interaction feedback across the entire game:
/// Every button displays its authentic original color at rest.
/// When tapped / pressed, it flashes a crisp illuminated greyish light.
/// When released or pointer exits, it reverts back to its original color.
/// </summary>
[DisallowMultipleComponent]
public class UIButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public Color customRestingColor = Color.clear;
    
    Graphic graphic;
    Color originalColor;
    bool isPointerDown;

    // Crisp illuminated silvery-grey light
    public static readonly Color GreyishLight = new Color(0.82f, 0.86f, 0.92f, 1.00f);

    void Awake()
    {
        CacheComponents();
    }

    void CacheComponents()
    {
        if (graphic == null)
            graphic = GetComponent<Graphic>();

        if (customRestingColor.a > 0.01f)
        {
            originalColor = customRestingColor;
        }
        else if (graphic != null)
        {
            originalColor = graphic.color;
        }

        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.transition = Selectable.Transition.None;
        }
    }

    void Start()
    {
        RevertToOriginal();
    }

    void OnEnable()
    {
        RevertToOriginal();
    }

    void OnDisable()
    {
        isPointerDown = false;
        RevertToOriginal();
    }

    public void SetOriginalColor(Color c)
    {
        originalColor = c;
        var g = EnsureGraphic();
        if (!isPointerDown && g != null)
        {
            g.color = c;
        }
    }

    public Color GetOriginalColor()
    {
        return originalColor;
    }

    Graphic EnsureGraphic()
    {
        if (graphic == null)
            graphic = GetComponent<Graphic>();
        return graphic;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPointerDown = true;
        var g = EnsureGraphic();
        if (g != null)
        {
            g.color = GreyishLight;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPointerDown = false;
        RevertToOriginal();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isPointerDown)
        {
            isPointerDown = false;
            RevertToOriginal();
        }
    }

    void RevertToOriginal()
    {
        var g = EnsureGraphic();
        if (g != null && originalColor.a > 0.001f)
        {
            g.color = originalColor;
        }
    }
}
