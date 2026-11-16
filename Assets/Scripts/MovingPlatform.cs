using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Vector3 endPos;
    public float speed = 2.2f;
    public float pauseTime = 0.8f;

    Vector3 startPos;
    Vector3 lastPos;
    bool toEnd = true;
    float pauseTimer;
    CubeController rider;

    public Vector3 CurrentVelocity { get; private set; }

    void Awake()
    {
        startPos = transform.localPosition;
        lastPos = transform.localPosition;
    }

    void OnEnable()
    {
        transform.localPosition = startPos;
        lastPos = transform.localPosition;
        toEnd = true;
        pauseTimer = 0f;
        CurrentVelocity = Vector3.zero;
        rider = null;
    }

    void FixedUpdate()
    {
        Vector3 curPos = transform.localPosition;

        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.fixedDeltaTime;
        }
        else
        {
            Vector3 target = toEnd ? endPos : startPos;
            curPos = Vector3.MoveTowards(curPos, target, speed * Time.fixedDeltaTime);
            transform.localPosition = curPos;

            if (Vector3.Distance(curPos, target) < 0.01f)
            {
                toEnd = !toEnd;
                pauseTimer = pauseTime;
            }
        }

        Vector3 localDelta = curPos - lastPos;
        Vector3 delta = (transform.parent != null) ? transform.parent.TransformVector(localDelta) : localDelta;
        CurrentVelocity = (Time.fixedDeltaTime > 0.0001f) ? (delta / Time.fixedDeltaTime) : Vector3.zero;
        lastPos = curPos;

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
