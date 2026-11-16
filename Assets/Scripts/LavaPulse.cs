using System.Collections.Generic;
using UnityEngine;

// Makes the lava walls slowly pulse (uses property blocks, so no material assets are changed).
public class LavaPulse : MonoBehaviour
{
    public Color baseColor = new Color(1f, 0.10f, 0.06f);
    public float min = 1.8f;
    public float max = 3.6f;

    Renderer[] rs;
    MaterialPropertyBlock mpb;
    float next;
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    void OnEnable()
    {
        var all = GetComponentsInChildren<Renderer>();
        var list = new List<Renderer>(all.Length);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && !all[i].gameObject.name.StartsWith("HaloFX"))
                list.Add(all[i]);
        }
        rs = list.ToArray();
        if (mpb == null) mpb = new MaterialPropertyBlock();
        next = 0f;
    }

    void Update()
    {
        if (Time.unscaledTime < next) return;
        next = Time.unscaledTime + 0.1f;
        float k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2f);
        mpb.SetColor(EmissionColorId, baseColor * Mathf.Lerp(min, max, k));
        for (int i = 0; i < rs.Length; i++)
        {
            if (rs[i] != null) rs[i].SetPropertyBlock(mpb);
        }
    }
}
