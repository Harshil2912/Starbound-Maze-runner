using UnityEngine;

public class SpikeTrap : MonoBehaviour
{
    public Transform spikes;
    public Renderer padRenderer;
    public float triggerRadius = 4.5f;
    public float warnTime = 0.6f;
    public float upTime = 1.1f;
    public float cooldown = 1.3f;
    public float hiddenY = -1.0f;
    public float upY = 0f;
    public Vector2 halfSize = new Vector2(1.7f, 1.7f);
    public float hitHeight = 1.4f;
    public int damage = 1;

    enum State { Idle, Warn, Up, Cooldown }
    State state;
    float timer;
    PlayerHealth player;
    MaterialPropertyBlock padMpb;
    float lastIntensity = -1f;
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    public bool IsUp { get { return state == State.Up; } }

    void Awake()
    {
        padMpb = new MaterialPropertyBlock();
    }

    void OnEnable()
    {
        state = State.Idle;
        timer = 0f;
        lastIntensity = -1f;
        SetY(hiddenY);
        SetPad(0.6f);
    }

    void SetY(float y)
    {
        if (spikes == null) return;
        var p = spikes.localPosition; p.y = y; spikes.localPosition = p;
    }

    void SetPad(float intensity)
    {
        if (padRenderer == null) return;
        if (Mathf.Abs(intensity - lastIntensity) < 0.04f) return;
        lastIntensity = intensity;
        if (padMpb == null) padMpb = new MaterialPropertyBlock();
        padRenderer.GetPropertyBlock(padMpb);
        padMpb.SetColor(EmissionColorId, new Color(1f, 0.1f, 0.1f) * intensity);
        padRenderer.SetPropertyBlock(padMpb);
    }

    void FixedUpdate()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerHealth>();
            if (player == null) return;
        }
        Vector3 d = Quaternion.Inverse(transform.rotation) * (player.transform.position - transform.position);
        float s = Mathf.Max(0.001f, transform.lossyScale.x);
        float dt = Time.fixedDeltaTime;
        switch (state)
        {
            case State.Idle:
                if (new Vector2(d.x, d.z).magnitude < triggerRadius * s && Mathf.Abs(d.y) < 2.5f * s)
                {
                    state = State.Warn; timer = warnTime;
                }
                break;
            case State.Warn:
                timer -= dt;
                SetY(hiddenY + 0.3f * Mathf.Abs(Mathf.Sin(timer * 45f)));
                SetPad(2.5f + Mathf.Sin(timer * 40f));
                if (timer <= 0f)
                {
                    state = State.Up;
                    timer = upTime;
                    SetY(upY);
                    SetPad(3f);
                    if (new Vector2(d.x, d.z).magnitude < 18f * s)
                        GameAudioManager.Instance?.PlaySpikeSnap();
                }
                break;
            case State.Up:
                timer -= dt;
                if (Mathf.Abs(d.x) < (halfSize.x + 0.4f) * s && Mathf.Abs(d.z) < (halfSize.y + 0.4f) * s && Mathf.Abs(d.y) < hitHeight * s)
                    player.TakeDamage(damage);
                if (timer <= 0f) { state = State.Cooldown; timer = cooldown; SetY(hiddenY); SetPad(0.6f); }
                break;
            case State.Cooldown:
                timer -= dt;
                if (timer <= 0f) state = State.Idle;
                break;
        }
    }
}
