using UnityEngine;

public class HeartPickup : MonoBehaviour
{
    public Transform visual;
    public float radius = 1.4f;

    PlayerHealth player;
    float baseY;
    bool taken;

    void Awake() { if (visual != null) baseY = visual.localPosition.y; }

    void OnEnable()
    {
        taken = false;
        if (visual != null) visual.gameObject.SetActive(true);
    }

    public bool Taken { get { return taken; } }

    void FixedUpdate()
    {
        if (taken) return;
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerHealth>();
            if (player == null) return;
        }
        float r = radius * Mathf.Max(0.001f, transform.lossyScale.x);
        if ((player.transform.position - transform.position).sqrMagnitude < r * r)
        {
            if (player.Heal(1))
            {
                taken = true;
                if (visual != null) visual.gameObject.SetActive(false);
            }
        }
    }

    void Update()
    {
        if (visual == null || taken) return;
        var p = visual.localPosition;
        p.y = baseY + Mathf.Sin(Time.time * 3f) * 0.15f;
        visual.localPosition = p;
        var cam = Camera.main;
        if (cam != null) visual.rotation = Quaternion.LookRotation(visual.position - cam.transform.position);
    }
}
