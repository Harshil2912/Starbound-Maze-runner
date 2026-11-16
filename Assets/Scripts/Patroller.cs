using UnityEngine;

public class Patroller : MonoBehaviour
{
    public Vector3 pointA;
    public Vector3 pointB;
    public float speed = 3.5f;
    public Transform spinner;
    public int damage = 1;
    public Vector3 hitHalfSize = new Vector3(1.1f, 1.1f, 1.1f);

    bool toB;
    PlayerHealth player;

    void OnEnable()
    {
        transform.localPosition = pointA;
        toB = true;
    }

    void FixedUpdate()
    {
        Vector3 target = toB ? pointB : pointA;
        transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, speed * Time.fixedDeltaTime);
        if ((transform.localPosition - target).sqrMagnitude < 0.0004f) toB = !toB;

        if (player == null)
        {
            player = FindFirstObjectByType<PlayerHealth>();
            if (player == null) return;
        }
        float s = transform.parent != null ? Mathf.Max(0.001f, transform.parent.lossyScale.x) : Mathf.Max(0.001f, transform.lossyScale.x);
        Vector3 d = Quaternion.Inverse(transform.rotation) * (player.transform.position - transform.position);
        if (Mathf.Abs(d.x) < hitHalfSize.x * s && Mathf.Abs(d.y) < hitHalfSize.y * s && Mathf.Abs(d.z) < hitHalfSize.z * s)
            player.TakeDamage(damage);
    }

    void Update()
    {
        if (spinner != null) spinner.Rotate(0f, 140f * Time.deltaTime, 0f, Space.Self);
    }
}
