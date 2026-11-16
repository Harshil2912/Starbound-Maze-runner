using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 3;
    public float invulnerableTime = 1.2f;
    public Transform shieldFx;

    public int Current { get; private set; }
    public float ShieldTime { get; private set; }
    public event Action<int, int> Changed;
    public event Action Damaged;
    public event Action Died;

    float invulnTimer;
    float damageSuppressionTimer;
    Renderer rend;
    CubeController controller;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        controller = GetComponent<CubeController>();
        Current = maxHealth;
    }

    void Start() { if (Changed != null) Changed(Current, maxHealth); }

    public bool IsInvulnerable { get { return invulnTimer > 0f || damageSuppressionTimer > 0f; } }
    public bool IsShielded { get { return ShieldTime > 0f; } }

    public void SuppressDamage(float seconds)
    {
        damageSuppressionTimer = Mathf.Max(damageSuppressionTimer, seconds);
    }

    public void GrantShield(float seconds)
    {
        ShieldTime = Mathf.Max(ShieldTime, seconds);
        if (controller != null) controller.Burst(new Color(0.3f, 0.7f, 1f), 22);
        GameAudioManager.Instance?.PlayShieldPickup();
        HapticsManager.Pickup();
    }

    // Hazards call this: ignored while shielded or in the post-hit grace period.
    public bool TakeDamage(int amount, bool customSfx = false)
    {
        if (IsInvulnerable || IsShielded || Current <= 0) return false;
        Apply(amount, customSfx);
        return true;
    }

    // Used for falling into a void: ignores both the grace period and the shield.
    public void ForceDamage(int amount, bool customSfx = false)
    {
        if (Current <= 0) return;
        Apply(amount, customSfx);
    }

    void Apply(int amount, bool customSfx)
    {
        if (damageSuppressionTimer > 0f) return;
        Current = Mathf.Max(0, Current - amount);
        invulnTimer = invulnerableTime;
        if (controller != null) controller.Hurt();
        if (!customSfx) GameAudioManager.Instance?.PlayDamageGeneric();
        HapticsManager.HeavyHit();
        if (Changed != null) Changed(Current, maxHealth);
        if (Damaged != null) Damaged();
        if (Current <= 0)
        {
            HapticsManager.Death();
            if (Died != null) Died();
        }
    }

    public bool Heal(int amount)
    {
        if (Current <= 0 || Current >= maxHealth) return false;
        Current = Mathf.Min(maxHealth, Current + amount);
        if (Changed != null) Changed(Current, maxHealth);
        if (controller != null) controller.Burst(new Color(1f, 0.45f, 0.65f), 16);
        GameAudioManager.Instance?.PlayHeartPickup();
        HapticsManager.Pickup();
        return true;
    }

    public void ResetFull()
    {
        Current = maxHealth;
        invulnTimer = 1.0f;
        ShieldTime = 0f;
        if (rend != null) rend.enabled = true;
        if (Changed != null) Changed(Current, maxHealth);
    }

    void FixedUpdate()
    {
        if (damageSuppressionTimer > 0f) damageSuppressionTimer -= Time.fixedDeltaTime;
        if (invulnTimer > 0f) invulnTimer -= Time.fixedDeltaTime;
        if (ShieldTime > 0f) ShieldTime = Mathf.Max(0f, ShieldTime - Time.fixedDeltaTime);
    }

    void Update()
    {
        if (rend != null)
        {
            bool blink = invulnTimer > 0f && Current > 0;
            rend.enabled = !blink || ((int)(Time.unscaledTime * 14f) % 2 == 0);
        }
        if (shieldFx != null)
        {
            bool on = ShieldTime > 0f && (ShieldTime > 2f || ((int)(Time.unscaledTime * 10f) % 2 == 0));
            if (shieldFx.gameObject.activeSelf != on) shieldFx.gameObject.SetActive(on);
            if (on) shieldFx.localScale = Vector3.one * (1.9f + 0.1f * Mathf.Sin(Time.unscaledTime * 8f));
        }
    }
}
