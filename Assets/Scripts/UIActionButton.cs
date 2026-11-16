using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public enum ActionType { Stop, ToggleMode }
    public CubeController target;
    public ActionType action;
    public Text label;

    Graphic graphic;
    Color restingColor;
    bool hasRestingColor;

    public static readonly Color DefaultStopRed = new Color(0.86f, 0.18f, 0.24f, 1.00f);
    public static readonly Color GreyishLight = new Color(0.82f, 0.86f, 0.92f, 1.00f);

    void Awake()
    {
        graphic = GetComponent<Graphic>();
        InitRestingColor();
    }

    void InitRestingColor()
    {
        if (hasRestingColor) return;
        if (action == ActionType.Stop)
        {
            restingColor = DefaultStopRed;
            hasRestingColor = true;
            if (graphic != null) graphic.color = restingColor;
        }
        else if (graphic != null)
        {
            restingColor = graphic.color;
            hasRestingColor = true;
        }
    }

    void Start()
    {
        InitRestingColor();
        if (graphic != null) graphic.color = restingColor;
        Refresh();
    }

    void OnEnable()
    {
        CubeController.OnStickyModeChanged += HandleStickyModeChanged;
        InitRestingColor();
        if (graphic != null) graphic.color = restingColor;
        Refresh();
    }

    void OnDisable()
    {
        CubeController.OnStickyModeChanged -= HandleStickyModeChanged;
        if (graphic != null && hasRestingColor) graphic.color = restingColor;
    }

    void HandleStickyModeChanged(bool sticky)
    {
        Refresh();
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (ControlsLayoutManager.IsEditingLayout) return;
        if (graphic != null) graphic.color = GreyishLight;

        if (action == ActionType.Stop)
        {
            if (target == null) return;
            target.StopMovement();
            GameAudioManager.Instance?.PlayStopBrake();
            HapticsManager.TriggerClick();
        }
        else
        {
            bool nextSticky = !CubeController.PreferredStickyMode;
            CubeController.PreferredStickyMode = nextSticky;
            HapticsManager.SuppressFor(0.25f);
            GameAudioManager.Instance?.StopAllSfx();
            GameAudioManager.Instance?.PlayUIClick();
            Refresh();
        }
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (graphic != null && hasRestingColor)
        {
            graphic.color = restingColor;
        }
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (graphic != null && hasRestingColor)
        {
            graphic.color = restingColor;
        }
    }

    public void Refresh()
    {
        if (action == ActionType.ToggleMode && label != null)
        {
            bool sticky = (target != null && target.gameObject.activeInHierarchy)
                ? target.stickyMode
                : CubeController.PreferredStickyMode;
            label.text = sticky ? "MODE: TAP TO MOVE" : "MODE: HOLD TO MOVE";
        }
    }
}
