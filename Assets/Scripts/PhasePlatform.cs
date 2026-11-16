using UnityEngine;

public class PhasePlatform : MonoBehaviour
{
    public float solidTime = 2.4f;
    public float flickerTime = 0.6f;
    public float hiddenTime = 1.6f;
    public float phase = 0f;
    public Renderer padRenderer;

    Collider col;
    MaterialPropertyBlock mpb;
    float timer;
    Color baseColor = Color.white;
    int lastVisualState = -1;
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        col = GetComponent<Collider>();
        if (padRenderer == null) padRenderer = GetComponentInChildren<Renderer>();
        mpb = new MaterialPropertyBlock();
        if (padRenderer != null && padRenderer.sharedMaterial != null)
        {
            var sm = padRenderer.sharedMaterial;
            if (sm.HasProperty(BaseColorId)) baseColor = sm.GetColor(BaseColorId);
            else if (sm.HasProperty(ColorId)) baseColor = sm.GetColor(ColorId);
            else baseColor = sm.color;
        }
    }

    void OnEnable()
    {
        timer = phase;
        lastVisualState = -1;
        UpdateState();
    }

    void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;
        UpdateState();
    }

    void ApplyPadColor(Color colTint, Color emitCol)
    {
        if (padRenderer == null) return;
        if (mpb == null) mpb = new MaterialPropertyBlock();
        padRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(ColorId, colTint);
        mpb.SetColor(BaseColorId, colTint);
        mpb.SetColor(EmissionColorId, emitCol);
        padRenderer.SetPropertyBlock(mpb);
    }

    void UpdateState()
    {
        float total = solidTime + flickerTime + hiddenTime;
        if (total <= 0.01f) total = 1f;
        float mod = timer % total;

        int stateKey;
        if (mod < solidTime)
        {
            stateKey = 0; // Solid
            if (stateKey != lastVisualState)
            {
                lastVisualState = stateKey;
                if (col != null) col.enabled = true;
                if (padRenderer != null) padRenderer.enabled = true;
                var c = baseColor; c.a = 1.0f;
                ApplyPadColor(c, new Color(0.6f, 0.3f, 1f) * 2.0f);
            }
        }
        else if (mod < solidTime + flickerTime)
        {
            bool on = (Mathf.Sin(timer * 35f) > 0f);
            stateKey = on ? 1 : 2; // Flickering warning
            if (stateKey != lastVisualState)
            {
                lastVisualState = stateKey;
                if (col != null) col.enabled = true;
                if (padRenderer != null) padRenderer.enabled = on;
                if (on)
                {
                    var c = baseColor; c.a = 0.85f;
                    ApplyPadColor(c, new Color(0.9f, 0.4f, 1f) * 2.5f);
                }
            }
        }
        else
        {
            stateKey = 3; // Hidden / Vanished
            if (stateKey != lastVisualState)
            {
                lastVisualState = stateKey;
                if (col != null) col.enabled = false;
                if (padRenderer != null) padRenderer.enabled = true;
                var c = baseColor; c.a = 0.15f;
                ApplyPadColor(c, Color.black);
            }
        }
    }
}
