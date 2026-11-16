using UnityEngine;

public class SweepLaser : MonoBehaviour
{
    public float beamLength = 14.4f; // 6 tiles * 2.4
    public float sweepAngle = 80f;
    public float sweepSpeed = 45f; // deg/s
    public float beamRadius = 0.45f;
    public int damage = 1;
    public float damageInterval = 0.8f;
    public LineRenderer beamLine;
    public Material beamMaterial;
    public Material beamHaloMaterial;
    public Light beamLight;

    float currentAngle;
    bool sweepingPositive = true;
    float nextDamageTime;
    PlayerHealth playerHealth;
    LineRenderer haloLine;
    static Material fallbackHaloMat;

    void Awake()
    {
        if (beamLine == null) beamLine = GetComponentInChildren<LineRenderer>();
        if (beamLine != null) beamLine.useWorldSpace = true;
        EnsureHaloLine();
    }

    void EnsureHaloLine()
    {
        if (haloLine != null) return;
        var existing = transform.Find("HaloFX");
        if (existing != null) haloLine = existing.GetComponent<LineRenderer>();
        if (haloLine == null)
        {
            var go = new GameObject("HaloFX");
            go.transform.SetParent(transform, false);
            haloLine = go.AddComponent<LineRenderer>();
            haloLine.positionCount = 2;
            haloLine.numCapVertices = 4;
            haloLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            haloLine.receiveShadows = false;
            haloLine.textureMode = LineTextureMode.Stretch;
            haloLine.alignment = LineAlignment.View;
        }
        haloLine.useWorldSpace = true;
        haloLine.sharedMaterial = GetHaloMaterial();
    }

    Material GetHaloMaterial()
    {
        if (beamHaloMaterial != null) return beamHaloMaterial;
        if (fallbackHaloMat == null)
        {
            Shader s = Shader.Find("Custom/HaloGlow");
            if (s != null)
            {
                fallbackHaloMat = new Material(s);
                fallbackHaloMat.SetColor("_BaseColor", new Color(1f, 0.07f, 0.04f, 0.72f));
                fallbackHaloMat.SetFloat("_GlowIntensity", 1.85f);
                fallbackHaloMat.SetFloat("_FalloffPower", 2.2f);
                fallbackHaloMat.SetFloat("_PulseSpeed", 6.0f);
                fallbackHaloMat.SetFloat("_PulseAmount", 0.12f);
                fallbackHaloMat.SetFloat("_ShapeMode", 3.0f);
                fallbackHaloMat.SetFloat("_Cull", 0.0f);
            }
        }
        return fallbackHaloMat;
    }

    void OnEnable()
    {
        currentAngle = 0f;
        sweepingPositive = true;
        nextDamageTime = 0f;
    }

    void FixedUpdate()
    {
        float delta = sweepSpeed * Time.fixedDeltaTime;
        if (sweepingPositive)
        {
            currentAngle += delta;
            if (currentAngle >= sweepAngle)
            {
                currentAngle = sweepAngle;
                sweepingPositive = false;
            }
        }
        else
        {
            currentAngle -= delta;
            if (currentAngle <= -sweepAngle)
            {
                currentAngle = -sweepAngle;
                sweepingPositive = true;
            }
        }

        float s = Mathf.Max(0.001f, transform.lossyScale.x);
        float scaledLength = beamLength * s;
        Vector3 localDir = Quaternion.Euler(0f, currentAngle, 0f) * Vector3.right; // around East (+X)
        Vector3 dir = transform.TransformDirection(localDir).normalized;
        Vector3 start = transform.position;
        Vector3 end = start + dir * scaledLength;

        if (beamLine != null)
        {
            if (beamMaterial != null) beamLine.sharedMaterial = beamMaterial;
            beamLine.startWidth = 0.35f * s;
            beamLine.endWidth = 0.35f * s;
            beamLine.SetPosition(0, start);
            beamLine.SetPosition(1, end);
        }

        if (haloLine != null)
        {
            haloLine.enabled = true;
            haloLine.startWidth = 0.98f * s;
            haloLine.endWidth = 0.98f * s;
            haloLine.SetPosition(0, start);
            haloLine.SetPosition(1, end);
        }

        if (Time.time >= nextDamageTime)
        {
            if (playerHealth == null) playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth != null)
            {
                Vector3 playerPos = playerHealth.transform.position;
                Vector3 toPlayer = playerPos - start;
                float proj = Vector3.Dot(toPlayer, dir);
                if (proj >= 0f && proj <= scaledLength)
                {
                    Vector3 closest = start + dir * proj;
                    if (Vector3.Distance(closest, playerPos) < (beamRadius + 0.5f) * s)
                    {
                        if (playerHealth.TakeDamage(damage, customSfx: true))
                        {
                            GameAudioManager.Instance?.PlayDamageLaser();
                            nextDamageTime = Time.time + damageInterval;
                        }
                    }
                }
            }
        }
    }
}
