using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Rigidbody2D targetRb;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    private void LateUpdate()
    {
        if (targetRb == null)
        {
            return;
        }

        Vector2 targetPosition2D = targetRb.position;
        transform.position = new Vector3(targetPosition2D.x, targetPosition2D.y, 0f) + offset;
    }
}
