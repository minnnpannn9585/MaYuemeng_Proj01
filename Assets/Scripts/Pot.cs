using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pot : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Transform holdPoint;
    [SerializeField] private KeyCode interactKey = KeyCode.F;
    [SerializeField] private float interactDistance = 1.5f;
    [SerializeField] private bool canBePickedUp = true;
    [SerializeField] private LayerMask obstructionMask = ~0;
    [SerializeField] private float contactOffset = 0.02f;

    private Rigidbody2D rb;
    private Collider2D potCollider;
    private Transform playerTransform;
    private Collider2D[] playerColliders;
    private Transform originalParent;
    private bool isHeld;
    private RigidbodyType2D originalBodyType;
    private float originalGravityScale;
    private RigidbodyConstraints2D originalConstraints;
    private readonly RaycastHit2D[] castHits = new RaycastHit2D[8];
    private readonly Collider2D[] overlapHits = new Collider2D[8];
    private ContactFilter2D obstructionFilter;

    public bool IsHeld => isHeld;
    public bool CanBePickedUp => canBePickedUp;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        potCollider = GetComponent<Collider2D>();
        originalParent = transform.parent;

        obstructionFilter.useTriggers = false;
        obstructionFilter.useLayerMask = true;
        obstructionFilter.SetLayerMask(obstructionMask);
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            playerTransform = player.transform;
            playerColliders = player.GetComponentsInChildren<Collider2D>();
        }
    }

    private void Update()
    {
        if (!canBePickedUp || playerTransform == null)
        {
            return;
        }

        if (!Input.GetKeyDown(interactKey))
        {
            return;
        }

        if (isHeld)
        {
            Drop();
            return;
        }

        float distance = Vector2.Distance(transform.position, playerTransform.position);
        if (distance <= interactDistance)
        {
            PickUp();
        }
    }

    private void LateUpdate()
    {
        if (!isHeld)
        {
            return;
        }

        Vector2 desired = holdPoint != null
            ? (Vector2)holdPoint.position
            : (Vector2)playerTransform.position;
        Vector2 current = rb != null ? rb.position : (Vector2)transform.position;
        SetPotPosition(ResolveHeldPosition(current, desired));
    }

    private void PickUp()
    {
        isHeld = true;

        if (rb != null)
        {
            originalBodyType = rb.bodyType;
            originalGravityScale = rb.gravityScale;
            originalConstraints = rb.constraints;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.constraints = originalConstraints | RigidbodyConstraints2D.FreezeRotation;
            rb.simulated = true;
        }

        if (potCollider != null)
        {
            potCollider.enabled = true;
        }

        SetPlayerCollisionIgnored(true);
    }

    private void Drop()
    {
        isHeld = false;
        transform.SetParent(originalParent, true);

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = originalBodyType;
            rb.gravityScale = originalGravityScale;
            rb.constraints = originalConstraints;
            rb.simulated = true;
        }

        if (potCollider != null)
        {
            potCollider.enabled = true;
        }

        SetPlayerCollisionIgnored(false);
    }

    public void ConfigureAsPresentMirror()
    {
        canBePickedUp = false;
        isHeld = false;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.simulated = true;
        }

        if (potCollider != null)
        {
            potCollider.enabled = true;
        }
    }

    public void SyncMirrorPosition(Vector3 worldPosition)
    {
        Vector3 targetPosition = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);

        if (rb != null && rb.bodyType == RigidbodyType2D.Kinematic && rb.simulated)
        {
            rb.MovePosition(targetPosition);
            return;
        }

        transform.position = targetPosition;
    }

    private Vector2 ResolveHeldPosition(Vector2 current, Vector2 desired)
    {
        Vector2 next = current;

        SetPotPosition(next);
        Physics2D.SyncTransforms();
        next.x += CastMovement(new Vector2(desired.x - next.x, 0f)).x;

        SetPotPosition(next);
        Physics2D.SyncTransforms();
        next.y += CastMovement(new Vector2(0f, desired.y - next.y)).y;

        SetPotPosition(next);
        Physics2D.SyncTransforms();
        next += GetDepenetration();

        return next;
    }

    private Vector2 CastMovement(Vector2 delta)
    {
        float distance = delta.magnitude;
        if (potCollider == null || distance < 0.0001f)
        {
            return Vector2.zero;
        }

        Vector2 direction = delta / distance;
        int hitCount = potCollider.Cast(direction, obstructionFilter, castHits, distance);
        float allowed = distance;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = castHits[i].collider;
            if (!IsSolidObstruction(hit))
            {
                continue;
            }

            float travel = Mathf.Max(0f, castHits[i].distance - contactOffset);
            if (travel < allowed)
            {
                allowed = travel;
            }
        }

        return direction * allowed;
    }

    private Vector2 GetDepenetration()
    {
        if (potCollider == null)
        {
            return Vector2.zero;
        }

        int overlapCount = potCollider.OverlapCollider(obstructionFilter, overlapHits);
        Vector2 correction = Vector2.zero;

        for (int i = 0; i < overlapCount; i++)
        {
            Collider2D hit = overlapHits[i];
            if (!IsSolidObstruction(hit))
            {
                continue;
            }

            ColliderDistance2D distance = potCollider.Distance(hit);
            if (distance.isOverlapped)
            {
                correction += distance.normal * -distance.distance;
            }
        }

        return correction;
    }

    private bool IsSolidObstruction(Collider2D hit)
    {
        if (hit == null || hit == potCollider || hit.isTrigger)
        {
            return false;
        }

        if (playerColliders == null)
        {
            return true;
        }

        for (int i = 0; i < playerColliders.Length; i++)
        {
            if (hit == playerColliders[i])
            {
                return false;
            }
        }

        return true;
    }

    private void SetPotPosition(Vector2 position)
    {
        Vector3 worldPosition = new Vector3(position.x, position.y, transform.position.z);
        transform.position = worldPosition;

        if (rb != null)
        {
            rb.position = position;
            rb.velocity = Vector2.zero;
        }
    }

    private void SetPlayerCollisionIgnored(bool ignore)
    {
        if (potCollider == null || playerColliders == null)
        {
            return;
        }

        for (int i = 0; i < playerColliders.Length; i++)
        {
            if (playerColliders[i] != null)
            {
                Physics2D.IgnoreCollision(potCollider, playerColliders[i], ignore);
            }
        }
    }
}
