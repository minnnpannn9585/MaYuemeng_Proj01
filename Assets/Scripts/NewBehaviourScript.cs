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

    private float horizontalInput;
    private float verticalInput;
    private bool jumpPressed;
    private bool isGrounded;
    private bool isInLadderArea;
    private bool isClimbing;
    private float originalGravityScale;

    void Start()
    {
        originalGravityScale = rb.gravityScale;
    }

    void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

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
            rb.velocity = new Vector2(rb.velocity.x, jumpspeed);
            jumpPressed = false;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ladder"))
        {
            isInLadderArea = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Ladder"))
        {
            isInLadderArea = false;
            isClimbing = false;
        }
    }
}
