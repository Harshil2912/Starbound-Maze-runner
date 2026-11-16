using UnityEngine;

public class DecoyEffect : MonoBehaviour
{
    public float flickerSpeed = 18f;
    public float minAlpha = 0.15f;
    public float maxAlpha = 0.45f;
    public float minGlow = 0.1f;
    public float maxGlow = 0.8f;

    Renderer rend;
    Material mat;
    Color baseCol;

    void Awake()
    {
        rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            mat = rend.material;
            baseCol = mat.color;
        }
        var col = GetComponent<Collider>();
        if (col != null) Destroy(col);
    }

    void Update()
    {
        if (mat == null) return;
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0.5f);
        float a = Mathf.Lerp(minAlpha, maxAlpha, noise);
        float glow = Mathf.Lerp(minGlow, maxGlow, noise);

        Color c = baseCol;
        c.a = a;
        mat.color = c;
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.SetColor("_EmissionColor", baseCol * glow);
        }
    }
}
