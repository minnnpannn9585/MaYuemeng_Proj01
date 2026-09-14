using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    public Rigidbody2D rb;
    public float walkspeed;
    public float jumpspeed;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    public float climbSpeed = 3f;
    public bool windDebugLogging = true;
    public float windDetectRadius = 0.6f;

    private float horizontalInput;
    private float verticalInput;
    private bool jumpPressed;
    private bool isGrounded;
    private bool isInLadderArea;
    private bool isClimbing;
    private float originalGravityScale;
    private float windTimer;
    private Vector2 windVelocity;
    private WindArea currentWindArea;
    private Collider2D bodyCollider;

    void Start()
    {
        originalGravityScale = rb.gravityScale;
        bodyCollider = GetComponent<Collider2D>();
        if (bodyCollider == null && rb != null)
        {
            bodyCollider = rb.GetComponent<Collider2D>();
        }
    }

    public void ApplyWind(Vector2 velocity, float duration)
    {
        windVelocity = velocity;
        windTimer = Mathf.Max(windTimer, duration);
        isClimbing = false;
        rb.gravityScale = originalGravityScale;
        rb.velocity = velocity;

        if (windDebugLogging)
        {
            Debug.Log($"[Wind] ApplyWind velocity={velocity} duration={duration}", this);
        }
    }

    public bool IsBeingBlown => windTimer > 0f;

    void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (horizontalInput > 0f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
        else if (horizontalInput < 0f)
        {
            Vector3 scale = transform.localScale;
            scale.x = -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        if (isInLadderArea && Mathf.Abs(verticalInput) > 0f)
        {
            isClimbing = true;
        }
        else if (!isInLadderArea)
        {
            isClimbing = false;
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isClimbing)
        {
            jumpPressed = true;
        }
    }

    void FixedUpdate()
    {
        if (windTimer > 0f)
        {
            windTimer -= Time.fixedDeltaTime;
            rb.gravityScale = originalGravityScale;
            rb.velocity = new Vector2(windVelocity.x, rb.velocity.y);

            if (jumpPressed)
            {
                jumpPressed = false;
            }

            return;
        }

        if (isClimbing)
        {
            rb.gravityScale = 0f;
            rb.velocity = new Vector2(horizontalInput * walkspeed, verticalInput * climbSpeed);
        }
        else
        {
            rb.gravityScale = originalGravityScale;
            rb.velocity = new Vector2(horizontalInput * walkspeed, rb.velocity.y);
        }

        if (jumpPressed)
        {
            jumpPressed = false;

            WindArea windArea = currentWindArea != null ? currentWindArea : FindWindAreaAtPlayer();

            if (windDebugLogging)
            {
                Debug.Log($"[Wind] Jump detected. currentWindArea={(currentWindArea != null)}, overlapWindArea={(windArea != null)}", this);
            }

            if (windArea != null)
            {
                float upwardVelocity = jumpspeed + windArea.WindUpBoost;
                ApplyWind(new Vector2(windArea.WindForce, upwardVelocity), windArea.WindDuration);
            }
            else
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpspeed);
            }
        }
    }

    private WindArea FindWindAreaAtPlayer()
    {
        Vector2 center = bodyCollider != null ? (Vector2)bodyCollider.bounds.center : (Vector2)transform.position;
        float radius = Mathf.Max(0.1f, windDetectRadius);

        Collider2D[] overlaps = Physics2D.OverlapCircleAll(center, radius);
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider2D overlap = overlaps[i];
            if (overlap == null)
            {
                continue;
            }

            WindArea windArea = overlap.GetComponent<WindArea>();
            if (windArea == null)
            {
                windArea = overlap.GetComponentInParent<WindArea>();
            }

            if (windArea != null && windArea.isActiveAndEnabled)
            {
                if (windDebugLogging)
                {
                    Debug.Log($"[Wind] Overlap found WindArea on '{overlap.name}'", this);
                }

                return windArea;
            }
        }

        return null;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ladder"))
        {
            isInLadderArea = true;
        }

        WindArea windArea = other.GetComponent<WindArea>();
        if (windArea == null)
        {
            windArea = other.GetComponentInParent<WindArea>();
        }
        if (windArea != null)
        {
            currentWindArea = windArea;
            if (windDebugLogging)
            {
                Debug.Log($"[Wind] Entered wind area '{other.name}'", this);
            }
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (currentWindArea == null)
        {
            WindArea windArea = other.GetComponent<WindArea>();
            if (windArea == null)
            {
                windArea = other.GetComponentInParent<WindArea>();
            }
            if (windArea != null)
            {
                currentWindArea = windArea;
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Ladder"))
        {
            isInLadderArea = false;
            isClimbing = false;
        }

        WindArea windArea = other.GetComponent<WindArea>();
        if (windArea == null)
        {
            windArea = other.GetComponentInParent<WindArea>();
        }
        if (windArea != null && windArea == currentWindArea)
        {
            currentWindArea = null;
        }
    }
}
