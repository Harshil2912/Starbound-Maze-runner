using UnityEngine;

public class Projectile : MonoBehaviour
{
    public Vector3 direction = Vector3.forward;
    public float speed = 9f;
    public float life = 5f;
    public float radius = 0.25f;
    public int damage = 1;

    static readonly Collider[] overlaps = new Collider[8];

    void FixedUpdate()
    {
        float s = Mathf.Max(0.001f, transform.lossyScale.x);
        float step = speed * s * Time.fixedDeltaTime;
        float curRadius = radius * s;
        life -= Time.fixedDeltaTime;

        // Already touching the player (e.g. spawned right next to them)?
        int n = Physics.OverlapSphereNonAlloc(transform.position, curRadius, overlaps, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            var h0 = overlaps[i].GetComponentInParent<PlayerHealth>();
            if (h0 != null)
            {
                h0.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }
        }

        RaycastHit hit;
        if (Physics.SphereCast(transform.position, curRadius, direction, out hit, step, ~0, QueryTriggerInteraction.Ignore))
        {
            var h = hit.collider.GetComponentInParent<PlayerHealth>();
            if (h != null) h.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }
        transform.position += direction * step;
        if (life <= 0f) Destroy(gameObject);
    }
}
