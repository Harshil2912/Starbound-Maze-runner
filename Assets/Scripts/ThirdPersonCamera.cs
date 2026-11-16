using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 6f, -9f);
    public float smooth = 8f;
    public float minFollowY = 0.5f;

    // Main-menu mode: slowly orbit a point instead of following the cube.
    public bool menuMode;
    public Vector3 menuCenter;
    public float menuRadius = 46f;
    public float menuHeight = 26f;

    float shakeTime, shakeDur, shakeMag;

    Vector3 Focus()
    {
        Vector3 p = target.position;
        p.y = Mathf.Max(p.y, minFollowY);
        return p;
    }

    public void SetMenuMode(bool on, Vector3 center)
    {
        menuMode = on;
        menuCenter = center;
        if (!on) Snap();
    }

    public void Shake(float duration, float magnitude)
    {
        shakeDur = Mathf.Max(0.01f, duration);
        shakeTime = duration;
        shakeMag = magnitude;
    }

    public void Snap()
    {
        if (target == null) return;
        shakeTime = 0f;
        transform.position = Focus() + offset;
        transform.LookAt(target.position + Vector3.up * 0.5f);
    }

    void LateUpdate()
    {
        if (menuMode)
        {
            float a = Time.unscaledTime * 0.15f;
            transform.position = menuCenter + new Vector3(Mathf.Cos(a) * menuRadius, menuHeight, Mathf.Sin(a) * menuRadius);
            transform.LookAt(menuCenter);
            return;
        }
        if (target == null) return;
        Vector3 desired = Focus() + offset;
        transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-smooth * Time.unscaledDeltaTime));
        transform.LookAt(target.position + Vector3.up * 0.5f);
        if (shakeTime > 0f)
        {
            shakeTime -= Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(shakeTime / shakeDur);
            transform.position += Random.insideUnitSphere * shakeMag * k;
        }
    }
}
