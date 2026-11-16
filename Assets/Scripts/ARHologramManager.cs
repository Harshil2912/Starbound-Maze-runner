using System.Collections.Generic;
using UnityEngine;

public class ARHologramManager : MonoBehaviour
{
    static ARHologramManager _instance;
    public static ARHologramManager Instance
    {
        get
        {
            if (_instance == null) _instance = FindAnyObjectByType<ARHologramManager>();
            return _instance;
        }
    }

    [Header("Hologram Settings (Balanced & Subtle Lighting)")]
    public bool isHologramActive = false;
    public Color holoBaseColor = new Color(0.12f, 0.32f, 0.38f, 0.78f);
    public Color rimGlowColor = new Color(0.20f, 0.65f, 0.75f, 1.0f);
    public Color scanBeamColor = new Color(0.30f, 0.70f, 0.78f, 1.0f);
    [Range(0.5f, 6f)] public float rimPower = 2.8f;
    [Range(0f, 3f)] public float scanIntensity = 0.22f;
    [Range(0f, 1f)] public float textureDetailMix = 0.72f;
    [Range(0.1f, 1f)] public float hologramOpacity = 0.76f;

    [Header("Exclusions")]
    public Transform playerTransform;

    [Header("Hierarchy Anchors")]
    public Transform arContentRoot;
    public Transform activeLevelAnchor;

    Shader hologramShader;
    readonly Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    readonly Dictionary<Material, Material> hologramCache = new Dictionary<Material, Material>();

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        hologramShader = Shader.Find("Custom/AR_HologramOverlay");

        ResolveReferences();
    }

    void ResolveReferences()
    {
        if (arContentRoot == null)
        {
            var rootObj = GameObject.Find("ARContentRoot");
            if (rootObj != null) arContentRoot = rootObj.transform;
        }

        if (activeLevelAnchor == null)
        {
            var anchorObj = GameObject.Find("ActiveLevelAnchor");
            if (anchorObj != null) activeLevelAnchor = anchorObj.transform;
        }

        if (playerTransform == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
            else
            {
                var pc = FindAnyObjectByType<CubeController>();
                if (pc != null) playerTransform = pc.transform;
            }
        }
    }

    public void SetHologramMode(bool active)
    {
        if (!Application.isPlaying) return;

        isHologramActive = active;
        if (active)
        {
            ApplyHologramToScene();
        }
        else
        {
            RestoreOriginalMaterials();
        }
    }

    public void UpdateHologramScale(float newScale)
    {
        float scanFreq = (newScale < 0.5f) ? (0.09f / Mathf.Max(0.001f, newScale)) : 0.08f;
        foreach (var mat in hologramCache.Values)
        {
            if (mat != null) mat.SetFloat("_ScanFreq", scanFreq);
        }
    }

    public void ApplyHologramToScene()
    {
        if (!Application.isPlaying) return;

        if (hologramShader == null)
            hologramShader = Shader.Find("Custom/AR_HologramOverlay");

        ResolveReferences();

        // Target all active renderers under ARContentRoot (includes BOTH Base platform and ActiveLevelAnchor levels 1-5)
        Renderer[] renderers;
        if (arContentRoot != null)
        {
            renderers = arContentRoot.GetComponentsInChildren<MeshRenderer>(false);
        }
        else if (activeLevelAnchor != null)
        {
            renderers = activeLevelAnchor.GetComponentsInChildren<MeshRenderer>(false);
        }
        else
        {
            renderers = FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        }

        foreach (var r in renderers)
        {
            if (r == null) continue;
            ApplyHologramToRenderer(r);
        }
    }

    bool ShouldExcludeRenderer(Renderer r)
    {
        if (r == null) return true;
        if (playerTransform != null && (r.transform == playerTransform || r.transform.IsChildOf(playerTransform)))
            return true;
        if (r.GetComponentInParent<Canvas>() != null)
            return true;

        string objName = r.gameObject.name;
        if (objName.Contains("ShadowCatcher") ||
            objName.Contains("AR_ShadowCatcherPlane") ||
            objName.Contains("AR_PlacementReticle") ||
            objName.Contains("AR_TablePlane") ||
            objName.Contains("HaloFX"))
        {
            return true;
        }
        return false;
    }

    public void ApplyHologramToRenderer(Renderer r)
    {
        if (!Application.isPlaying || r == null || hologramShader == null) return;
        if (ShouldExcludeRenderer(r)) return;

        if (!originalMaterials.ContainsKey(r))
        {
            originalMaterials[r] = r.sharedMaterials;
        }

        var origMats = originalMaterials[r];
        var holoMats = new Material[origMats.Length];

        for (int i = 0; i < origMats.Length; i++)
        {
            var orig = origMats[i];
            if (orig == null) continue;

            if (!hologramCache.TryGetValue(orig, out var holoMat) || holoMat == null)
            {
                holoMat = new Material(hologramShader);
                holoMat.name = orig.name + "_AR_Holo";
                holoMat.enableInstancing = true;

                bool hasTex = false;
                if (orig.HasProperty("_BaseMap") && orig.GetTexture("_BaseMap") != null)
                {
                    holoMat.SetTexture("_BaseMap", orig.GetTexture("_BaseMap"));
                    holoMat.SetTextureScale("_BaseMap", orig.GetTextureScale("_BaseMap"));
                    holoMat.SetTextureOffset("_BaseMap", orig.GetTextureOffset("_BaseMap"));
                    hasTex = true;
                }
                else if (orig.HasProperty("_MainTex") && orig.GetTexture("_MainTex") != null)
                {
                    holoMat.SetTexture("_BaseMap", orig.GetTexture("_MainTex"));
                    holoMat.SetTextureScale("_BaseMap", orig.GetTextureScale("_MainTex"));
                    holoMat.SetTextureOffset("_BaseMap", orig.GetTextureOffset("_MainTex"));
                    hasTex = true;
                }

                ResolveCategoryAppearance(r, orig, hasTex, out Color tintBase, out Color tintRim, out Color tintScan, out Color tintEmit, out float alpha);

                holoMat.SetColor("_BaseColor", tintBase);
                holoMat.SetColor("_RimColor", tintRim);
                holoMat.SetColor("_ScanColor", tintScan);
                holoMat.SetColor("_EmissionColor", tintEmit);
                holoMat.SetFloat("_RimPower", rimPower);
                holoMat.SetFloat("_ScanIntensity", scanIntensity);
                holoMat.SetFloat("_TextureMix", textureDetailMix);
                holoMat.SetFloat("_HoloAlpha", alpha);

                float s = (arContentRoot != null) ? arContentRoot.lossyScale.x : 0.020f;
                float scanFreq = (s < 0.5f) ? (0.09f / Mathf.Max(0.001f, s)) : 0.08f;
                holoMat.SetFloat("_ScanFreq", scanFreq);

                hologramCache[orig] = holoMat;
            }

            holoMats[i] = holoMat;
        }

        r.sharedMaterials = holoMats;
    }

    void ResolveCategoryAppearance(Renderer r, Material orig, bool hasTex, out Color baseCol, out Color rimCol, out Color scanCol, out Color emitCol, out float alpha)
    {
        string matName = orig != null ? orig.name : string.Empty;

        // 1. Lava Walls: Glowing crimson Red
        if (matName.Contains("L4_Lava") || r.GetComponentInParent<LavaWall>() != null)
        {
            baseCol = new Color(0.80f, 0.08f, 0.05f, 0.86f);
            rimCol = new Color(1.0f, 0.16f, 0.10f, 1.0f);
            scanCol = new Color(1.0f, 0.24f, 0.12f, 1.0f);
            emitCol = new Color(1.9f, 0.12f, 0.06f, 1.0f);
            alpha = 0.84f;
            return;
        }

        // 2. Laser Warn (Amber) & Fire / Lens (Red)
        if (matName.Contains("L5_LaserWarn"))
        {
            baseCol = new Color(0.88f, 0.42f, 0.08f, 0.85f);
            rimCol = new Color(1.0f, 0.62f, 0.14f, 1.0f);
            scanCol = rimCol;
            emitCol = new Color(1.3f, 0.50f, 0.08f, 1.0f);
            alpha = 0.84f;
            return;
        }

        if (matName.Contains("L5_Laser") || r.gameObject.name.Contains("Lens"))
        {
            baseCol = new Color(0.90f, 0.08f, 0.06f, 0.90f);
            rimCol = new Color(1.0f, 0.18f, 0.10f, 1.0f);
            scanCol = rimCol;
            emitCol = new Color(2.5f, 0.16f, 0.10f, 1.0f);
            alpha = 0.88f;
            return;
        }

        // 3. Laser Balls / Turret Projectiles / Turret Eye: Yellow
        if (matName.Contains("L3_Projectile") || matName.Contains("L5_Bolt") || matName.Contains("L3_TurretEye") || r.GetComponent<Projectile>() != null)
        {
            baseCol = new Color(0.96f, 0.85f, 0.12f, 0.92f);
            rimCol = new Color(1.0f, 0.96f, 0.28f, 1.0f);
            scanCol = rimCol;
            emitCol = new Color(2.8f, 2.3f, 0.25f, 1.0f);
            alpha = 0.90f;
            return;
        }

        // 4. Teleporters: Preserve Pair A (Purple) & Pair B (Green)
        if (matName.Contains("L4_TeleA"))
        {
            baseCol = new Color(0.64f, 0.22f, 0.96f, 0.86f);
            rimCol = new Color(0.84f, 0.42f, 1.0f, 1.0f);
            scanCol = rimCol;
            emitCol = new Color(1.6f, 0.48f, 2.4f, 1.0f);
            alpha = 0.86f;
            return;
        }

        if (matName.Contains("L4_TeleB"))
        {
            baseCol = new Color(0.14f, 0.92f, 0.44f, 0.86f);
            rimCol = new Color(0.32f, 1.0f, 0.62f, 1.0f);
            scanCol = rimCol;
            emitCol = new Color(0.35f, 2.1f, 0.95f, 1.0f);
            alpha = 0.86f;
            return;
        }

        // 5. Shield Orb: Blue
        if (matName.Contains("L4_Shield") || r.GetComponentInParent<ShieldOrb>() != null)
        {
            baseCol = new Color(0.18f, 0.54f, 1.0f, 0.88f);
            rimCol = new Color(0.38f, 0.82f, 1.0f, 1.0f);
            scanCol = rimCol;
            emitCol = new Color(0.45f, 1.25f, 2.4f, 1.0f);
            alpha = 0.88f;
            return;
        }

        // 6. Goal Flag Red accent
        if (matName.Contains("GoalFlagRed"))
        {
            baseCol = new Color(0.84f, 0.08f, 0.06f, 0.88f);
            rimCol = new Color(1.0f, 0.22f, 0.14f, 1.0f);
            scanCol = rimCol;
            emitCol = new Color(1.6f, 0.14f, 0.10f, 1.0f);
            alpha = 0.86f;
            return;
        }

        // 7. Crate (Level 3 Puzzle Yellow Cube): Vibrant Golden Yellow with amber glow rim
        if (matName.Contains("L3_Crate") || r.GetComponentInParent<Crate>() != null)
        {
            baseCol = new Color(0.98f, 0.78f, 0.12f, 0.96f);
            rimCol = new Color(1.0f, 0.92f, 0.32f, 1.0f);
            scanCol = rimCol;
            emitCol = new Color(1.8f, 1.35f, 0.16f, 1.0f);
            alpha = 0.96f;
            return;
        }

        // 8. Heart Pickups: Vibrant Ruby Red with soft pink rim glow
        if (matName.Contains("Heart") || r.GetComponentInParent<HeartPickup>() != null)
        {
            baseCol = new Color(0.95f, 0.12f, 0.28f, 0.94f);
            rimCol = new Color(1.0f, 0.35f, 0.55f, 1.0f);
            scanCol = rimCol;
            emitCol = new Color(2.0f, 0.20f, 0.40f, 1.0f);
            alpha = 0.94f;
            return;
        }

        // 9. Energy Cores / Orbs (Level 4 Collectible): Radiant Electric Cyan & Turquoise Core Glow
        if (matName.Contains("L5_Orb") || matName.Contains("EnergyCore") || r.GetComponentInParent<EnergyCore>() != null)
        {
            baseCol = new Color(0.08f, 0.88f, 0.95f, 0.96f);
            rimCol = new Color(0.40f, 1.0f, 0.92f, 1.0f);
            scanCol = rimCol;
            emitCol = new Color(0.35f, 2.5f, 2.8f, 1.0f);
            alpha = 0.96f;
            return;
        }

        // Default Frosted Cyan Hologram for structural walls, floors, doors & plates
        baseCol = holoBaseColor;
        if (!hasTex)
        {
            Color srcColor = Color.white;
            if (orig.HasProperty("_BaseColor")) srcColor = orig.GetColor("_BaseColor");
            else if (orig.HasProperty("_Color")) srcColor = orig.GetColor("_Color");
            baseCol = Color.Lerp(holoBaseColor, srcColor * holoBaseColor * 2.2f, 0.35f);
            baseCol.a = holoBaseColor.a;
        }
        rimCol = rimGlowColor;
        scanCol = scanBeamColor;
        emitCol = Color.clear;
        alpha = hologramOpacity;
    }

    public void RestoreOriginalMaterials()
    {
        foreach (var kvp in originalMaterials)
        {
            if (kvp.Key != null && kvp.Value != null)
            {
                kvp.Key.sharedMaterials = kvp.Value;
            }
        }
        originalMaterials.Clear();
    }

    void OnDestroy()
    {
        RestoreOriginalMaterials();
        foreach (var kvp in hologramCache)
        {
            if (kvp.Value != null) Destroy(kvp.Value);
        }
        hologramCache.Clear();
    }
}
