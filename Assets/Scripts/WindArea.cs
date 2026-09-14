using UnityEngine;

[DefaultExecutionOrder(300)]
[RequireComponent(typeof(Collider2D))]
public class WindArea : MonoBehaviour
{
    [SerializeField] private float windForce = 15f;
    [SerializeField] private float windUpBoost = 4f;
    [SerializeField] private float windDuration = 0.6f;
    [SerializeField] private bool debugLogging = true;

    private Collider2D windCollider;

    public float WindForce
    {
        get => windForce;
        set => windForce = value;
    }

    public float WindUpBoost
    {
        get => windUpBoost;
        set => windUpBoost = value;
    }

    public float WindDuration
    {
        get => windDuration;
        set => windDuration = value;
    }

    private void Awake()
    {
        EnsureTrigger();
    }

    private void OnEnable()
    {
        EnsureTrigger();

        if (debugLogging)
        {
            string bounds = windCollider != null ? windCollider.bounds.ToString() : "no-collider";
            Debug.Log($"[Wind] WindArea enabled on '{name}'. activeInHierarchy={gameObject.activeInHierarchy}, isTrigger={(windCollider != null && windCollider.isTrigger)}, bounds={bounds}", this);
        }
    }

    private void EnsureTrigger()
    {
        if (windCollider == null)
        {
            windCollider = GetComponent<Collider2D>();
        }

        if (windCollider != null)
        {
            windCollider.isTrigger = true;
        }
    }

    private void OnDrawGizmos()
    {
        Collider2D gizmoCollider = windCollider != null ? windCollider : GetComponent<Collider2D>();
        if (gizmoCollider == null)
        {
            return;
        }

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
        Bounds b = gizmoCollider.bounds;
        Gizmos.DrawCube(b.center, b.size);
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 1f);
        Gizmos.DrawWireCube(b.center, b.size);
    }
}
