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
    [SerializeField] private float contactOffset = 0.03f;
    [SerializeField] private int depenetrationIterations = 4;

    private Rigidbody2D rb;
    private Collider2D potCollider;
    private Transform playerTransform;
    private Collider2D[] playerColliders;
    private Transform originalParent;
    private bool isHeld;
    private bool hasValidHeldPosition;
    private Vector2 lastValidHeldPosition;
    private RigidbodyType2D originalBodyType;
    private float originalGravityScale;
    private RigidbodyConstraints2D originalConstraints;
    private readonly RaycastHit2D[] castHits = new RaycastHit2D[16];
    private readonly Collider2D[] overlapHits = new Collider2D[16];
    private ContactFilter2D obstructionFilter;
    private Vector2 previousVelocity;
    private float nextImpactCueTime;

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

    private void FixedUpdate()
    {
        if (rb != null)
        {
            previousVelocity = rb.velocity;
        }
    }

    private void PickUp()
    {
        isHeld = true;
        hasValidHeldPosition = false;

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

        Vector2 start = rb != null ? rb.position : (Vector2)transform.position;
        SetPotPosition(Depenetrate(start));
        RememberValidHeldPosition();
        InteractionFeedback.Play(InteractionCue.Pickup, transform.position);
    }

    private void Drop()
    {
        isHeld = false;
        hasValidHeldPosition = false;
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
        previousVelocity = Vector2.zero;
        InteractionFeedback.Play(InteractionCue.Drop, transform.position);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isHeld || !canBePickedUp || collision.contactCount == 0)
        {
            return;
        }

        if (previousVelocity.y > -1.5f || previousVelocity.magnitude < 2.5f)
        {
            return;
        }

        ContactPoint2D contact = collision.GetContact(0);
        if (contact.normal.y < 0.35f || Time.time < nextImpactCueTime)
        {
            return;
        }

        nextImpactCueTime = Time.time + 0.08f;
        InteractionFeedback.Play(InteractionCue.PotImpact, contact.point,
            intensity: Mathf.Lerp(0.6f, 1.35f, Mathf.InverseLerp(2.5f, 10f, previousVelocity.magnitude)));
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
        Vector2 next = Depenetrate(current);

        SetPotPosition(next);
        Physics2D.SyncTransforms();
        if (IsOverlappingSolid())
        {
            return hasValidHeldPosition ? lastValidHeldPosition : next;
        }

        next += CastMovement(new Vector2(desired.x - next.x, 0f));
        next = Depenetrate(next);

        SetPotPosition(next);
        Physics2D.SyncTransforms();
        next += CastMovement(new Vector2(0f, desired.y - next.y));
        next = Depenetrate(next);

        SetPotPosition(next);
        Physics2D.SyncTransforms();
        if (IsOverlappingSolid())
        {
            return hasValidHeldPosition ? lastValidHeldPosition : next;
        }

        lastValidHeldPosition = next;
        hasValidHeldPosition = true;
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
        int hitCount = potCollider.Cast(direction, obstructionFilter, castHits, distance + contactOffset);
        float allowed = distance;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = castHits[i];
            if (!IsSolidObstruction(hit.collider) || hit.distance < 0f)
            {
                continue;
            }

            float travel = Mathf.Max(0f, hit.distance - contactOffset);
            if (travel < allowed)
            {
                allowed = travel;
            }
        }

        return direction * allowed;
    }

    private Vector2 Depenetrate(Vector2 origin)
    {
        Vector2 next = origin;
        int iterations = Mathf.Max(1, depenetrationIterations);

        for (int i = 0; i < iterations; i++)
        {
            SetPotPosition(next);
            Physics2D.SyncTransforms();

            Vector2 push = GetDeepestSeparation();
            if (push.sqrMagnitude < 0.000001f)
            {
                break;
            }

            next += push;
        }

        SetPotPosition(next);
        Physics2D.SyncTransforms();
        return next;
    }

    private Vector2 GetDeepestSeparation()
    {
        if (potCollider == null)
        {
            return Vector2.zero;
        }

        int overlapCount = potCollider.OverlapCollider(obstructionFilter, overlapHits);
        float deepest = 0f;
        Vector2 separation = Vector2.zero;

        for (int i = 0; i < overlapCount; i++)
        {
            Collider2D hit = overlapHits[i];
            if (!IsSolidObstruction(hit))
            {
                continue;
            }

            ColliderDistance2D distance = potCollider.Distance(hit);
            if (!distance.isValid || !distance.isOverlapped)
            {
                continue;
            }

            float penetration = distance.distance - contactOffset;
            if (penetration >= deepest)
            {
                continue;
            }

            deepest = penetration;
            separation = distance.normal * penetration;
        }

        return separation;
    }

    private bool IsOverlappingSolid()
    {
        if (potCollider == null)
        {
            return false;
        }

        int overlapCount = potCollider.OverlapCollider(obstructionFilter, overlapHits);
        for (int i = 0; i < overlapCount; i++)
        {
            Collider2D hit = overlapHits[i];
            if (!IsSolidObstruction(hit))
            {
                continue;
            }

            ColliderDistance2D distance = potCollider.Distance(hit);
            if (distance.isValid && distance.isOverlapped)
            {
                return true;
            }
        }

        return false;
    }

    private void RememberValidHeldPosition()
    {
        Vector2 current = rb != null ? rb.position : (Vector2)transform.position;
        SetPotPosition(current);
        Physics2D.SyncTransforms();
        if (!IsOverlappingSolid())
        {
            lastValidHeldPosition = current;
            hasValidHeldPosition = true;
        }
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
