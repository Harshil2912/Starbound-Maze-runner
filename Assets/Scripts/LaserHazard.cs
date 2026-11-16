using UnityEngine;

[ExecuteAlways]
public class LaserHazard : MonoBehaviour
{
    public Transform emitterA;
    public Transform emitterB;
    public float damageInterval = 0.5f;
    public int damage = 1;
    public float knockSpeed = 12f;
    public bool sweeps;
    public Vector3 sweepOffset = new Vector3(0, 0, 5f);
    public float sweepSpeed = 2f;
    public bool intermittent;
    public float onTime = 2f;
    public float offTime = 1f;
    public float rotateSpeed = 0f;

    LineRenderer lr;
    LineRenderer haloLine;
    BoxCollider triggerCol;
    Light laserLight;
    Vector3 origPosA;
    Vector3 origPosB;
    float nextDamageTime;
    float cycleTimer;
    bool isFiring = true;
    static Material fallbackHaloMat;

    void Awake()
    {
        InitRefs();
    }

    void OnEnable()
    {
        InitRefs();
        UpdateBeam();
    }

    void InitRefs()
    {
        if (lr == null) lr = GetComponent<LineRenderer>();
        if (triggerCol == null) triggerCol = GetComponent<BoxCollider>();
        if (laserLight == null) laserLight = GetComponentInChildren<Light>();
        if (emitterA != null && origPosA == Vector3.zero) origPosA = emitterA.localPosition;
        if (emitterB != null && origPosB == Vector3.zero) origPosB = emitterB.localPosition;
        if (haloLine == null)
        {
            var existing = transform.Find("HaloFX");
            if (existing != null) haloLine = existing.GetComponent<LineRenderer>();
            if (haloLine == null && lr != null)
            {
                var go = new GameObject("HaloFX");
                go.transform.SetParent(transform, false);
                haloLine = go.AddComponent<LineRenderer>();
                haloLine.positionCount = 2;
                haloLine.numCapVertices = 4;
                haloLine.useWorldSpace = true;
                haloLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                haloLine.receiveShadows = false;
                haloLine.alignment = LineAlignment.View;
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
                haloLine.sharedMaterial = fallbackHaloMat;
            }
        }
    }

    void Update()
    {
        if (intermittent)
        {
            cycleTimer += Time.deltaTime;
            float total = onTime + offTime;
            float mod = cycleTimer % total;
            isFiring = (mod < onTime);
            if (lr != null) lr.enabled = isFiring;
            if (haloLine != null) haloLine.enabled = isFiring;
            if (triggerCol != null) triggerCol.enabled = isFiring;
            if (laserLight != null) laserLight.enabled = isFiring;
        }

        if (!isFiring) return;

        if (sweeps && emitterA != null && emitterB != null)
        {
            float t = Mathf.Sin(Time.time * sweepSpeed);
            emitterA.localPosition = origPosA + sweepOffset * t;
            emitterB.localPosition = origPosB + sweepOffset * t;
        }

        if (rotateSpeed != 0f)
        {
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        }

        UpdateBeam();
    }

    void UpdateBeam()
    {
        if (emitterA == null || emitterB == null) return;

        Vector3 pA = emitterA.position;
        Vector3 pB = emitterB.position;

        float sScale = Mathf.Max(0.001f, transform.lossyScale.x);
        if (lr != null)
        {
            lr.startWidth = 0.35f * sScale;
            lr.endWidth = 0.35f * sScale;
            lr.SetPosition(0, pA);
            lr.SetPosition(1, pB);
        }
        if (haloLine != null)
        {
            haloLine.startWidth = 0.98f * sScale;
            haloLine.endWidth = 0.98f * sScale;
            haloLine.SetPosition(0, pA);
            haloLine.SetPosition(1, pB);
        }

        if (triggerCol != null)
        {
            Vector3 mid = (pA + pB) * 0.5f;
            Vector3 dir = pB - pA;
            float length = dir.magnitude;
            float s = Mathf.Max(0.001f, triggerCol.transform.lossyScale.z);
            float localLength = length / s;

            triggerCol.transform.position = mid;
            if (length > 0.001f)
            {
                triggerCol.transform.rotation = Quaternion.LookRotation(dir);
            }
            triggerCol.size = new Vector3(0.5f, 0.5f, localLength);
        }

        if (laserLight != null)
        {
            laserLight.transform.position = (pA + pB) * 0.5f;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (!isFiring) return;
        if (Time.time < nextDamageTime) return;

        CubeController player = other.GetComponent<CubeController>();
        if (player == null) player = other.GetComponentInParent<CubeController>();

        if (player != null)
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null && health.TakeDamage(damage, customSfx: true))
            {
                GameAudioManager.Instance?.PlayDamageLaser();
                nextDamageTime = Time.time + damageInterval;
                Vector3 knockDir = (player.transform.position - transform.position).normalized;
                if (knockDir.sqrMagnitude < 0.01f) knockDir = Vector3.up;
                player.Knockback(knockDir, knockSpeed, 0.3f);
            }
        }
    }
}
