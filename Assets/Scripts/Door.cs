using UnityEngine;

public class Door : Activatable
{
    public Transform slab;
    public float closedY = 1.4f;
    public float openY = -1.6f;
    public float speed = 5f;

    Collider col;
    bool open;

    void Awake() { if (slab != null) col = slab.GetComponent<Collider>(); }

    void OnEnable()
    {
        open = false;
        if (slab != null)
        {
            var p = slab.localPosition; p.y = closedY; slab.localPosition = p;
        }
        if (col != null) col.enabled = true;
    }

    public bool IsOpen { get { return slab != null && Mathf.Approximately(slab.localPosition.y, openY); } }

    public override void Activate(bool on)
    {
        if (open != on && on) GameAudioManager.Instance?.PlayGateOpen();
        open = on;
        if (!on && col != null) col.enabled = true;
    }

    void FixedUpdate()
    {
        if (slab == null) return;
        var p = slab.localPosition;
        p.y = Mathf.MoveTowards(p.y, open ? openY : closedY, speed * Time.fixedDeltaTime);
        slab.localPosition = p;
        if (open && Mathf.Approximately(p.y, openY) && col != null) col.enabled = false;
    }
}
