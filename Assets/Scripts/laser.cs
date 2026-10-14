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
    private LineRenderer glowRenderer;
    private Material beamMaterial;
    private ContactFilter2D triggerFilter;
    private int laserImpactId;
    private float nextLaserSparkTime;
    private float laserImpactLostTime;

    private void Awake()
    {
        EnsureLineRenderer();

        triggerFilter.useLayerMask = false;
        triggerFilter.useTriggers = true;
    }

    private void LateUpdate()
    {
        if (lineRenderer == null) EnsureLineRenderer();
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
        lineRenderer.startWidth = lineWidth * 0.4f;
        lineRenderer.endWidth = lineWidth * 0.4f;
        Color core = Color.Lerp(lineColor, new Color(1f, 0.95f, 0.8f), 0.8f);
        lineRenderer.startColor = core;
        lineRenderer.endColor = core;
        lineRenderer.sortingOrder = sortingOrder;
        lineRenderer.numCapVertices = 4;

        if (beamMaterial == null)
        {
            Shader spriteShader = Resources.Load<Shader>("Feedback/FeedbackParticle");
            if (spriteShader != null)
            {
                beamMaterial = new Material(spriteShader);
                beamMaterial.SetFloat("_Shape", 5f);
            }
        }
        if (beamMaterial != null) lineRenderer.sharedMaterial = beamMaterial;
        if (glowRenderer == null)
        {
            GameObject glow = new GameObject("Laser soft edge");
            glow.transform.SetParent(transform, false);
            glowRenderer = glow.AddComponent<LineRenderer>();
        }
        glowRenderer.sharedMaterial = beamMaterial;
        glowRenderer.positionCount = 2;
        glowRenderer.useWorldSpace = true;
        glowRenderer.startWidth = glowRenderer.endWidth = lineWidth * 2.4f;
        Color halo = Color.Lerp(lineColor, new Color(1f, 0.55f, 0.35f), 0.4f);
        halo.a = 0.18f;
        glowRenderer.startColor = glowRenderer.endColor = halo;
        glowRenderer.sortingOrder = sortingOrder - 1;
        glowRenderer.numCapVertices = 4;
    }

    private void OnEnable()
    {
        if (lineRenderer != null) lineRenderer.enabled = true;
        if (glowRenderer != null) glowRenderer.enabled = true;
    }

    private void OnDisable()
    {
        if (lineRenderer != null) lineRenderer.enabled = false;
        if (glowRenderer != null) glowRenderer.enabled = false;
        laserImpactId = 0;
    }

    private void OnDestroy()
    {
        if (beamMaterial != null) Destroy(beamMaterial);
        if (glowRenderer != null) Destroy(glowRenderer.gameObject);
    }

    private void UpdateLaser()
    {
        Vector3 origin = transform.position;
        Vector3 direction = -transform.right;
        Vector2 direction2D = ((Vector2)direction).normalized;
        float visibleLength = GetVisibleLength((Vector2)origin, direction2D, out Collider2D blockedCollider);
        Vector3 end = origin + (Vector3)(direction2D * visibleLength);

        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, end);
        glowRenderer.SetPosition(0, origin);
        glowRenderer.SetPosition(1, end);
        UpdateLaserImpact(end, blockedCollider);
    }

    private float GetVisibleLength(Vector2 origin, Vector2 direction, out Collider2D blockedCollider)
    {
        blockedCollider = null;
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
                blockedCollider = hitCollider;
            }
        }

        return visibleLength;
    }

    private void UpdateLaserImpact(Vector3 point, Collider2D blockedCollider)
    {
        if (blockedCollider == null)
        {
            if (laserImpactId != 0 && Time.time >= laserImpactLostTime)
            {
                laserImpactId = 0;
            }

            return;
        }

        laserImpactLostTime = Time.time + 0.18f;
        int id = blockedCollider.GetInstanceID();
        bool newTarget = id != laserImpactId;
        laserImpactId = id;

        if (newTarget)
        {
            nextLaserSparkTime = Time.time + 0.12f;
            InteractionFeedback.Play(InteractionCue.LaserHit, point);
            return;
        }

        if (Time.time < nextLaserSparkTime)
        {
            return;
        }

        nextLaserSparkTime = Time.time + 0.12f;
        InteractionFeedback.Play(InteractionCue.LaserSpark, point, false, true);
    }
}
