using UnityEngine;

[DefaultExecutionOrder(200)]
public class PotManager : MonoBehaviour
{
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
    [SerializeField] private GameObject cactusLaserBlocker;

    private Sprite defaultPresentSprite;

    public Pot PastTimelinePot => pastTimelinePot;
    public Transform PresentTimelinePot => presentTimelinePot;
    public bool IsPotAtBloomTarget
    {
        get
        {
            if (pastTimelinePot == null || bloomArea == null)
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

        Sprite targetSprite = IsPotAtBloomTarget && bloomedPresentSprite != null
            ? bloomedPresentSprite
            : defaultPresentSprite;

        if (targetSprite != null)
        {
            presentPotSpriteRenderer.sprite = targetSprite;
        }
    }

    private void UpdatePotFeatureState()
    {
        bool shouldActivateFeature = IsPotAtBloomTarget;

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
}
