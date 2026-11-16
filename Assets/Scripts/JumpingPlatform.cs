using UnityEngine;

public class JumpingPlatform : MonoBehaviour
{
    public Vector3 moveOffset = new Vector3(0, 0, 6f);
    public float moveSpeed = 2f;
    public bool isMoving = true;
    public float pauseTime = 0.5f;

    Vector3 startLocalPos;
    Vector3 targetLocalPos;
    Vector3 lastLocalPos;
    float phase;
    CubeController rider;

    public Vector3 CurrentVelocity { get; private set; }

    void Awake()
    {
        startLocalPos = transform.localPosition;
        targetLocalPos = startLocalPos + moveOffset;
        lastLocalPos = transform.localPosition;
    }

    void OnEnable()
    {
        transform.localPosition = startLocalPos;
        lastLocalPos = startLocalPos;
        CurrentVelocity = Vector3.zero;
        rider = null;
    }

    void FixedUpdate()
    {
        Vector3 curLocalPos = transform.localPosition;
        if (isMoving && moveOffset.sqrMagnitude > 0.01f)
        {
            phase += Time.fixedDeltaTime * moveSpeed;
            float t = (Mathf.Sin(phase) + 1f) * 0.5f;
            curLocalPos = Vector3.Lerp(startLocalPos, targetLocalPos, t);
            transform.localPosition = curLocalPos;
        }

        Vector3 localDelta = curLocalPos - lastLocalPos;
        Vector3 delta = (transform.parent != null) ? transform.parent.TransformVector(localDelta) : localDelta;
        CurrentVelocity = (Time.fixedDeltaTime > 0.0001f) ? (delta / Time.fixedDeltaTime) : Vector3.zero;
        lastLocalPos = curLocalPos;

        if (rider != null)
        {
            var rb = rider.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position += delta;
            }
            else
            {
                rider.transform.position += delta;
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        CheckRider(collision);
    }

    void OnCollisionStay(Collision collision)
    {
        if (rider == null) CheckRider(collision);
    }

    void OnCollisionExit(Collision collision)
    {
        var player = collision.gameObject.GetComponent<CubeController>();
        if (player != null && rider == player)
        {
            rider = null;
        }
    }

    void CheckRider(Collision collision)
    {
        var player = collision.gameObject.GetComponent<CubeController>();
        if (player == null) return;

        int count = collision.contactCount;
        for (int i = 0; i < count; i++)
        {
            if (collision.GetContact(i).normal.y < -0.5f)
            {
                rider = player;
                break;
            }
        }
    }
}
