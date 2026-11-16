using UnityEngine;

public class CrumblePlatform : MonoBehaviour
{
    public float delayBeforeFall = 0.8f;
    public float fallSpeed = 14f;
    public float respawnDelay = 4.0f;
    public ParticleSystem breakParticles;
    public Renderer padRenderer;

    Vector3 initialLocalPos;
    Quaternion initialLocalRot;
    Collider col;
    bool isStepped;
    bool isFalling;
    bool isCollapsed;
    float stepTimer;
    float respawnTimer;

    static readonly Collider[] buf = new Collider[8];

    void Awake()
    {
        initialLocalPos = transform.localPosition;
        initialLocalRot = transform.localRotation;
        col = GetComponent<Collider>();
        if (padRenderer == null) padRenderer = GetComponentInChildren<Renderer>();
        if (breakParticles != null)
        {
            var main = breakParticles.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        }
    }

    void OnEnable()
    {
        ResetPlatform();
    }

    public void ResetPlatform()
    {
        transform.localPosition = initialLocalPos;
        transform.localRotation = initialLocalRot;
        isStepped = false;
        isFalling = false;
        isCollapsed = false;
        stepTimer = 0f;
        respawnTimer = 0f;
        if (col != null) col.enabled = true;
        if (padRenderer != null) padRenderer.enabled = true;
    }

    void FixedUpdate()
    {
        if (isCollapsed)
        {
            respawnTimer += Time.fixedDeltaTime;
            if (respawnTimer >= respawnDelay)
            {
                ResetPlatform();
            }
            return;
        }

        if (isFalling)
        {
            transform.localPosition += Vector3.down * fallSpeed * Time.fixedDeltaTime;
            if (transform.localPosition.y < initialLocalPos.y - 12f)
            {
                isFalling = false;
                isCollapsed = true;
                if (padRenderer != null) padRenderer.enabled = false;
            }
            return;
        }

        if (!isStepped)
        {
            // Check if player is standing on top (scaled to parent diorama space)
            float s = transform.parent != null ? Mathf.Max(0.001f, transform.parent.lossyScale.x) : Mathf.Max(0.001f, transform.lossyScale.x);
            Vector3 probeCenter = transform.position + transform.up * (0.4f * s);
            Vector3 probeHalf = new Vector3(1.0f * s, 0.3f * s, 1.0f * s);
            int count = Physics.OverlapBoxNonAlloc(probeCenter, probeHalf, buf, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (buf[i].GetComponentInParent<CubeController>() != null)
                {
                    isStepped = true;
                    stepTimer = 0f;
                    GameAudioManager.Instance?.PlayCrumble();
                    HapticsManager.LightTap();
                    break;
                }
            }
        }
        else
        {
            stepTimer += Time.fixedDeltaTime;
            // Shake effect in local coordinates
            float shake = Mathf.Sin(stepTimer * 40f) * 0.05f;
            transform.localPosition = initialLocalPos + new Vector3(shake, 0f, 0f);

            if (stepTimer >= delayBeforeFall)
            {
                isFalling = true;
                if (col != null) col.enabled = false;
                if (breakParticles != null) breakParticles.Play();
            }
        }
    }
}
