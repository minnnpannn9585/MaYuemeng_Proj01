using UnityEngine;
using UnityEngine.SceneManagement;

public class Bat : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float fleeSpeed = 9f;
    [SerializeField] private float hoverAmplitude = 0.18f;
    [SerializeField] private float hoverFrequency = 2.2f;
    [SerializeField] private float destroyAfterFlee = 2.5f;

    private FlashlightController flashlight;
    private Collider2D batCollider;
    private SpriteRenderer spriteRenderer;
    private Vector3 restPosition;
    private bool isFleeing;
    private Vector2 fleeDirection;
    private float hoverOffset;

    private void Awake()
    {
        batCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        restPosition = transform.position;
        hoverOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Start()
    {
        flashlight = FindFirstObjectByType<FlashlightController>();
    }

    private void Update()
    {
        if (isFleeing)
        {
            transform.position += (Vector3)(fleeDirection * fleeSpeed * Time.deltaTime);
            FaceDirection(fleeDirection.x);
            return;
        }

        float hover = Mathf.Sin((Time.time * hoverFrequency) + hoverOffset) * hoverAmplitude;
        transform.position = restPosition + Vector3.up * hover;

        if (flashlight != null && flashlight.Contains(transform.position))
        {
            StartFleeing();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isFleeing || !other.CompareTag(playerTag))
        {
            return;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void StartFleeing()
    {
        isFleeing = true;

        if (batCollider != null)
        {
            batCollider.enabled = false;
        }

        Vector2 awayFromLight = (Vector2)transform.position - flashlight.Origin;
        if (awayFromLight.sqrMagnitude < 0.001f)
        {
            awayFromLight = Vector2.up;
        }

        fleeDirection = (awayFromLight.normalized + Vector2.up * 0.65f).normalized;
        FaceDirection(fleeDirection.x);
        Destroy(gameObject, destroyAfterFlee);
    }

    private void FaceDirection(float directionX)
    {
        if (spriteRenderer == null || Mathf.Abs(directionX) < 0.01f)
        {
            return;
        }

        spriteRenderer.flipX = directionX < 0f;
    }
}
