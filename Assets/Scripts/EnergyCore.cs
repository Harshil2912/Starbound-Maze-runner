using UnityEngine;

public class EnergyCore : MonoBehaviour
{
    public Transform visual;
    public CoreGate gate;
    public float radius = 1.6f;

    CubeController player;
    float baseY;
    bool taken;

    public bool Taken { get { return taken; } }

    void Awake() { if (visual != null) baseY = visual.localPosition.y; }

    public void ResetCore()
    {
        taken = false;
        if (visual != null) visual.gameObject.SetActive(true);
    }

    void OnEnable()
    {
        if (visual != null) visual.gameObject.SetActive(!taken);
    }

    void FixedUpdate()
    {
        if (taken) return;
        if (player == null)
        {
            player = FindFirstObjectByType<CubeController>();
            if (player == null) return;
        }
        Vector3 d = player.transform.position - transform.position;
        float s = Mathf.Max(0.001f, transform.lossyScale.x);
        if (new Vector2(d.x, d.z).magnitude < radius * s && Mathf.Abs(d.y) < 3f * s)
        {
            taken = true;
            if (visual != null) visual.gameObject.SetActive(false);
            player.Burst(new Color(0.3f, 1f, 0.9f), 28);
            GameAudioManager.Instance?.PlayCorePickup();
            HapticsManager.Pickup();
            if (gate != null) gate.Collect();
        }
    }

    void Update()
    {
        if (visual == null || taken) return;
        visual.Rotate(0f, 110f * Time.deltaTime, 0f, Space.Self);
        var p = visual.localPosition;
        p.y = baseY + Mathf.Sin(Time.time * 2.5f) * 0.2f;
        visual.localPosition = p;
    }
}
