using UnityEngine;

public class ZoneNotifier : MonoBehaviour
{
    static readonly string[] ZoneNames = new string[]
    {
        "ZONE 1: DOCKING BAY",
        "ZONE 2: REACTOR WALKWAY",
        "ZONE 3: GRAVITY WELL",
        "ZONE 4: PLASMA CONDUIT",
        "ZONE 5: CONTROL NEXUS",
        "ZONE 6: CARGO HOLD",
        "ZONE 7: LASER LAB",
        "ZONE 8: ANTEROOM",
        "ZONE 9: SINGULARITY CORE"
    };

    CubeController player;
    LevelManager levelManager;
    int lastZone = -1;

    void OnEnable()
    {
        lastZone = -1;
    }

    void FixedUpdate()
    {
        if (levelManager == null) levelManager = FindFirstObjectByType<LevelManager>();
        if (levelManager == null || levelManager.CurrentLevel != 5) return;

        if (player == null)
        {
            player = FindFirstObjectByType<CubeController>();
            if (player == null) return;
        }

        Vector3 local = transform.InverseTransformPoint(player.transform.position);
        int c = Mathf.FloorToInt((local.x + 57.6f) / 2.4f);
        int r = Mathf.FloorToInt((57.6f - local.z) / 2.4f);

        int zCol = Mathf.Clamp(c / 16, 0, 2);
        int zRow = Mathf.Clamp(r / 16, 0, 2);

        int zoneIndex = -1;
        if (zRow == 2)
        {
            if (zCol == 0) zoneIndex = 0; // Z1
            else if (zCol == 1) zoneIndex = 1; // Z2
            else if (zCol == 2) zoneIndex = 2; // Z3
        }
        else if (zRow == 1)
        {
            if (zCol == 2) zoneIndex = 3; // Z4
            else if (zCol == 1) zoneIndex = 4; // Z5
            else if (zCol == 0) zoneIndex = 5; // Z6
        }
        else if (zRow == 0)
        {
            if (zCol == 0) zoneIndex = 6; // Z7
            else if (zCol == 1) zoneIndex = 7; // Z8
            else if (zCol == 2) zoneIndex = 8; // Z9
        }

        if (zoneIndex >= 0 && zoneIndex != lastZone)
        {
            lastZone = zoneIndex;
            ShowBanner(ZoneNames[zoneIndex]);
        }
    }

    void ShowBanner(string text)
    {
        if (levelManager != null && levelManager.messageText != null)
        {
            levelManager.messageText.text = text;
            levelManager.messageText.gameObject.SetActive(true);
            CancelInvoke(nameof(HideBanner));
            Invoke(nameof(HideBanner), 2.5f);
        }
    }

    void HideBanner()
    {
        if (levelManager != null && levelManager.messageText != null)
        {
            levelManager.messageText.gameObject.SetActive(false);
        }
    }
}
