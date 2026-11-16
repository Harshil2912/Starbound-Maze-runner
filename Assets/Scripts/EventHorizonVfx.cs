using UnityEngine;

public class EventHorizonVfx : MonoBehaviour
{
    public Transform ring1;
    public Transform ring2;
    public Transform ring3;

    public float rotSpeed1 = 15f;
    public float rotSpeed2 = -25f;
    public float rotSpeed3 = 10f;

    void Update()
    {
        if (ring1 != null) ring1.Rotate(Vector3.up, rotSpeed1 * Time.deltaTime, Space.Self);
        if (ring2 != null) ring2.Rotate(Vector3.up, rotSpeed2 * Time.deltaTime, Space.Self);
        if (ring3 != null) ring3.Rotate(Vector3.up, rotSpeed3 * Time.deltaTime, Space.Self);
    }
}
