using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Player transform
    public Vector3 offset = new Vector3(0f, 0f, -10f); // Keep camera at a distance

    void LateUpdate()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }
}
