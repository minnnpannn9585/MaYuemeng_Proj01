using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(300)]
[RequireComponent(typeof(Collider2D))]
public class GlowRevealArea : MonoBehaviour
{
    [SerializeField] private LayerMask revealMask = ~0;

    private readonly Collider2D[] overlapHits = new Collider2D[32];
    private readonly HashSet<HiddenPlatformReveal> revealedPlatforms = new HashSet<HiddenPlatformReveal>();
    private readonly HashSet<HiddenPlatformReveal> currentFramePlatforms = new HashSet<HiddenPlatformReveal>();
    private readonly List<HiddenPlatformReveal> stalePlatforms = new List<HiddenPlatformReveal>();

    private Collider2D revealCollider;
    private ContactFilter2D revealFilter;

    public LayerMask RevealMask
    {
        get => revealMask;
        set
        {
            revealMask = value;
            ConfigureFilter();
        }
    }

    private void Awake()
    {
        revealCollider = GetComponent<Collider2D>();
        revealCollider.isTrigger = true;
        ConfigureFilter();
    }

    private void OnEnable()
    {
        RefreshRevealedPlatforms();
    }

    private void LateUpdate()
    {
        RefreshRevealedPlatforms();
    }

    private void OnDisable()
    {
        ClearRevealedPlatforms();
    }

    private void ConfigureFilter()
    {
        revealFilter.useLayerMask = true;
        revealFilter.SetLayerMask(revealMask);
        revealFilter.useTriggers = false;
    }

    private void RefreshRevealedPlatforms()
    {
        if (revealCollider == null)
        {
            return;
        }

        Physics2D.SyncTransforms();
        currentFramePlatforms.Clear();

        int overlapCount = revealCollider.OverlapCollider(revealFilter, overlapHits);
        for (int i = 0; i < overlapCount; i++)
        {
            Collider2D overlap = overlapHits[i];
            if (overlap == null)
            {
                continue;
            }

            HiddenPlatformReveal hiddenPlatform = overlap.GetComponent<HiddenPlatformReveal>();
            if (hiddenPlatform == null)
            {
                hiddenPlatform = overlap.GetComponentInParent<HiddenPlatformReveal>();
            }

            if (hiddenPlatform == null || !currentFramePlatforms.Add(hiddenPlatform))
            {
                continue;
            }

            if (revealedPlatforms.Add(hiddenPlatform))
            {
                hiddenPlatform.RegisterRevealSource(this);
            }
        }

        stalePlatforms.Clear();
        foreach (HiddenPlatformReveal revealedPlatform in revealedPlatforms)
        {
            if (!currentFramePlatforms.Contains(revealedPlatform))
            {
                stalePlatforms.Add(revealedPlatform);
            }
        }

        for (int i = 0; i < stalePlatforms.Count; i++)
        {
            HiddenPlatformReveal stalePlatform = stalePlatforms[i];
            revealedPlatforms.Remove(stalePlatform);
            if (stalePlatform != null)
            {
                stalePlatform.UnregisterRevealSource(this);
            }
        }
    }

    private void ClearRevealedPlatforms()
    {
        foreach (HiddenPlatformReveal revealedPlatform in revealedPlatforms)
        {
            if (revealedPlatform != null)
            {
                revealedPlatform.UnregisterRevealSource(this);
            }
        }

        revealedPlatforms.Clear();
        currentFramePlatforms.Clear();
        stalePlatforms.Clear();
    }
}
