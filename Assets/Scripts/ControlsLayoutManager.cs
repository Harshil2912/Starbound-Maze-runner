using UnityEngine;
using UnityEngine.UI;

public class ControlsLayoutManager : MonoBehaviour
{
    public enum ControlId { DPad, Jump }

    public static bool IsEditingLayout { get; private set; }

    [Header("Safe Area & Targets")]
    public RectTransform safeAreaRect;
    public RectTransform dpadCluster;
    public RectTransform jumpButton;

    [Header("Highlight Visuals (shown while editing)")]
    public GameObject dpadEditFrame;
    public GameObject jumpEditFrame;

    [Header("Bounded Layout Limits")]
    public float minScale = 0.75f;
    public float maxScale = 1.35f;
    public float edgePadding = 18f;
    public float topHudReserved = 185f;
    public float minGapBetweenControls = 20f;

    [Header("Defaults (normalized inside valid safe bounds)")]
    public Vector2 defaultDpadNorm = new Vector2(0.02f, 0.04f);
    public float defaultDpadScale = 1.0f;
    public Vector2 defaultJumpNorm = new Vector2(0.98f, 0.04f);
    public float defaultJumpScale = 1.0f;

    const string PrefDpadX = "MR_Ctrl_Dpad_X";
    const string PrefDpadY = "MR_Ctrl_Dpad_Y";
    const string PrefDpadScale = "MR_Ctrl_Dpad_S";
    const string PrefJumpX = "MR_Ctrl_Jump_X";
    const string PrefJumpY = "MR_Ctrl_Jump_Y";
    const string PrefJumpScale = "MR_Ctrl_Jump_S";

    Vector2 dpadNorm;
    float dpadScale = 1f;
    Vector2 jumpNorm;
    float jumpScale = 1f;
    ControlId selected = ControlId.DPad;
    Vector2 lastSafeSize = Vector2.zero;

    public ControlId SelectedControl { get { return selected; } }
    public event System.Action OnLayoutStateChanged;

    void Awake()
    {
        LoadLayout();
    }

    void Start()
    {
        ApplyLayout();
        UpdateHighlights();
    }

    void OnDisable()
    {
        IsEditingLayout = false;
    }

    void Update()
    {
        if (safeAreaRect != null)
        {
            Vector2 sz = safeAreaRect.rect.size;
            if (sz.x > 10f && sz.y > 10f && Vector2.SqrMagnitude(sz - lastSafeSize) > 1f)
            {
                lastSafeSize = sz;
                ApplyLayout();
            }
        }
    }

    public void SetEditingMode(bool editing)
    {
        IsEditingLayout = editing;
        UpdateHighlights();
        if (!editing) SaveLayout();
        OnLayoutStateChanged?.Invoke();
    }

    public void SelectControl(ControlId id)
    {
        selected = id;
        UpdateHighlights();
        OnLayoutStateChanged?.Invoke();
    }

    public float GetSelectedScale()
    {
        return (selected == ControlId.DPad) ? dpadScale : jumpScale;
    }

    public float GetNormalizedScaleSliderValue()
    {
        return Mathf.InverseLerp(minScale, maxScale, GetSelectedScale());
    }

    public void SetSelectedScaleFromSlider(float t)
    {
        SetControlScale(selected, Mathf.Lerp(minScale, maxScale, Mathf.Clamp01(t)));
    }

    public void SetControlScale(ControlId id, float newScale)
    {
        float clamped = Mathf.Clamp(newScale, minScale, maxScale);
        if (id == ControlId.DPad) dpadScale = clamped;
        else jumpScale = clamped;

        ApplyLayout();
        SeparateControls(id);
        SaveLayout();
        OnLayoutStateChanged?.Invoke();
    }

    public void MoveControlByScreenDelta(ControlId id, Vector2 screenDelta, Camera uiCamera)
    {
        if (safeAreaRect == null) return;
        RectTransform target = (id == ControlId.DPad) ? dpadCluster : jumpButton;
        if (target == null) return;

        // Convert screen-pixel delta into safeAreaRect local units via canvas scale factor
        Canvas rootCanvas = safeAreaRect.GetComponentInParent<Canvas>();
        float scaleFactor = (rootCanvas != null && rootCanvas.scaleFactor > 0.001f) ? rootCanvas.scaleFactor : 1f;
        Vector2 localDelta = screenDelta / scaleFactor;

        Vector2 currentPos = target.anchoredPosition;
        Vector2 desiredPos = currentPos + localDelta;
        desiredPos = ClampPositionInSafeArea(target, desiredPos);
        target.anchoredPosition = desiredPos;

        SeparateControls(id);
        StoreNormalizedFromAnchored(id);
        UpdateHighlights();
    }

    public void ResetToDefaults()
    {
        dpadNorm = defaultDpadNorm;
        dpadScale = defaultDpadScale;
        jumpNorm = defaultJumpNorm;
        jumpScale = defaultJumpScale;
        ApplyLayout();
        SeparateControls(ControlId.DPad);
        SaveLayout();
        OnLayoutStateChanged?.Invoke();
    }

    public void LoadLayout()
    {
        dpadNorm.x = PlayerPrefs.GetFloat(PrefDpadX, defaultDpadNorm.x);
        dpadNorm.y = PlayerPrefs.GetFloat(PrefDpadY, defaultDpadNorm.y);
        dpadScale = Mathf.Clamp(PlayerPrefs.GetFloat(PrefDpadScale, defaultDpadScale), minScale, maxScale);

        jumpNorm.x = PlayerPrefs.GetFloat(PrefJumpX, defaultJumpNorm.x);
        jumpNorm.y = PlayerPrefs.GetFloat(PrefJumpY, defaultJumpNorm.y);
        jumpScale = Mathf.Clamp(PlayerPrefs.GetFloat(PrefJumpScale, defaultJumpScale), minScale, maxScale);
    }

    public void SaveLayout()
    {
        PlayerPrefs.SetFloat(PrefDpadX, dpadNorm.x);
        PlayerPrefs.SetFloat(PrefDpadY, dpadNorm.y);
        PlayerPrefs.SetFloat(PrefDpadScale, dpadScale);

        PlayerPrefs.SetFloat(PrefJumpX, jumpNorm.x);
        PlayerPrefs.SetFloat(PrefJumpY, jumpNorm.y);
        PlayerPrefs.SetFloat(PrefJumpScale, jumpScale);
        PlayerPrefs.Save();
    }

    public void ApplyLayout()
    {
        if (safeAreaRect == null) return;

        if (dpadCluster != null)
        {
            dpadCluster.anchorMin = new Vector2(0.5f, 0.5f);
            dpadCluster.anchorMax = new Vector2(0.5f, 0.5f);
            dpadCluster.pivot = new Vector2(0.5f, 0.5f);
            dpadCluster.localScale = Vector3.one * dpadScale;
            GetValidCenterBounds(dpadCluster, out float minX, out float maxX, out float minY, out float maxY);
            dpadCluster.anchoredPosition = new Vector2(
                Mathf.Lerp(minX, maxX, Mathf.Clamp01(dpadNorm.x)),
                Mathf.Lerp(minY, maxY, Mathf.Clamp01(dpadNorm.y))
            );
        }

        if (jumpButton != null)
        {
            jumpButton.anchorMin = new Vector2(0.5f, 0.5f);
            jumpButton.anchorMax = new Vector2(0.5f, 0.5f);
            jumpButton.pivot = new Vector2(0.5f, 0.5f);
            jumpButton.localScale = Vector3.one * jumpScale;
            GetValidCenterBounds(jumpButton, out float minX, out float maxX, out float minY, out float maxY);
            jumpButton.anchoredPosition = new Vector2(
                Mathf.Lerp(minX, maxX, Mathf.Clamp01(jumpNorm.x)),
                Mathf.Lerp(minY, maxY, Mathf.Clamp01(jumpNorm.y))
            );
        }
    }

    void GetValidCenterBounds(RectTransform rt, out float minX, out float maxX, out float minY, out float maxY)
    {
        Rect r = safeAreaRect.rect;
        if (r.width < 100f || r.height < 100f)
        {
            r = new Rect(-960f, -540f, 1920f, 1080f);
        }

        float s = rt.localScale.x;
        float halfW = (rt.rect.width * 0.5f) * s;
        float halfH = (rt.rect.height * 0.5f) * s;

        minX = r.xMin + edgePadding + halfW;
        maxX = r.xMax - edgePadding - halfW;
        if (minX > maxX) minX = maxX = r.center.x;

        minY = r.yMin + edgePadding + halfH;
        maxY = r.yMax - topHudReserved - halfH;
        if (minY > maxY) minY = maxY = r.yMin + (r.height - topHudReserved) * 0.5f;
    }

    Vector2 ClampPositionInSafeArea(RectTransform rt, Vector2 pos)
    {
        GetValidCenterBounds(rt, out float minX, out float maxX, out float minY, out float maxY);
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        return pos;
    }

    void SeparateControls(ControlId moving)
    {
        if (dpadCluster == null || jumpButton == null) return;

        RectTransform mover = (moving == ControlId.DPad) ? dpadCluster : jumpButton;
        RectTransform other = (moving == ControlId.DPad) ? jumpButton : dpadCluster;

        Vector2 mPos = mover.anchoredPosition;
        Vector2 oPos = other.anchoredPosition;

        float needX = (mover.rect.width * 0.5f * mover.localScale.x) +
                      (other.rect.width * 0.5f * other.localScale.x) + minGapBetweenControls;
        float needY = (mover.rect.height * 0.5f * mover.localScale.y) +
                      (other.rect.height * 0.5f * other.localScale.y) + minGapBetweenControls;

        float dx = mPos.x - oPos.x;
        float dy = mPos.y - oPos.y;
        float penX = needX - Mathf.Abs(dx);
        float penY = needY - Mathf.Abs(dy);

        if (penX > 0f && penY > 0f)
        {
            if (penX <= penY)
            {
                float dir = (dx >= 0f) ? 1f : -1f;
                Vector2 pushed = ClampPositionInSafeArea(mover, new Vector2(oPos.x + dir * needX, mPos.y));
                if (Mathf.Abs(pushed.x - oPos.x) < needX - 2f)
                {
                    // Hit horizontal wall; resolve vertically instead
                    float dirY = (dy >= 0f) ? 1f : -1f;
                    pushed = ClampPositionInSafeArea(mover, new Vector2(mPos.x, oPos.y + dirY * needY));
                }
                mover.anchoredPosition = pushed;
            }
            else
            {
                float dirY = (dy >= 0f) ? 1f : -1f;
                Vector2 pushed = ClampPositionInSafeArea(mover, new Vector2(mPos.x, oPos.y + dirY * needY));
                if (Mathf.Abs(pushed.y - oPos.y) < needY - 2f)
                {
                    float dirX = (dx >= 0f) ? 1f : -1f;
                    pushed = ClampPositionInSafeArea(mover, new Vector2(oPos.x + dirX * needX, mPos.y));
                }
                mover.anchoredPosition = pushed;
            }
        }

        StoreNormalizedFromAnchored(ControlId.DPad);
        StoreNormalizedFromAnchored(ControlId.Jump);
    }

    void StoreNormalizedFromAnchored(ControlId id)
    {
        RectTransform rt = (id == ControlId.DPad) ? dpadCluster : jumpButton;
        if (rt == null) return;

        GetValidCenterBounds(rt, out float minX, out float maxX, out float minY, out float maxY);
        float nx = (maxX > minX + 0.01f) ? Mathf.InverseLerp(minX, maxX, rt.anchoredPosition.x) : 0.5f;
        float ny = (maxY > minY + 0.01f) ? Mathf.InverseLerp(minY, maxY, rt.anchoredPosition.y) : 0.5f;

        if (id == ControlId.DPad) dpadNorm = new Vector2(nx, ny);
        else jumpNorm = new Vector2(nx, ny);
    }

    void UpdateHighlights()
    {
        if (dpadEditFrame != null)
        {
            dpadEditFrame.SetActive(IsEditingLayout);
            var img = dpadEditFrame.GetComponent<Image>();
            if (img != null)
                img.color = (selected == ControlId.DPad) ? new Color(0.15f, 0.9f, 1f, 0.9f) : new Color(1f, 1f, 1f, 0.35f);
        }
        if (jumpEditFrame != null)
        {
            jumpEditFrame.SetActive(IsEditingLayout);
            var img = jumpEditFrame.GetComponent<Image>();
            if (img != null)
                img.color = (selected == ControlId.Jump) ? new Color(0.15f, 0.9f, 1f, 0.9f) : new Color(1f, 1f, 1f, 0.35f);
        }
    }
}

