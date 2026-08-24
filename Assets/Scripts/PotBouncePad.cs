using UnityEngine;

public class PotBouncePad : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float bounceVelocity = 12f;
    [SerializeField] private float topEntryTolerance = 0.05f;

    private Collider2D bounceCollider;

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

    private void TryBounce(Collider2D other, Rigidbody2D otherRb)
    {
        if (other == null || otherRb == null || !other.CompareTag(playerTag))
        {
            return;
        }

        if (!IsEnteringFromAbove(other, otherRb.velocity))
        {
            return;
        }

        otherRb.velocity = new Vector2(otherRb.velocity.x, bounceVelocity);
    }

    private bool IsEnteringFromAbove(Collider2D other, Vector2 otherVelocity)
    {
        if (otherVelocity.y > 0.1f || bounceCollider == null)
        {
            return false;
        }

        float playerBottom = other.bounds.min.y;
        float surfaceTopY = bounceCollider.bounds.max.y;
        return playerBottom >= surfaceTopY - topEntryTolerance;
    }
}
