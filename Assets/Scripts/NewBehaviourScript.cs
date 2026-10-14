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
    private bool wasGrounded;
    private bool hasSampledGround;
    private float lowestAirborneSpeed;
    private float suppressLandingUntil;
    private bool insideWind;
    private float windJumpBoost;
    private float windJumpSpeed;

    public void ResetLandingFeedback(float seconds = 0.15f)
    {
        lowestAirborneSpeed = 0f;
        hasSampledGround = false;
        jumpPressed = false;
        suppressLandingUntil = Time.time + seconds;
    }

    public void SetInsideWind(float jumpSpeed)
    {
        insideWind = true;
        if (jumpSpeed > windJumpSpeed)
        {
            windJumpSpeed = jumpSpeed;
        }
    }

    void Start()
    {
        originalGravityScale = rb.gravityScale;
    }

    void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (!hasSampledGround)
        {
            wasGrounded = isGrounded;
            hasSampledGround = true;
        }

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

        bool wasClimbing = isClimbing;
        if (isInLadderArea && Mathf.Abs(verticalInput) > 0f)
        {
            isClimbing = true;
        }
        else if (!isInLadderArea)
        {
            isClimbing = false;
        }

        if (isClimbing && !wasClimbing)
        {
            InteractionFeedback.Play(InteractionCue.Climb, transform.position);
        }

        if (!isGrounded)
        {
            lowestAirborneSpeed = Mathf.Min(lowestAirborneSpeed, rb.velocity.y);
        }
        else
        {
            if (!wasGrounded && lowestAirborneSpeed < -3f && !isClimbing && Time.time >= suppressLandingUntil)
            {
                InteractionFeedback.Play(InteractionCue.Land, groundCheck.position,
                    intensity: Mathf.Lerp(0.55f, 1.35f, Mathf.InverseLerp(3f, 13f, -lowestAirborneSpeed)));
            }

            lowestAirborneSpeed = 0f;
        }

        wasGrounded = isGrounded;

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isClimbing)
        {
            jumpPressed = true;
        }
    }

    void FixedUpdate()
    {
        bool groundedNow = groundCheck != null
            && Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        if (groundedNow && !jumpPressed)
        {
            windJumpBoost = 0f;
        }

        if (isClimbing)
        {
            rb.gravityScale = 0f;
            rb.velocity = new Vector2(horizontalInput * walkspeed, verticalInput * climbSpeed);
            windJumpBoost = 0f;
        }
        else
        {
            rb.gravityScale = originalGravityScale;
            rb.velocity = new Vector2(horizontalInput * walkspeed + windJumpBoost, rb.velocity.y);
        }

        if (jumpPressed)
        {
            float xSpeed = rb.velocity.x;
            if (insideWind)
            {
                windJumpBoost = windJumpSpeed;
                xSpeed = horizontalInput * walkspeed + windJumpBoost;
                Vector3 scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x);
                transform.localScale = scale;
            }

            rb.velocity = new Vector2(xSpeed, jumpspeed);
            InteractionFeedback.Play(InteractionCue.Jump, groundCheck.position);
            jumpPressed = false;
        }

        insideWind = false;
        windJumpSpeed = 0f;
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
