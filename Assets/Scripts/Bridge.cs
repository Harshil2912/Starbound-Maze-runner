using UnityEngine;

public class Bridge : Activatable
{
    public Transform[] tiles;
    public int[] tileCols;
    public int[] tileRows;
    public DynamicFloor floor;
    public float upY = -0.25f;
    public float downY = -3f;
    public float speed = 6f;

    bool on;
    bool solidNow;

    void OnEnable()
    {
        on = false;
        solidNow = false;
        SetY(downY);
    }

    void SetY(float y)
    {
        if (tiles == null) return;
        foreach (var t in tiles)
        {
            var p = t.localPosition; p.y = y; t.localPosition = p;
        }
    }

    public bool IsUp { get { return solidNow; } }

    public override void Activate(bool value)
    {
        on = value;
        if (!value) SetSolid(false);
    }

    void SetSolid(bool s)
    {
        if (solidNow == s) return;
        solidNow = s;
        if (floor == null) return;
        for (int i = 0; i < tileCols.Length; i++) floor.SetSolid(tileCols[i], tileRows[i], s);
    }

    void FixedUpdate()
    {
        if (tiles == null || tiles.Length == 0) return;
        float y = tiles[0].localPosition.y;
        y = Mathf.MoveTowards(y, on ? upY : downY, speed * Time.fixedDeltaTime);
        SetY(y);
        if (on && Mathf.Approximately(y, upY)) SetSolid(true);
    }
}
