using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

public class UIHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, ICancelHandler
{
    public CubeController target;
    public Vector2 direction;
    public bool isJump;

    [SerializeField] Color customRestingColor = Color.clear;
    Color restingColor;
    bool hasRestingColor;

    Graphic graphic;
    Selectable selectable;
    bool held;
    int activePointerId = int.MinValue;
    bool trackingTouch;
    int activeTouchId = -1;
    bool trackingMouse;

    static readonly Color DefaultRestingColor = new Color(0.18f, 0.26f, 0.40f, 0.90f);
    public static readonly Color GreyishLightColor = new Color(0.82f, 0.86f, 0.92f, 1.00f);
    static readonly Color LatchedColor = new Color(0.10f, 0.88f, 0.95f, 1.00f);

    void Awake()
    {
        graphic = GetComponent<Graphic>();
        selectable = GetComponent<Selectable>();
        if (selectable != null)
        {
            selectable.transition = Selectable.Transition.None;
            var nav = selectable.navigation;
            nav.mode = Navigation.Mode.None;
            selectable.navigation = nav;
        }

        if (customRestingColor.a > 0.01f)
        {
            restingColor = customRestingColor;
            hasRestingColor = true;
        }
        else if (graphic != null && graphic.color.a > 0.01f)
        {
            restingColor = graphic.color;
            hasRestingColor = true;
        }
        else
        {
            restingColor = DefaultRestingColor;
            hasRestingColor = true;
        }
    }

    void Start()
    {
        UpdateVisual(false, false);
    }

    void OnEnable()
    {
        UpdateVisual(false, false);
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (ControlsLayoutManager.IsEditingLayout || Time.timeScale == 0f) return;
        if (target == null) return;

        if (isJump)
        {
            held = true;
            RecordPointer(e);
            UpdateVisual(true, false);
            target.RequestJump();
            return;
        }

        if (target.stickyMode)
        {
            target.ToggleSticky(direction);
            HapticsManager.LightTap();
            RefreshStickyVisual();
            return;
        }

        RecordPointer(e);
        if (!held)
        {
            held = true;
            target.SetButtonHeld(this, true);
            HapticsManager.LightTap();
        }
        UpdateVisual(true, false);
    }

    void RecordPointer(PointerEventData e)
    {
        activePointerId = e != null ? e.pointerId : int.MinValue;
        trackingTouch = false;
        trackingMouse = false;
        activeTouchId = -1;

#if ENABLE_INPUT_SYSTEM
        var ext = e as ExtendedPointerEventData;
        if (ext != null)
        {
            if (ext.pointerType == UIPointerType.Touch)
            {
                trackingTouch = true;
                activeTouchId = ext.touchId;
            }
            else if (ext.pointerType == UIPointerType.MouseOrPen)
            {
                trackingMouse = true;
            }
        }
#endif
    }

    bool IsTrackedPointerStillPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (trackingTouch)
        {
            var ts = Touchscreen.current;
            if (ts == null) return false;
            var touches = ts.touches;
            for (int i = 0; i < touches.Count; i++)
            {
                var t = touches[i];
                if (t.touchId.ReadValue() == activeTouchId)
                    return t.isInProgress;
            }
            return false;
        }
        if (trackingMouse)
        {
            var m = Mouse.current;
            return m != null && m.leftButton.isPressed;
        }
#endif
        return true;
    }

    void Update()
    {
        if (held)
        {
            if (ControlsLayoutManager.IsEditingLayout || Time.timeScale == 0f || !IsTrackedPointerStillPressed())
            {
                Release();
                return;
            }
        }
        else if (!isJump && target != null && target.stickyMode)
        {
            RefreshStickyVisual();
        }
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (e == null || activePointerId == int.MinValue || e.pointerId == activePointerId)
        {
            Release();
        }
    }

    public void OnCancel(BaseEventData e)
    {
        // Ignore spurious UGUI selection/navigation cancels while the player's finger is still held on this button
        if (ControlsLayoutManager.IsEditingLayout || Time.timeScale == 0f || !IsTrackedPointerStillPressed())
        {
            Release();
        }
    }

    void OnDisable() { Release(); }
    void OnApplicationFocus(bool hasFocus) { if (!hasFocus) Release(); }

    public void ClearHeldState()
    {
        held = false;
        activePointerId = int.MinValue;
        trackingTouch = false;
        trackingMouse = false;
        activeTouchId = -1;
        UpdateVisual(false, false);
    }

    void RefreshStickyVisual()
    {
        if (isJump || target == null || !target.stickyMode) return;
        Vector2 cur = target.ActiveMoveInput;
        bool active = (direction.x != 0f && Mathf.Sign(cur.x) == Mathf.Sign(direction.x) && Mathf.Abs(cur.x) > 0.2f)
                   || (direction.y != 0f && Mathf.Sign(cur.y) == Mathf.Sign(direction.y) && Mathf.Abs(cur.y) > 0.2f);
        UpdateVisual(false, active);
    }

    public void SetRestingColor(Color col)
    {
        restingColor = col;
        hasRestingColor = true;
        UpdateVisual(held, false);
    }

    void UpdateVisual(bool isPressed, bool isLatched)
    {
        if (graphic == null) graphic = GetComponent<Graphic>();
        if (graphic == null) return;
        if (!hasRestingColor)
        {
            restingColor = customRestingColor.a > 0.01f ? customRestingColor : graphic.color;
            hasRestingColor = true;
        }
        graphic.color = isPressed ? GreyishLightColor : (isLatched ? LatchedColor : restingColor);
    }

    void Release()
    {
        if (held)
        {
            held = false;
            activePointerId = int.MinValue;
            trackingTouch = false;
            trackingMouse = false;
            activeTouchId = -1;
            if (!isJump && target != null) target.SetButtonHeld(this, false);
        }
        UpdateVisual(false, false);
    }
}
