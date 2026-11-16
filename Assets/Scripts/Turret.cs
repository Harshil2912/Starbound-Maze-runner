using UnityEngine;

public class Turret : MonoBehaviour
{
    public Transform muzzle;
    public Material projectileMaterial;
    public Material haloMaterial;
    public float interval = 2.4f;
    public float initialDelay = 0.5f;
    public float range = 26f;
    public float projectileSpeed = 9f;
    public int burstCount = 1;
    public float burstSpacing = 0.15f;

    float timer;
    PlayerHealth player;
    int burstRemaining;
    float burstTimer;
    static Material fallbackHaloMat;
    static Mesh cachedSphereMesh;

    static Mesh GetSphereMesh()
    {
        if (cachedSphereMesh != null) return cachedSphereMesh;
        var tmp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var mf = tmp.GetComponent<MeshFilter>();
        if (mf != null) cachedSphereMesh = mf.sharedMesh;
        DestroyImmediate(tmp);
        return cachedSphereMesh;
    }

    void OnEnable()
    {
        timer = initialDelay;
        burstRemaining = 0;
        burstTimer = 0f;
    }

    void FixedUpdate()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerHealth>();
            if (player == null) return;
        }

        // Handle active burst
        if (burstRemaining > 0)
        {
            burstTimer -= Time.fixedDeltaTime;
            if (burstTimer <= 0f)
            {
                SpawnBolt();
                burstRemaining--;
                burstTimer = burstSpacing;
            }
            return;
        }

        timer -= Time.fixedDeltaTime;
        if (timer > 0f) return;
        float r = range * Mathf.Max(0.001f, transform.lossyScale.x);
        if ((player.transform.position - transform.position).sqrMagnitude > r * r) { timer = 0.2f; return; }
        
        timer = interval;
        if (burstCount <= 1)
        {
            SpawnBolt();
        }
        else
        {
            SpawnBolt();
            burstRemaining = burstCount - 1;
            burstTimer = burstSpacing;
        }
    }

    void SpawnBolt()
    {
        if (muzzle == null) return;
        Mesh sphereMesh = GetSphereMesh();

        var go = new GameObject("Projectile");
        go.transform.position = muzzle.position;
        float s = Mathf.Max(0.001f, transform.lossyScale.x);
        go.transform.localScale = Vector3.one * (0.5f * s);
        if (transform.parent != null) go.transform.SetParent(transform.parent, true);
        go.AddComponent<MeshFilter>().sharedMesh = sphereMesh;
        var r = go.AddComponent<MeshRenderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        if (projectileMaterial != null) r.sharedMaterial = projectileMaterial;

        // Attach bright yellow halo sphere around each fired laser ball
        var halo = new GameObject("HaloFX");
        halo.transform.SetParent(go.transform, false);
        halo.transform.localPosition = Vector3.zero;
        halo.transform.localScale = Vector3.one * 2.15f;
        halo.AddComponent<MeshFilter>().sharedMesh = sphereMesh;
        var haloRend = halo.AddComponent<MeshRenderer>();
        haloRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        haloRend.receiveShadows = false;
        haloRend.sharedMaterial = GetYellowHaloMaterial();

        var p = go.AddComponent<Projectile>();
        p.direction = muzzle.forward;
        p.speed = projectileSpeed;
        GameAudioManager.Instance?.PlayTurretFire();
    }

    Material GetYellowHaloMaterial()
    {
        if (haloMaterial != null) return haloMaterial;
        if (fallbackHaloMat == null)
        {
            Shader s = Shader.Find("Custom/HaloGlow");
            if (s != null)
            {
                fallbackHaloMat = new Material(s);
                fallbackHaloMat.SetColor("_BaseColor", new Color(1f, 0.80f, 0.03f, 0.68f));
                fallbackHaloMat.SetFloat("_GlowIntensity", 1.55f);
                fallbackHaloMat.SetFloat("_FalloffPower", 2.8f);
                fallbackHaloMat.SetFloat("_PulseSpeed", 4.0f);
                fallbackHaloMat.SetFloat("_PulseAmount", 0.14f);
                fallbackHaloMat.SetFloat("_ShapeMode", 0.0f);
                fallbackHaloMat.SetFloat("_Cull", 2.0f);
            }
        }
        return fallbackHaloMat;
    }
}
