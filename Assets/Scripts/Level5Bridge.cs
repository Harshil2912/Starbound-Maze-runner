using UnityEngine;

public class Level5Bridge : Activatable
{
    public Transform[] bridgeSegments;
    public float downY = -15f;
    public float upY = 0f;
    public float deploySpeed = 8f;
    public Light bridgeLight;
    public ParticleSystem deployFx;

    bool isDeployed;
    float currentY;

    void Awake()
    {
        if (deployFx != null)
        {
            var main = deployFx.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        }
    }

    void OnEnable()
    {
        isDeployed = false;
        currentY = downY;
        SetHeight(downY);
        SetColliders(false);
    }

    public override void Activate(bool value)
    {
        if (isDeployed == value) return;
        isDeployed = value;

        if (isDeployed)
        {
            SetColliders(true);
            if (deployFx != null) deployFx.Play();
            GameAudioManager.Instance?.PlayGateOpen();
            HapticsManager.TriggerClick();
            var lm = FindFirstObjectByType<LevelManager>();
            if (lm != null && lm.messageText != null)
            {
                lm.messageText.text = "BRIDGE ACTIVATED!\nCross the Chasm to the Citadel!";
                lm.messageText.gameObject.SetActive(true);
                CancelInvoke(nameof(HideMessage));
                Invoke(nameof(HideMessage), 3f);
            }
        }
    }

    void Update()
    {
        float targetY = isDeployed ? upY : downY;
        if (!Mathf.Approximately(currentY, targetY))
        {
            currentY = Mathf.MoveTowards(currentY, targetY, deploySpeed * Time.deltaTime);
            SetHeight(currentY);

            if (!isDeployed && Mathf.Approximately(currentY, downY))
            {
                SetColliders(false);
            }
        }

        if (bridgeLight != null)
        {
            bridgeLight.intensity = Mathf.Lerp(bridgeLight.intensity, isDeployed ? 4f : 0f, Time.deltaTime * 3f);
        }
    }

    void SetHeight(float y)
    {
        if (bridgeSegments == null) return;
        for (int i = 0; i < bridgeSegments.Length; i++)
        {
            if (bridgeSegments[i] != null)
            {
                Vector3 p = bridgeSegments[i].localPosition;
                p.y = y;
                bridgeSegments[i].localPosition = p;
            }
        }
    }

    void SetColliders(bool enabled)
    {
        if (bridgeSegments == null) return;
        for (int i = 0; i < bridgeSegments.Length; i++)
        {
            if (bridgeSegments[i] != null)
            {
                var cols = bridgeSegments[i].GetComponentsInChildren<Collider>();
                for (int c = 0; c < cols.Length; c++) cols[c].enabled = enabled;
            }
        }
    }

    void HideMessage()
    {
        var lm = FindFirstObjectByType<LevelManager>();
        if (lm != null && lm.messageText != null)
        {
            lm.messageText.gameObject.SetActive(false);
        }
    }
}
