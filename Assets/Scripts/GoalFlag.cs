using UnityEngine;

// The glowing red flag that marks the end of every level.
public class GoalFlag : MonoBehaviour
{
    public Transform flagPivot;
    public Light glow;

    float baseIntensity;

    void Awake() { if (glow != null) baseIntensity = glow.intensity; }

    void Update()
    {
        float t = Time.time;
        if (flagPivot != null) flagPivot.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 2.2f) * 14f, 0f);
        if (glow != null) glow.intensity = baseIntensity * (1f + 0.18f * Mathf.Sin(t * 3.1f));
    }
}
