using UnityEngine;
using UnityEngine.Rendering.Universal;

[DefaultExecutionOrder(200)]
public class PotManager : MonoBehaviour
{
    private const string GeneratedMushroomTrampolineName = "MushroomBouncePad";
    private const string GeneratedCactusLaserBlockerName = "CactusLaserBlocker";
    private const string GeneratedGlowRevealAreaName = "GlowRevealArea";

    private enum PotType
    {
        Vine,
        Mushroom,
        Cactus,
        Glow
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
    [SerializeField] private GameObject glowRevealArea;
    [SerializeField] private LayerMask glowRevealMask = ~0;
    [SerializeField] private Vector2 glowAreaSize = new Vector2(6f, 3f);
    [SerializeField] private float glowAreaForwardOffset = 3f;
    [SerializeField] private float glowAreaVerticalOffset = 1.25f;
    [SerializeField] private bool glowFacesRight = true;
    [SerializeField] private Color glowPresentTint = new Color(0.9f, 1f, 0.65f, 1f);
    [SerializeField] private Color glowLightColor = new Color(1f, 0.96f, 0.76f, 1f);
    [SerializeField] private float glowLightIntensity = 0.8f;
    [SerializeField] private float glowLightRadius = 5.5f;

    private Sprite defaultPresentSprite;
    private Color defaultPresentColor = Color.white;
    private BoxCollider2D mushroomBouncePadCollider;
    private BoxCollider2D cactusLaserBlockerCollider;
    private BoxCollider2D glowRevealAreaCollider;
    private GlowRevealArea glowRevealAreaBehaviour;
    private Light2D glowLight;

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
            defaultPresentColor = presentPotSpriteRenderer.color;
        }

        EnsureMushroomTrampoline();
        EnsureCactusLaserBlocker();
        EnsureGlowRevealArea();

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
        EnsureGlowRevealArea();
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

        presentPotSpriteRenderer.color = potType == PotType.Glow
            ? glowPresentTint
            : defaultPresentColor;
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

        if (glowRevealArea != null)
        {
            glowRevealArea.SetActive(shouldActivateFeature && potType == PotType.Glow);
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
        Vector2 spriteSize = referenceSprite != null
            ? new Vector2(referenceSprite.bounds.size.x, referenceSprite.bounds.size.y)
            : Vector2.one;
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
        Vector2 spriteSize = referenceSprite != null
            ? new Vector2(referenceSprite.bounds.size.x, referenceSprite.bounds.size.y)
            : Vector2.one;
        Vector2 spriteCenter = referenceSprite != null
            ? new Vector2(referenceSprite.bounds.center.x, referenceSprite.bounds.center.y)
            : Vector2.zero;
        float blockerWidth = Mathf.Max(0.4f, spriteSize.x * 0.55f);
        float blockerHeight = Mathf.Max(0.4f, spriteSize.y * 0.7f);

        cactusLaserBlocker.transform.localPosition = new Vector3(spriteCenter.x, spriteCenter.y, 0f);
        cactusLaserBlockerCollider.offset = Vector2.zero;
        cactusLaserBlockerCollider.size = new Vector2(blockerWidth, blockerHeight);
    }

    private void EnsureGlowRevealArea()
    {
        if (potType != PotType.Glow || presentTimelinePot == null)
        {
            return;
        }

        if (glowRevealArea == null)
        {
            glowRevealAreaBehaviour = presentTimelinePot.GetComponentInChildren<GlowRevealArea>(true);
            glowRevealArea = glowRevealAreaBehaviour != null
                ? glowRevealAreaBehaviour.gameObject
                : CreateGlowRevealArea();
        }

        if (glowRevealArea == null)
        {
            return;
        }

        glowRevealArea.layer = presentTimelinePot.gameObject.layer;

        if (glowRevealArea.transform.parent != presentTimelinePot)
        {
            glowRevealArea.transform.SetParent(presentTimelinePot, false);
        }

        glowRevealAreaCollider = glowRevealArea.GetComponent<BoxCollider2D>();
        if (glowRevealAreaCollider == null)
        {
            glowRevealAreaCollider = glowRevealArea.AddComponent<BoxCollider2D>();
        }

        glowRevealAreaCollider.isTrigger = true;

        glowRevealAreaBehaviour = glowRevealArea.GetComponent<GlowRevealArea>();
        if (glowRevealAreaBehaviour == null)
        {
            glowRevealAreaBehaviour = glowRevealArea.AddComponent<GlowRevealArea>();
        }

        glowRevealAreaBehaviour.RevealMask = glowRevealMask;

        glowLight = glowRevealArea.GetComponent<Light2D>();
        if (glowLight == null)
        {
            glowLight = glowRevealArea.AddComponent<Light2D>();
        }

        ConfigureGlowLight();
        UpdateGlowRevealAreaGeometry();
    }

    private GameObject CreateGlowRevealArea()
    {
        GameObject revealArea = new GameObject(GeneratedGlowRevealAreaName);
        revealArea.transform.SetParent(presentTimelinePot, false);
        revealArea.transform.localRotation = Quaternion.identity;
        revealArea.transform.localScale = Vector3.one;
        revealArea.SetActive(false);
        return revealArea;
    }

    private void ConfigureGlowLight()
    {
        if (glowLight == null)
        {
            return;
        }

        glowLight.lightType = Light2D.LightType.Point;
        glowLight.color = glowLightColor;
        glowLight.intensity = glowLightIntensity;
        glowLight.pointLightInnerAngle = 40f;
        glowLight.pointLightOuterAngle = 75f;
    }

    private void UpdateGlowRevealAreaGeometry()
    {
        if (glowRevealArea == null || glowRevealAreaCollider == null)
        {
            return;
        }

        Sprite referenceSprite = presentPotSpriteRenderer != null ? presentPotSpriteRenderer.sprite : null;
        Vector2 spriteSize = referenceSprite != null
            ? new Vector2(referenceSprite.bounds.size.x, referenceSprite.bounds.size.y)
            : Vector2.one;
        Vector2 spriteCenter = referenceSprite != null
            ? new Vector2(referenceSprite.bounds.center.x, referenceSprite.bounds.center.y)
            : new Vector2(0f, 0.5f);
        float directionSign = glowFacesRight ? 1f : -1f;

        if (presentPotSpriteRenderer != null && presentPotSpriteRenderer.flipX)
        {
            directionSign *= -1f;
        }
        else if (presentTimelinePot.lossyScale.x < 0f)
        {
            directionSign *= -1f;
        }

        float revealWidth = Mathf.Max(glowAreaSize.x, spriteSize.x * 1.8f);
        float revealHeight = Mathf.Max(glowAreaSize.y, spriteSize.y * 1.15f);
        float revealForwardOffset = Mathf.Max(glowAreaForwardOffset, (spriteSize.x * 0.5f) + (revealWidth * 0.5f) - 0.2f);

        glowRevealArea.transform.localPosition = new Vector3(
            spriteCenter.x + (directionSign * revealForwardOffset),
            spriteCenter.y + glowAreaVerticalOffset,
            0f);
        glowRevealArea.transform.localRotation = Quaternion.Euler(0f, 0f, directionSign >= 0f ? 0f : 180f);

        glowRevealAreaCollider.offset = Vector2.zero;
        glowRevealAreaCollider.size = new Vector2(revealWidth, revealHeight);

        if (glowLight != null)
        {
            glowLight.pointLightInnerRadius = Mathf.Max(1f, glowLightRadius * 0.35f);
            glowLight.pointLightOuterRadius = Mathf.Max(glowLightRadius, revealWidth);
        }
    }
}
