using UnityEngine;

public class Teleporter : MonoBehaviour
{
    public Teleporter other;
    public Transform ring;
    public float radius = 1.5f;
    public float chargeTime = 0.3f;

    float cooldown;
    float charge;
    CubeController player;

    public void SetCooldown(float t) { cooldown = t; }

    void OnEnable() { cooldown = 0.5f; charge = 0f; }

    void FixedUpdate()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<CubeController>();
            if (player == null) return;
        }
        if (cooldown > 0f) { cooldown -= Time.fixedDeltaTime; return; }
        Vector3 d = player.transform.position - transform.position;
        float s = Mathf.Max(0.001f, transform.lossyScale.x);
        if (new Vector2(d.x, d.z).magnitude < radius * s && Mathf.Abs(d.y) < 2f * s)
        {
            charge += Time.fixedDeltaTime;
            if (charge >= chargeTime && other != null) Go();
        }
        else charge = 0f;
    }

    void Go()
    {
        charge = 0f;
        cooldown = 1.5f;
        other.SetCooldown(1.5f);
        Color c = GetPortalColor();
        player.Burst(c, 24);
        player.TeleportTo(other.transform.TransformPoint(new Vector3(0f, 0.5f, 0f)));
        player.Burst(c, 24);
        GameAudioManager.Instance?.PlayTeleporter();
        HapticsManager.TriggerClick();
        var cam = Camera.main;
        if (cam != null)
        {
            var rig = cam.GetComponent<ThirdPersonCamera>();
            if (rig != null) rig.Snap();
        }
    }

    Color GetPortalColor()
    {
        if (gameObject.name.Contains("TeleB")) return new Color(0.20f, 1.0f, 0.50f);
        if (ring != null)
        {
            var r = ring.GetComponent<Renderer>();
            if (r != null && r.sharedMaterial != null)
            {
                if (r.sharedMaterial.name.Contains("TeleB")) return new Color(0.20f, 1.0f, 0.50f);
                if (r.sharedMaterial.HasProperty("_BaseColor")) return r.sharedMaterial.GetColor("_BaseColor");
            }
        }
        return new Color(0.75f, 0.35f, 1f);
    }

    void Update()
    {
        if (ring != null) ring.Rotate(0f, 90f * Time.deltaTime, 0f, Space.Self);
    }
}
