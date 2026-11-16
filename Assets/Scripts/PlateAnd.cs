using UnityEngine;

public class PlateAnd : Activatable
{
    public PressurePlate plateA;
    public PressurePlate plateB;
    public Activatable[] targets;

    bool currentActive;

    public override void Activate(bool value)
    {
        // Can be manually triggered or called by events
        Evaluate();
    }

    void OnEnable()
    {
        currentActive = false;
        Evaluate();
    }

    void FixedUpdate()
    {
        Evaluate();
    }

    void Evaluate()
    {
        bool a = plateA != null && plateA.IsActive;
        bool b = plateB != null && plateB.IsActive;
        bool both = a && b;

        if (both != currentActive)
        {
            currentActive = both;
            if (targets != null)
            {
                foreach (var t in targets)
                {
                    if (t != null) t.Activate(currentActive);
                }
            }
        }
    }
}
