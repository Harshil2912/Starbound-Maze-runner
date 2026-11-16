using UnityEngine;

public static class HapticsManager
{
    const string PrefHaptics = "MR_HapticsEnabled";

    static bool initialized;
    static bool enabled = true;
    static float suppressUntilTime = -10f;
    static float lastAnyTime = -10f;
    static float lastStepTime = -10f;
    static float lastBumpTime = -10f;
    static float lastHeavyTime = -10f;

#if UNITY_ANDROID && !UNITY_EDITOR
    static AndroidJavaObject vibrator;
    static AndroidJavaClass vibrationEffectClass;
    static bool androidInitDone;
    static bool hasVibrator;
    static int sdkInt;
#endif

    public static bool Enabled
    {
        get
        {
            EnsureInit();
            return enabled;
        }
        set
        {
            EnsureInit();
            enabled = value;
            PlayerPrefs.SetInt(PrefHaptics, enabled ? 1 : 0);
            PlayerPrefs.Save();
            if (!enabled) CancelAll();
        }
    }

    public static void CancelAll()
    {
        lastAnyTime = Time.unscaledTime;
        lastStepTime = lastAnyTime;
        lastBumpTime = lastAnyTime;
        lastHeavyTime = lastAnyTime;
#if UNITY_ANDROID && !UNITY_EDITOR
        EnsureAndroidVibrator();
        if (vibrator != null)
        {
            try { vibrator.Call("cancel"); }
            catch (System.Exception) { }
        }
#endif
    }

    public static void SuppressFor(float seconds)
    {
        suppressUntilTime = Mathf.Max(suppressUntilTime, Time.unscaledTime + Mathf.Max(0f, seconds));
        CancelAll();
    }

    static void EnsureInit()
    {
        if (initialized) return;
        initialized = true;
        enabled = PlayerPrefs.GetInt(PrefHaptics, 1) == 1;
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    static void EnsureAndroidVibrator()
    {
        if (androidInitDone) return;
        androidInitDone = true;
        try
        {
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                sdkInt = version.GetStatic<int>("SDK_INT");
            }
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                if (activity != null)
                {
                    vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    if (vibrator != null)
                    {
                        hasVibrator = vibrator.Call<bool>("hasVibrator");
                    }
                }
            }
            if (sdkInt >= 26)
            {
                vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
            }
        }
        catch (System.Exception)
        {
            hasVibrator = false;
            vibrator = null;
        }
    }
#endif

    static void Vibrate(long durationMs, int amplitude, float globalCooldown = 0.025f)
    {
        EnsureInit();
        float now = Time.unscaledTime;
        if (!enabled || now < suppressUntilTime) return;

        if (now - lastAnyTime < globalCooldown) return;
        lastAnyTime = now;

#if UNITY_ANDROID && !UNITY_EDITOR
        EnsureAndroidVibrator();
        if (!hasVibrator || vibrator == null) return;
        try
        {
            if (sdkInt >= 26 && vibrationEffectClass != null)
            {
                int amp = Mathf.Clamp(amplitude, 1, 255);
                using (var effect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", durationMs, amp))
                {
                    vibrator.Call("vibrate", effect);
                }
            }
            else
            {
                vibrator.Call("vibrate", durationMs);
            }
        }
        catch (System.Exception) { }
#endif
    }

    public static void MoveStep()
    {
        float now = Time.unscaledTime;
        if (now - lastStepTime < 0.24f) return;
        lastStepTime = now;
        Vibrate(12L, 38, 0.02f);
    }

    public static void Jump()
    {
        Vibrate(20L, 90, 0.05f);
    }

    public static void Land()
    {
        Vibrate(15L, 62, 0.08f);
    }

    public static void BumpWall()
    {
        float now = Time.unscaledTime;
        if (now - lastBumpTime < 0.18f) return;
        lastBumpTime = now;
        Vibrate(28L, 130, 0.03f);
    }

    public static void TriggerClick()
    {
        Vibrate(16L, 75, 0.03f);
    }

    public static void LightTap()
    {
        Vibrate(12L, 55, 0.025f);
    }

    public static void Pickup()
    {
        Vibrate(26L, 135, 0.04f);
    }

    public static void HeavyHit()
    {
        float now = Time.unscaledTime;
        if (now - lastHeavyTime < 0.18f) return;
        lastHeavyTime = now;
        Vibrate(55L, 220, 0.03f);
    }

    public static void Death()
    {
        Vibrate(115L, 255, 0.05f);
    }

    public static void GoalSuccess()
    {
        Vibrate(65L, 195, 0.05f);
    }
}

