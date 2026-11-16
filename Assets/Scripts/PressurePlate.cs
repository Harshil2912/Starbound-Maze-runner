using System.Collections.Generic;
using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    public float requiredMass = 0.9f;
    public Vector3 zoneHalfExtents = new Vector3(1.3f, 0.7f, 1.3f);
    public float holdTime = 0f;
    public Activatable[] targets;
    public Transform top;
    public Renderer topRenderer;
    public Color offColor = new Color(0.9f, 0.7f, 0.1f);
    public Color onColor = new Color(0.2f, 1f, 0.4f);
    public float pressDepth = 0.05f;

    static readonly Collider[] buf = new Collider[16];
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    readonly HashSet<Rigidbody> seen = new HashSet<Rigidbody>();
    MaterialPropertyBlock mpb;
    bool active;
    float hold;
    float topBaseY;

    public bool IsActive { get { return active; } }

    void Awake()
    {
        if (top != null) topBaseY = top.localPosition.y;
        mpb = new MaterialPropertyBlock();
    }

    void OnEnable()
    {
        hold = 0f;
        Apply(false);
    }

    void Apply(bool on)
    {
        active = on;
        if (targets != null)
            foreach (var t in targets) if (t != null) t.Activate(on);
        if (top != null)
        {
            var p = top.localPosition; p.y = topBaseY - (on ? pressDepth : 0f); top.localPosition = p;
        }
        if (topRenderer != null)
        {
            if (mpb == null) mpb = new MaterialPropertyBlock();
            Color c = on ? onColor : offColor;
            topRenderer.GetPropertyBlock(mpb);
            mpb.SetColor(ColorId, c);
            mpb.SetColor(BaseColorId, c);
            mpb.SetColor(EmissionColorId, c * 1.6f);
            topRenderer.SetPropertyBlock(mpb);
        }
    }

    void FixedUpdate()
    {
        float s = Mathf.Max(0.001f, transform.lossyScale.x);
        int n = Physics.OverlapBoxNonAlloc(transform.position + transform.up * (0.6f * s), zoneHalfExtents * s, buf, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
        seen.Clear();
        float mass = 0f;
        for (int i = 0; i < n; i++)
        {
            var rb = buf[i].attachedRigidbody;
            if (rb != null && seen.Add(rb)) mass += rb.mass;
        }
        bool pressed = mass >= requiredMass;
        if (pressed)
        {
            hold = holdTime;
            if (!active)
            {
                Apply(true);
                GameAudioManager.Instance?.PlaySwitchPlate();
                HapticsManager.TriggerClick();
            }
        }
        else if (active)
        {
            hold -= Time.fixedDeltaTime;
            if (hold <= 0f) Apply(false);
        }
    }
}
