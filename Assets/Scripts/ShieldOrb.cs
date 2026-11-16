using UnityEngine;

public class ShieldOrb : MonoBehaviour
{
    public Transform visual;
    public float radius = 1.6f;
    public float duration = 8f;

    PlayerHealth player;
    float baseY;
    bool taken;

    public bool Taken { get { return taken; } }

    void Awake() { if (visual != null) baseY = visual.localPosition.y; }

    void OnEnable()
    {
        taken = false;
        if (visual != null) visual.gameObject.SetActive(true);
    }

    void FixedUpdate()
    {
        if (taken) return;
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerHealth>();
            if (player == null) return;
        }
        Vector3 d = player.transform.position - transform.position;
        float s = Mathf.Max(0.001f, transform.lossyScale.x);
        if (new Vector2(d.x, d.z).magnitude < radius * s && Mathf.Abs(d.y) < 3f * s)
        {
            taken = true;
            if (visual != null) visual.gameObject.SetActive(false);
            player.GrantShield(duration);
        }
    }

    void Update()
    {
        if (visual == null || taken) return;
        visual.Rotate(0f, 80f * Time.deltaTime, 0f, Space.Self);
        var p = visual.localPosition;
        p.y = baseY + Mathf.Sin(Time.time * 2f) * 0.18f;
        visual.localPosition = p;
    }
}
