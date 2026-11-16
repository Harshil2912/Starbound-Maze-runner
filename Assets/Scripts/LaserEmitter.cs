using UnityEngine;

public class LaserEmitter : MonoBehaviour
{
    public float onTime = 1.4f;
    public float offTime = 2.2f;
    public float warnTime = 0.5f;
    public float phase = 0f;
    public float beamLength = 7.2f; // length in units
    public float beamRadius = 0.45f;
    public Vector3 beamDirection = Vector3.forward;
    public Transform emitterVisual;
    public LineRenderer beamLine;
    public Material warnMaterial;
    public Material fireMaterial;
    public Material beamHaloMaterial;
    public Light beamLight;
    public ParticleSystem fireParticles;
    public int damage = 1;
    public float damageInterval = 0.8f;

    float cycleTime;
    float nextDamageTime;
    bool wasFiring;
    PlayerHealth playerHealth;
    LineRenderer haloLine;
    static Material fallbackHaloMat;

    public enum LaserState { Off, Warning, Firing }
    public LaserState State { get; private set; }

    void Awake()
    {
        if (beamLine == null) beamLine = GetComponentInChildren<LineRenderer>();
        if (beamLine != null) beamLine.useWorldSpace = true;
        EnsureHaloLine();
        if (fireParticles != null)
        {
            var main = fireParticles.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        }
    }

    void EnsureHaloLine()
    {
        if (haloLine != null) return;
        var existing = transform.Find("HaloFX");
        if (existing != null)
        {
            haloLine = existing.GetComponent<LineRenderer>();
        }
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
        cycleTime = phase;
        wasFiring = false;
        nextDamageTime = 0f;
        UpdateVisuals(LaserState.Off, false);
    }

    void FixedUpdate()
    {
        cycleTime += Time.fixedDeltaTime;
        float totalCycle = offTime + onTime;
        if (totalCycle <= 0.01f) totalCycle = 1f;
        float mod = cycleTime % totalCycle;

        LaserState newState;
        bool flickerWarn = false;

        if (mod < (offTime - warnTime))
        {
            newState = LaserState.Off;
        }
        else if (mod < offTime)
        {
            newState = LaserState.Warning;
            // 0.4-0.5s warning flicker
            flickerWarn = Mathf.Sin(cycleTime * 45f) > 0f;
        }
        else
        {
            newState = LaserState.Firing;
        }

        if (newState == LaserState.Firing && !wasFiring)
        {
            if (fireParticles != null) fireParticles.Play();
            if (playerHealth != null)
            {
                float s = Mathf.Max(0.001f, transform.lossyScale.x);
                if ((playerHealth.transform.position - transform.position).sqrMagnitude <= (18f * s) * (18f * s))
                    GameAudioManager.Instance?.PlayLaserWarn();
            }
        }
        wasFiring = (newState == LaserState.Firing);
        State = newState;

        UpdateVisuals(newState, flickerWarn);

        if (newState == LaserState.Firing)
        {
            CheckDamage();
        }
    }

    void UpdateVisuals(LaserState state, bool flickerWarn)
    {
        float s = Mathf.Max(0.001f, transform.lossyScale.x);
        Vector3 dir = transform.TransformDirection(beamDirection.normalized).normalized;
        Vector3 start = transform.position;
        Vector3 end = start + dir * (beamLength * s);

        if (beamLine != null)
        {
            if (state == LaserState.Off)
            {
                beamLine.enabled = false;
            }
            else if (state == LaserState.Warning)
            {
                beamLine.enabled = flickerWarn;
                if (warnMaterial != null) beamLine.sharedMaterial = warnMaterial;
                beamLine.startWidth = 0.12f * s;
                beamLine.endWidth = 0.12f * s;
                beamLine.SetPosition(0, start);
                beamLine.SetPosition(1, end);
            }
            else // Firing
            {
                beamLine.enabled = true;
                if (fireMaterial != null) beamLine.sharedMaterial = fireMaterial;
                beamLine.startWidth = 0.38f * s;
                beamLine.endWidth = 0.38f * s;
                beamLine.SetPosition(0, start);
                beamLine.SetPosition(1, end);
            }
        }

        if (haloLine != null)
        {
            if (state == LaserState.Firing)
            {
                haloLine.enabled = true;
                haloLine.startWidth = 1.05f * s;
                haloLine.endWidth = 1.05f * s;
                haloLine.SetPosition(0, start);
                haloLine.SetPosition(1, end);
            }
            else if (state == LaserState.Warning && flickerWarn)
            {
                haloLine.enabled = true;
                haloLine.startWidth = 0.36f * s;
                haloLine.endWidth = 0.36f * s;
                haloLine.SetPosition(0, start);
                haloLine.SetPosition(1, end);
            }
            else
            {
                haloLine.enabled = false;
            }
        }

        if (beamLight != null)
        {
            beamLight.enabled = (state == LaserState.Firing) || (state == LaserState.Warning && flickerWarn);
            beamLight.color = (state == LaserState.Warning) ? new Color(1f, 0.50f, 0.1f) : new Color(1f, 0.10f, 0.08f);
        }
    }

    void CheckDamage()
    {
        if (Time.time < nextDamageTime) return;

        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth == null) return;
        }

        float s = Mathf.Max(0.001f, transform.lossyScale.x);
        float scaledLength = beamLength * s;
        Vector3 start = transform.position;
        Vector3 dir = transform.TransformDirection(beamDirection.normalized).normalized;
        Vector3 playerPos = playerHealth.transform.position;

        // Project player position onto beam line segment
        Vector3 toPlayer = playerPos - start;
        float proj = Vector3.Dot(toPlayer, dir);
        if (proj >= -0.2f * s && proj <= scaledLength + 0.2f * s)
        {
            Vector3 closestPoint = start + dir * Mathf.Clamp(proj, 0f, scaledLength);
            float dist = Vector3.Distance(closestPoint, playerPos);
            if (dist < (beamRadius + 0.5f) * s) // cube half-size is 0.5
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
