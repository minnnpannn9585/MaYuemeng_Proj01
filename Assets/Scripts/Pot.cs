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

    private Rigidbody2D rb;
    private Collider2D potCollider;
    private Transform playerTransform;
    private Transform originalParent;
    private bool isHeld;

    public bool IsHeld => isHeld;
    public bool CanBePickedUp => canBePickedUp;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        potCollider = GetComponent<Collider2D>();
        originalParent = transform.parent;
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            playerTransform = player.transform;
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
        if (!isHeld || holdPoint == null)
        {
            return;
        }

        transform.position = holdPoint.position;
    }

    private void PickUp()
    {
        isHeld = true;
        transform.SetParent(playerTransform);

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        if (potCollider != null)
        {
            potCollider.enabled = false;
        }
    }

    private void Drop()
    {
        isHeld = false;
        transform.SetParent(originalParent, true);

        if (holdPoint != null)
        {
            transform.position = holdPoint.position;
        }

        if (rb != null)
        {
            rb.simulated = true;
        }

        if (potCollider != null)
        {
            potCollider.enabled = true;
        }
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
}
