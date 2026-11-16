using UnityEngine;

public class DummyWall : MonoBehaviour
{
    public enum DummyType { IllusionPassage, DecoyObstacle, DecoyGoal }
    public DummyType dummyType = DummyType.IllusionPassage;
    public float fadeAlpha = 0.25f;
    public string messageOnInteract = "";

    Renderer rend;
    Collider col;
    Material matInstance;
    Color originalColor;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        col = GetComponent<Collider>();

        if (dummyType == DummyType.IllusionPassage && col != null)
        {
            col.isTrigger = true;
        }

        if (rend != null)
        {
            matInstance = rend.material;
            originalColor = matInstance.color;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<CubeController>();
        if (player == null) player = other.GetComponentInParent<CubeController>();
        if (player == null) return;

        if (dummyType == DummyType.IllusionPassage)
        {
            if (matInstance != null)
            {
                Color c = originalColor;
                c.a = fadeAlpha;
                matInstance.color = c;
            }
        }
        else if (dummyType == DummyType.DecoyGoal)
        {
            var lm = FindFirstObjectByType<LevelManager>();
            if (lm != null && lm.messageText != null)
            {
                lm.messageText.text = "DECOY DETECTED!\nThe True Bridge Must Be Activated!";
                lm.messageText.gameObject.SetActive(true);
                CancelInvoke(nameof(HideMessage));
                Invoke(nameof(HideMessage), 3f);
            }
            player.Knockback((player.transform.position - transform.position).normalized, 10f, 0.3f);
            GameAudioManager.Instance?.PlayWallBump();
            HapticsManager.BumpWall();
        }
    }

    void OnTriggerExit(Collider other)
    {
        var player = other.GetComponent<CubeController>();
        if (player == null) player = other.GetComponentInParent<CubeController>();
        if (player == null) return;

        if (dummyType == DummyType.IllusionPassage && matInstance != null)
        {
            matInstance.color = originalColor;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (dummyType == DummyType.DecoyObstacle)
        {
            var player = collision.gameObject.GetComponent<CubeController>();
            if (player != null)
            {
                player.Knockback((player.transform.position - transform.position).normalized, 8f, 0.25f);
                GameAudioManager.Instance?.PlayWallBump();
                HapticsManager.BumpWall();
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
