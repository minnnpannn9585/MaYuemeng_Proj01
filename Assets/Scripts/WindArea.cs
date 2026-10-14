using UnityEngine;

// Marks the player as inside the wind. The shove itself happens only on jump.
[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(Collider2D))]
public class WindArea : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float pushSpeed = 7f;
    [SerializeField] private float groundReach = 1.25f;

    private Collider2D areaCollider;
    private NewBehaviourScript player;
    private Collider2D playerCollider;

    public float PushSpeed
    {
        get => pushSpeed;
        set => pushSpeed = Mathf.Max(0f, value);
    }

    private void Awake()
    {
        areaCollider = GetComponent<Collider2D>();
        if (areaCollider != null)
        {
            areaCollider.isTrigger = true;
        }
    }

    private void FixedUpdate()
    {
        if (areaCollider == null || !areaCollider.enabled || pushSpeed <= 0f)
        {
            return;
        }

        if (!TryGetPlayer(out NewBehaviourScript currentPlayer, out Collider2D currentCollider))
        {
            return;
        }

        if (!IsPlayerInWind(currentCollider))
        {
            return;
        }

        currentPlayer.SetInsideWind(pushSpeed);
    }

    private bool TryGetPlayer(out NewBehaviourScript currentPlayer, out Collider2D currentCollider)
    {
        if (player == null)
        {
            player = FindFirstObjectByType<NewBehaviourScript>();
            if (player != null && !player.CompareTag(playerTag))
            {
                player = null;
            }

            playerCollider = player != null ? player.GetComponent<Collider2D>() : null;
        }

        currentPlayer = player;
        currentCollider = playerCollider;
        return currentPlayer != null && currentCollider != null;
    }

    private bool IsPlayerInWind(Collider2D currentCollider)
    {
        if (areaCollider.IsTouching(currentCollider))
        {
            return true;
        }

        Bounds windBounds = areaCollider.bounds;
        Vector3 min = windBounds.min;
        min.y -= groundReach;
        windBounds.min = min;
        return windBounds.Intersects(currentCollider.bounds);
    }
}
