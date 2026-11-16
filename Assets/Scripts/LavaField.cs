using UnityEngine;

public class LavaField : MonoBehaviour
{
    public int damage = 1;
    public float knockSpeed = 10f;
    public float knockTime = 0.25f;

    CubeController player;
    PlayerHealth health;
    static readonly Collider[] buf = new Collider[16];

    void FixedUpdate()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<CubeController>();
            if (player == null) return;
            health = player.GetComponent<PlayerHealth>();
        }
        if (health == null) return;
        Vector3 p = player.transform.position;
        float s = Mathf.Max(0.001f, player.transform.lossyScale.x);
        int n = Physics.OverlapBoxNonAlloc(p, new Vector3(0.62f * s, 0.6f * s, 0.62f * s), buf, player.transform.rotation, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            if (buf[i].GetComponent<LavaWall>() == null) continue;
            if (health.TakeDamage(damage, customSfx: true))
            {
                GameAudioManager.Instance?.PlayDamageLava();
                Vector3 closest = buf[i].ClosestPoint(p);
                Vector3 dir = p - closest;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.00001f) dir = p - buf[i].bounds.center;
                player.Knockback(dir, knockSpeed, knockTime);
            }
            break;
        }
    }
}
