using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    public int levelNumber = 1;
    public LevelManager manager;

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<CubeController>() == null) return;
        if (manager == null) manager = FindFirstObjectByType<LevelManager>();
        if (manager != null) manager.OnGoalReached(levelNumber);
    }
}
