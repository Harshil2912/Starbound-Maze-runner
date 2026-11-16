using UnityEngine;

// Opens the exit gate once every energy core has been collected.
public class CoreGate : MonoBehaviour
{
    public Door gate;
    public int total = 4;

    public int Collected { get; private set; }
    GameUI ui;

    public bool IsOpen { get { return gate != null && gate.IsOpen; } }

    public void ResetGate()
    {
        Collected = 0;
        if (gate != null) gate.Activate(false);
        Refresh(true);
    }

    void OnEnable()
    {
        if (gate != null && Collected >= total)
        {
            gate.Activate(true);
        }
        Refresh(true);
    }

    void OnDisable() { Refresh(false); }

    public void Collect()
    {
        Collected++;
        Refresh(true);
        if (Collected >= total && gate != null)
        {
            gate.Activate(true);
            GameAudioManager.Instance?.PlayGateOpen();
            HapticsManager.TriggerClick();
        }
    }

    void Refresh(bool visible)
    {
        if (ui == null) ui = FindFirstObjectByType<GameUI>(FindObjectsInactive.Include);
        if (ui == null) return;
        ui.SetCores(Collected, total);
        ui.SetCoresVisible(visible);
    }
}
