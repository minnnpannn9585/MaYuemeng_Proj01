using UnityEngine;

[RequireComponent(typeof(Transform))]
public class laser : MonoBehaviour
{
    [SerializeField] private float maxLength = 12f;
    [SerializeField] private float lineWidth = 0.12f;
    [SerializeField] private Color lineColor = Color.red;
    [SerializeField] private int sortingOrder = 20;

    private readonly RaycastHit2D[] raycastHits = new RaycastHit2D[16];
    private LineRenderer lineRenderer;
    private ContactFilter2D triggerFilter;

    private void Awake()
    {
        EnsureLineRenderer();

        triggerFilter.useLayerMask = false;
        triggerFilter.useTriggers = true;
    }

    private void LateUpdate()
    {
        EnsureLineRenderer();
        UpdateLaser();
    }

    private void EnsureLineRenderer()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.sortingOrder = sortingOrder;

        if (lineRenderer.sharedMaterial == null)
        {
            Shader spriteShader = Shader.Find("Sprites/Default");
            if (spriteShader != null)
            {
                lineRenderer.sharedMaterial = new Material(spriteShader);
            }
        }
    }

    private void UpdateLaser()
    {
        Vector3 origin = transform.position;
        Vector3 direction = -transform.right;
        float visibleLength = GetVisibleLength((Vector2)origin, (Vector2)direction.normalized);

        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, origin + (direction.normalized * visibleLength));
    }

    private float GetVisibleLength(Vector2 origin, Vector2 direction)
    {
        int hitCount = Physics2D.Raycast(origin, direction, triggerFilter, raycastHits, maxLength);
        float visibleLength = maxLength;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = raycastHits[i].collider;
            if (hitCollider == null || hitCollider.GetComponent<CactusLaserTarget>() == null)
            {
                continue;
            }

            if (raycastHits[i].distance < visibleLength)
            {
                visibleLength = raycastHits[i].distance;
            }
        }

        return visibleLength;
    }
}
