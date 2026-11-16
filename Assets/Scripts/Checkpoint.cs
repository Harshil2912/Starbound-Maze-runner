using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public Renderer flag;
    public Color offColor = new Color(0.8f, 0.8f, 0.85f);
    public Color onColor = new Color(0.2f, 1f, 0.45f);
    public float radius = 2.2f;

    bool reached;
    CubeController player;
    MaterialPropertyBlock mpb;
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    public bool Reached { get { return reached; } }

    void OnEnable()
    {
        reached = false;
        Paint(offColor, 0.2f);
    }

    void Paint(Color c, float glow)
    {
        if (flag == null) return;
        if (mpb == null) mpb = new MaterialPropertyBlock();
        flag.GetPropertyBlock(mpb);
        mpb.SetColor(ColorId, c);
        mpb.SetColor(BaseColorId, c);
        mpb.SetColor(EmissionColorId, c * glow);
        flag.SetPropertyBlock(mpb);
    }

    void FixedUpdate()
    {
        if (reached) return;
        if (player == null)
        {
            player = FindFirstObjectByType<CubeController>();
            if (player == null) return;
        }
        Vector3 d = player.transform.position - transform.position;
        d.y = 0f;
        float effectiveRadius = radius * Mathf.Max(0.001f, transform.lossyScale.x);
        if (d.magnitude < effectiveRadius)
        {
            reached = true;
            player.SetRespawn(transform.TransformPoint(new Vector3(0f, 0.5f, 0f)));
            Paint(onColor, 2f);
            player.Burst(onColor, 20);
            GameAudioManager.Instance?.PlayCheckpoint();
            HapticsManager.Pickup();
        }
    }
}
