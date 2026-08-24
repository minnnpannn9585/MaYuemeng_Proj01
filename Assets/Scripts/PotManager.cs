using UnityEngine;

[DefaultExecutionOrder(200)]
public class PotManager : MonoBehaviour
{
    private const string GeneratedMushroomTrampolineName = "MushroomBouncePad";
    private const string GeneratedCactusLaserBlockerName = "CactusLaserBlocker";

    private enum PotType
    {
        Vine,
        Mushroom,
        Cactus
    }

    [SerializeField] private PotType potType = PotType.Vine;
    [SerializeField] private Pot pastTimelinePot;
    [SerializeField] private Transform presentTimelinePot;
    [SerializeField] private float timelineOffset = 50f;
    [SerializeField] private BoxCollider2D bloomArea;
    [SerializeField] private bool bloomAreaIsInPresentTimeline = true;
    [SerializeField] private SpriteRenderer presentPotSpriteRenderer;
    [SerializeField] private Sprite bloomedPresentSprite;
    [SerializeField] private GameObject ladder;
    [SerializeField] private GameObject mushroomTrampoline;
    [SerializeField] private float mushroomBounceVelocity = 8f;
    [SerializeField] private GameObject cactusLaserBlocker;

    private Sprite defaultPresentSprite;
    private BoxCollider2D mushroomBouncePadCollider;
    private BoxCollider2D cactusLaserBlockerCollider;

    public Pot PastTimelinePot => pastTimelinePot;
    public Transform PresentTimelinePot => presentTimelinePot;
    public bool IsPotAtBloomTarget
    {
        get
        {
            if (potType != PotType.Vine || pastTimelinePot == null || bloomArea == null)
            {
                return false;
            }

            Vector2 trackedPosition = bloomAreaIsInPresentTimeline
                ? GetPresentTimelinePosition(pastTimelinePot.transform.position)
                : pastTimelinePot.transform.position;

            return bloomArea.OverlapPoint(trackedPosition);
        }
    }

    private void Awake()
    {
        if (presentPotSpriteRenderer == null && presentTimelinePot != null)
        {
            presentPotSpriteRenderer = presentTimelinePot.GetComponent<SpriteRenderer>();
        }

        if (presentPotSpriteRenderer != null)
        {
            defaultPresentSprite = presentPotSpriteRenderer.sprite;
        }

        EnsureMushroomTrampoline();
        EnsureCactusLaserBlocker();

        if (presentTimelinePot == null)
        {
            UpdatePotFeatureState();
            return;
        }

        Pot presentPotBehaviour = presentTimelinePot.GetComponent<Pot>();
        if (presentPotBehaviour != null)
        {
            presentPotBehaviour.ConfigureAsPresentMirror();
        }

        UpdatePresentPotSprite();
        UpdatePotFeatureState();
    }

    private void LateUpdate()
    {
        if (pastTimelinePot == null || presentTimelinePot == null)
        {
            UpdatePotFeatureState();
            return;
        }

        Pot presentPotBehaviour = presentTimelinePot.GetComponent<Pot>();
        if (presentPotBehaviour != null)
        {
            presentPotBehaviour.SyncMirrorPosition(GetPresentTimelinePosition(pastTimelinePot.transform.position));
        }
        else
        {
            Vector3 targetPosition = GetPresentTimelinePosition(pastTimelinePot.transform.position);
            presentTimelinePot.position = new Vector3(targetPosition.x, targetPosition.y, presentTimelinePot.position.z);
        }

        UpdatePresentPotSprite();
        EnsureMushroomTrampoline();
        EnsureCactusLaserBlocker();
        UpdatePotFeatureState();
    }

    private Vector3 GetPresentTimelinePosition(Vector3 pastWorldPosition)
    {
        return pastWorldPosition + Vector3.left * timelineOffset;
    }

    private void UpdatePresentPotSprite()
    {
        if (presentPotSpriteRenderer == null)
        {
            return;
        }

        bool useBloomedSprite = potType == PotType.Vine && IsPotAtBloomTarget && bloomedPresentSprite != null;
        Sprite targetSprite = useBloomedSprite
            ? bloomedPresentSprite
            : defaultPresentSprite;

        if (targetSprite != null)
        {
            presentPotSpriteRenderer.sprite = targetSprite;
        }
    }

    private void UpdatePotFeatureState()
    {
        bool shouldActivateFeature = ShouldActivateFeature();

        if (ladder != null)
        {
            ladder.SetActive(shouldActivateFeature && potType == PotType.Vine);
        }

        if (mushroomTrampoline != null)
        {
            mushroomTrampoline.SetActive(shouldActivateFeature && potType == PotType.Mushroom);
        }

        if (cactusLaserBlocker != null)
        {
            cactusLaserBlocker.SetActive(shouldActivateFeature && potType == PotType.Cactus);
        }
    }

    private bool ShouldActivateFeature()
    {
        if (potType != PotType.Vine)
        {
            return true;
        }

        return IsPotAtBloomTarget;
    }

    private void EnsureMushroomTrampoline()
    {
        if (potType != PotType.Mushroom || presentTimelinePot == null)
        {
            return;
        }

        if (mushroomTrampoline == null)
        {
            PotBouncePad existingBouncePad = presentTimelinePot.GetComponentInChildren<PotBouncePad>(true);
            mushroomTrampoline = existingBouncePad != null
                ? existingBouncePad.gameObject
                : CreateMushroomTrampoline();
        }

        if (mushroomTrampoline == null)
        {
            return;
        }

        mushroomTrampoline.layer = presentTimelinePot.gameObject.layer;

        if (mushroomTrampoline.transform.parent != presentTimelinePot)
        {
            mushroomTrampoline.transform.SetParent(presentTimelinePot, false);
        }

        mushroomBouncePadCollider = mushroomTrampoline.GetComponent<BoxCollider2D>();
        if (mushroomBouncePadCollider == null)
        {
            mushroomBouncePadCollider = mushroomTrampoline.AddComponent<BoxCollider2D>();
        }

        mushroomBouncePadCollider.isTrigger = true;

        PotBouncePad bouncePad = mushroomTrampoline.GetComponent<PotBouncePad>();
        if (bouncePad == null)
        {
            bouncePad = mushroomTrampoline.AddComponent<PotBouncePad>();
        }

        bouncePad.BounceVelocity = mushroomBounceVelocity;

        UpdateMushroomTrampolineGeometry();
    }

    private GameObject CreateMushroomTrampoline()
    {
        GameObject trampoline = new GameObject(GeneratedMushroomTrampolineName);
        trampoline.transform.SetParent(presentTimelinePot, false);
        trampoline.transform.localRotation = Quaternion.identity;
        trampoline.transform.localScale = Vector3.one;
        trampoline.SetActive(false);
        return trampoline;
    }

    private void UpdateMushroomTrampolineGeometry()
    {
        if (mushroomTrampoline == null || mushroomBouncePadCollider == null)
        {
            return;
        }

        Sprite referenceSprite = presentPotSpriteRenderer != null ? presentPotSpriteRenderer.sprite : null;
        Vector2 spriteSize = referenceSprite != null ? referenceSprite.bounds.size : Vector2.one;
        float topY = referenceSprite != null ? referenceSprite.bounds.max.y : 0.5f;
        float padWidth = Mathf.Max(0.5f, spriteSize.x * 0.7f);
        float padHeight = Mathf.Max(0.18f, spriteSize.y * 0.18f);

        mushroomTrampoline.transform.localPosition = new Vector3(0f, topY - (padHeight * 0.5f), 0f);
        mushroomBouncePadCollider.offset = Vector2.zero;
        mushroomBouncePadCollider.size = new Vector2(padWidth, padHeight);
    }

    private void EnsureCactusLaserBlocker()
    {
        if (potType != PotType.Cactus || presentTimelinePot == null)
        {
            return;
        }

        if (cactusLaserBlocker == null)
        {
            CactusLaserTarget existingLaserTarget = presentTimelinePot.GetComponentInChildren<CactusLaserTarget>(true);
            cactusLaserBlocker = existingLaserTarget != null
                ? existingLaserTarget.gameObject
                : CreateCactusLaserBlocker();
        }

        if (cactusLaserBlocker == null)
        {
            return;
        }

        cactusLaserBlocker.layer = presentTimelinePot.gameObject.layer;

        if (cactusLaserBlocker.transform.parent != presentTimelinePot)
        {
            cactusLaserBlocker.transform.SetParent(presentTimelinePot, false);
        }

        cactusLaserBlockerCollider = cactusLaserBlocker.GetComponent<BoxCollider2D>();
        if (cactusLaserBlockerCollider == null)
        {
            cactusLaserBlockerCollider = cactusLaserBlocker.AddComponent<BoxCollider2D>();
        }

        cactusLaserBlockerCollider.isTrigger = true;

        if (cactusLaserBlocker.GetComponent<CactusLaserTarget>() == null)
        {
            cactusLaserBlocker.AddComponent<CactusLaserTarget>();
        }

        UpdateCactusLaserBlockerGeometry();
    }

    private GameObject CreateCactusLaserBlocker()
    {
        GameObject blocker = new GameObject(GeneratedCactusLaserBlockerName);
        blocker.transform.SetParent(presentTimelinePot, false);
        blocker.transform.localRotation = Quaternion.identity;
        blocker.transform.localScale = Vector3.one;
        blocker.SetActive(false);
        return blocker;
    }

    private void UpdateCactusLaserBlockerGeometry()
    {
        if (cactusLaserBlocker == null || cactusLaserBlockerCollider == null)
        {
            return;
        }

        Sprite referenceSprite = presentPotSpriteRenderer != null ? presentPotSpriteRenderer.sprite : null;
        Vector2 spriteSize = referenceSprite != null ? referenceSprite.bounds.size : Vector2.one;
        Vector2 spriteCenter = referenceSprite != null ? referenceSprite.bounds.center : Vector2.zero;
        float blockerWidth = Mathf.Max(0.4f, spriteSize.x * 0.55f);
        float blockerHeight = Mathf.Max(0.4f, spriteSize.y * 0.7f);

        cactusLaserBlocker.transform.localPosition = new Vector3(spriteCenter.x, spriteCenter.y, 0f);
        cactusLaserBlockerCollider.offset = Vector2.zero;
        cactusLaserBlockerCollider.size = new Vector2(blockerWidth, blockerHeight);
    }
}
