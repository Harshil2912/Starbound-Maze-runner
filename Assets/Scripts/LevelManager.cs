using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public const int LevelCount = 5;
    const string ProgressKey = "SB_Completed";

    public CubeController player;
    public PlayerHealth health;
    public ThirdPersonCamera cameraRig;
    public GameUI ui;
    public Text messageText;
    public GameObject[] level1Objects;
    public Transform level1Start;
    public GameObject level2Root;
    public Transform level2Start;
    public GameObject level3Root;
    public Transform level3Start;
    public GameObject level4Root;
    public Transform level4Start;
    public GameObject level5Root;
    public Transform level5Start;
    public float messageTime = 2f;

    public int CurrentLevel { get; private set; }
    bool busy;
    Vector3 level1Spawn;

    // ---- progress: levels you have finished (saved between sessions) ----
    public int Completed { get { return PlayerPrefs.GetInt(ProgressKey, 0); } }
    public bool IsCompleted(int level) { return level >= 1 && level <= Completed; }
    // a level can be played if it is the first one, or the previous one has been finished
    public bool IsUnlocked(int level) { return level >= 1 && level <= LevelCount && level <= Completed + 1; }

    public void MarkCompleted(int level)
    {
        if (level > Completed)
        {
            PlayerPrefs.SetInt(ProgressKey, Mathf.Min(level, LevelCount));
            PlayerPrefs.Save();
        }
    }

    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey(ProgressKey);
        PlayerPrefs.Save();
    }

    void Awake()
    {
        if (level1Start != null)
            level1Spawn = level1Start.position + Vector3.up * 0.5f;
        else if (player != null && (Mathf.Abs(player.transform.position.x) > 1f || Mathf.Abs(player.transform.position.z) > 1f))
            level1Spawn = player.transform.position;
        else
            level1Spawn = new Vector3(-16f, 0.5f, -16f);

        CurrentLevel = 1;
        busy = false;
        if (level2Root != null) level2Root.SetActive(false);
        if (level3Root != null) level3Root.SetActive(false);
        if (level4Root != null) level4Root.SetActive(false);
        if (level5Root != null) level5Root.SetActive(false);
        HideMessage();
        if (health != null) health.Died += OnPlayerDied;
    }

    void OnDestroy()
    {
        if (health != null) health.Died -= OnPlayerDied;
    }

    // ---- main menu ----
    public void ShowMenu()
    {
        StopAllCoroutines();
        busy = false;
        HideMessage();
        if (DualModeManager.Instance != null)
        {
            DualModeManager.Instance.PrepareForMenu();
        }
        EnterLevel(1);                 // the world behind the menu is Level 1
        player.SetFrozen(true);
        if (cameraRig != null) cameraRig.SetMenuMode(true, Vector3.zero);
        if (ui != null) ui.ShowMainMenu();
    }

    // called by the level-select cards
    public void StartLevel(int n)
    {
        if (!IsUnlocked(n)) return;
        StopAllCoroutines();
        busy = false;
        HideMessage();
        player.SetFrozen(false);
        Time.timeScale = 1f;
        if (ui != null) ui.EnsureHudVisible();
        EnterLevel(n);
        if (DualModeManager.Instance != null)
        {
            DualModeManager.Instance.ApplyCurrentModeToGameplay();
        }
    }

    public void OnGoalReached(int level)
    {
        if (busy || level != CurrentLevel) return;
        GameAudioManager.Instance?.PlayLevelComplete();
        HapticsManager.GoalSuccess();
        MarkCompleted(level);
        StartCoroutine(Advance(level));
    }

    IEnumerator Advance(int from)
    {
        busy = true;
        player.SetFrozen(true);
        if (from < LevelCount)
        {
            ShowMessage("LEVEL " + from + " COMPLETE!\nGet ready for Level " + (from + 1) + "...");
            yield return new WaitForSeconds(messageTime);
            EnterLevel(from + 1);
            ShowMessage("LEVEL " + CurrentLevel);
            yield return new WaitForSeconds(1.2f);
            HideMessage();
            player.SetFrozen(false);
            busy = false;
        }
        else
        {
            ShowMessage("GAME FINISHED!\nYou beat all " + LevelCount + " levels.\nBack to the main menu...");
            yield return new WaitForSeconds(3.5f);
            ShowMenu();
        }
    }

    // Switches the whole game to the given level (1..5) and puts the cube at its start.
    public void EnterLevel(int n)
    {
        if (level2Root != null) level2Root.SetActive(n == 2);
        if (level3Root != null)
        {
            // toggled off/on so every hazard, plate, crate and pickup starts fresh
            level3Root.SetActive(false);
            if (n == 3) level3Root.SetActive(true);
        }
        if (level4Root != null)
        {
            level4Root.SetActive(false);
            if (n == 4)
            {
                level4Root.SetActive(true);
                var cores = FindObjectsByType<EnergyCore>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < cores.Length; i++) if (cores[i] != null) cores[i].ResetCore();
                var coreGate = FindAnyObjectByType<CoreGate>(FindObjectsInactive.Include);
                if (coreGate != null) coreGate.ResetGate();
            }
        }
        if (level5Root != null)
        {
            level5Root.SetActive(false);
            if (n == 5) level5Root.SetActive(true);
        }
        foreach (var o in level1Objects)
            if (o != null) o.SetActive(n == 1);

        if (DualModeManager.Instance != null)
        {
            DualModeManager.Instance.OnLevelEntered(n);
        }

        float s = (player != null) ? Mathf.Max(0.001f, player.transform.lossyScale.x) : 1f;
        Vector3 spawn;
        if (n == 1)
        {
            if (level1Start != null) spawn = level1Start.position + level1Start.up * (0.5f * s);
            else if (level1Spawn.sqrMagnitude > 1f) spawn = level1Spawn;
            else spawn = new Vector3(-16f, 0.5f * s, -16f);
        }
        else if (n == 2) spawn = (level2Start != null) ? level2Start.position + level2Start.up * (0.5f * s) : new Vector3(-28f, 0.5f * s, 72f);
        else if (n == 3) spawn = (level3Start != null) ? level3Start.position + level3Start.up * (0.5f * s) : new Vector3(-32f, 0.5f * s, 168f);
        else if (n == 4) spawn = (level4Start != null) ? level4Start.position + level4Start.up * (0.5f * s) : new Vector3(-49.5f, 0.5f * s, 350.5f);
        else spawn = (level5Start != null) ? level5Start.position + level5Start.up * (0.5f * s) : new Vector3(-51.6f, 0.5f * s, 548.4f);

        player.fallsCostHealth = (n >= 3);
        player.SetRespawn(spawn);
        player.TeleportTo(spawn);
        player.ResetVisual();
        if (health != null) health.ResetFull();
        CurrentLevel = n;
        busy = false;
        if (cameraRig != null) cameraRig.SetMenuMode(false, Vector3.zero);
        if (ui != null) ui.SetLevel(n);
        if (ARHologramManager.Instance != null && ARHologramManager.Instance.isHologramActive)
        {
            ARHologramManager.Instance.ApplyHologramToScene();
        }
    }

    public void EnterLevel2() { EnterLevel(2); }

    void OnPlayerDied()
    {
        if (busy) return;
        StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        busy = true;
        player.SetFrozen(true);
        GameAudioManager.Instance?.PlayDeath();
        HapticsManager.Death();
        ShowMessage("YOU DIED");
        yield return new WaitForSeconds(1.5f);
        RespawnAfterDeath();
        HideMessage();
        player.SetFrozen(false);
        busy = false;
    }

    // Back to the last checkpoint with full health.
    public void RespawnAfterDeath()
    {
        if (health != null) health.ResetFull();
        player.TeleportTo(player.RespawnPoint);
        player.ResetVisual();

        // Anti-softlock: reset crates that are not resting on a plate
        var crates = FindObjectsByType<Crate>(FindObjectsSortMode.None);
        for (int i = 0; i < crates.Length; i++)
        {
            if (crates[i] != null && !crates[i].IsOnPlate())
            {
                crates[i].ResetCrate();
            }
        }
    }

    // Pause menu: restart the current level from its beginning.
    public void RetryLevel()
    {
        StopAllCoroutines();
        busy = false;
        HideMessage();
        player.SetFrozen(false);
        if (ui != null) ui.EnsureHudVisible();
        EnterLevel(CurrentLevel);
        if (DualModeManager.Instance != null)
        {
            DualModeManager.Instance.ApplyCurrentModeToGameplay();
        }
    }

    // Pause menu: reset the whole run to Level 1.
    public void RestartGame()
    {
        StopAllCoroutines();
        busy = false;
        HideMessage();
        player.SetFrozen(false);
        EnterLevel(1);
        if (DualModeManager.Instance != null)
        {
            DualModeManager.Instance.ApplyCurrentModeToGameplay();
        }
    }

    void ShowMessage(string s)
    {
        if (messageText == null) return;
        messageText.text = s;
        messageText.gameObject.SetActive(true);
    }

    void HideMessage()
    {
        if (messageText != null) messageText.gameObject.SetActive(false);
    }
}
