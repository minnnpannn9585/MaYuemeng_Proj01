using UnityEngine;

public class PotBouncePad : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float bounceVelocity = 8f;
    [SerializeField] private float topEntryTolerance = 0.05f;
    [SerializeField] private float bounceCooldown = 0.1f;

    private Collider2D bounceCollider;
    private Rigidbody2D lastBouncedBody;
    private float lastBounceTime = float.NegativeInfinity;

    public float BounceVelocity
    {
        get => bounceVelocity;
        set => bounceVelocity = value;
    }

    private void Awake()
    {
        bounceCollider = GetComponent<Collider2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryBounce(collision.collider, collision.rigidbody);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryBounce(other, other.attachedRigidbody);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryBounce(collision.collider, collision.rigidbody);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryBounce(other, other.attachedRigidbody);
    }

    private void TryBounce(Collider2D other, Rigidbody2D otherRb)
    {
        if (other == null || otherRb == null || !other.CompareTag(playerTag))
        {
            return;
        }

        if (otherRb == lastBouncedBody && Time.time < lastBounceTime + bounceCooldown)
        {
            return;
        }

        if (!IsEnteringFromAbove(other, otherRb.velocity))
        {
            return;
        }

        otherRb.velocity = new Vector2(otherRb.velocity.x, bounceVelocity);
        lastBouncedBody = otherRb;
        lastBounceTime = Time.time;
    }

    private bool IsEnteringFromAbove(Collider2D other, Vector2 otherVelocity)
    {
        if (otherVelocity.y > 0.1f || bounceCollider == null)
        {
            return false;
        }

        float playerBottom = other.bounds.min.y;
        float surfaceTopY = bounceCollider.bounds.max.y;
        return playerBottom >= surfaceTopY - (topEntryTolerance * 2f);
    }
}
