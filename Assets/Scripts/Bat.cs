using UnityEngine;

public class Bat : MonoBehaviour
{
    [SerializeField] private float fleeSpeed = 6f;

    private SpriteRenderer spriteRenderer;
    private Vector2 fleeDirection;
    private bool isFleeing;

    public bool IsFleeing => isFleeing;

    public Bounds WorldBounds
    {
        get
        {
            if (spriteRenderer != null)
            {
                return spriteRenderer.bounds;
            }

            Collider2D batCollider = GetComponent<Collider2D>();
            if (batCollider != null)
            {
                return batCollider.bounds;
            }

            return new Bounds(transform.position, Vector3.one);
        }
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (!isFleeing)
        {
            return;
        }

        transform.position += (Vector3)(fleeDirection * fleeSpeed * Time.deltaTime);
    }

    public void StartFleeing()
    {
        if (isFleeing)
        {
            return;
        }

        isFleeing = true;
        InteractionFeedback.Play(InteractionCue.BatFlee, WorldBounds.center);
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        fleeDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        if (spriteRenderer != null && !Mathf.Approximately(fleeDirection.x, 0f))
        {
            spriteRenderer.flipX = fleeDirection.x < 0f;
        }
    }
}
