using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(300)]
[RequireComponent(typeof(Collider2D))]
public class GlowRevealArea : MonoBehaviour
{
    [SerializeField] private LayerMask revealMask = ~0;

    private readonly HashSet<HiddenPlatformReveal> revealedPlatforms = new HashSet<HiddenPlatformReveal>();
    private readonly HashSet<HiddenPlatformReveal> currentFramePlatforms = new HashSet<HiddenPlatformReveal>();
    private readonly List<HiddenPlatformReveal> stalePlatforms = new List<HiddenPlatformReveal>();

    private Collider2D revealCollider;
    private TimeTravel timeTravel;

    public LayerMask RevealMask
    {
        get => revealMask;
        set => revealMask = value;
    }

    private void Awake()
    {
        ResolveCollider();
    }

    private void OnEnable()
    {
        ResolveCollider();
        EnsureHiddenPlatformsOnRevealLayer();
        RefreshRevealedPlatforms();
        RefreshIlluminatedBats();
    }

    private void LateUpdate()
    {
        ResolveCollider();
        RefreshRevealedPlatforms();
        RefreshIlluminatedBats();
    }

    private void OnDisable()
    {
        ClearRevealedPlatforms();
    }

    private void EnsureHiddenPlatformsOnRevealLayer()
    {
        SpriteRenderer[] spriteRenderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = spriteRenderers[i];
            if (spriteRenderer == null || !MatchesRevealMask(spriteRenderer.gameObject.layer))
            {
                continue;
            }

            if (spriteRenderer.GetComponentInParent<HiddenPlatformReveal>() != null)
            {
                continue;
            }

            spriteRenderer.gameObject.AddComponent<HiddenPlatformReveal>();
        }
    }

    private void RefreshIlluminatedBats()
    {
        if (revealCollider == null || IsPlayerInPastTimeline())
        {
            return;
        }

        Bat[] bats = FindObjectsByType<Bat>(FindObjectsSortMode.None);
        for (int i = 0; i < bats.Length; i++)
        {
            Bat bat = bats[i];
            if (bat == null || bat.IsFleeing)
            {
                continue;
            }

            if (IsInsideGlowArea(bat.WorldBounds))
            {
                bat.StartFleeing();
            }
        }
    }

    private void RefreshRevealedPlatforms()
    {
        if (revealCollider == null)
        {
            return;
        }

        Physics2D.SyncTransforms();
        currentFramePlatforms.Clear();
        HiddenPlatformReveal[] platforms = FindObjectsByType<HiddenPlatformReveal>(FindObjectsSortMode.None);
        for (int i = 0; i < platforms.Length; i++)
        {
            HiddenPlatformReveal platform = platforms[i];
            if (platform == null || !MatchesRevealMask(platform.gameObject.layer))
            {
                continue;
            }

            if (!IsInsideGlowArea(platform.WorldBounds))
            {
                continue;
            }

            currentFramePlatforms.Add(platform);
        }

        if (IsPlayerInPastTimeline())
        {
            return;
        }

        foreach (HiddenPlatformReveal platform in currentFramePlatforms)
        {
            if (revealedPlatforms.Add(platform))
            {
                platform.RegisterRevealSource(this);
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

    public void SetRevealCollider(Collider2D collider)
    {
        revealCollider = collider;
        if (revealCollider != null)
        {
            revealCollider.isTrigger = true;
        }
    }

    private void ResolveCollider()
    {
        if (revealCollider != null && revealCollider.enabled)
        {
            revealCollider.isTrigger = true;
            return;
        }

        Collider2D[] colliders = GetComponents<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null && colliders[i].enabled)
            {
                revealCollider = colliders[i];
                revealCollider.isTrigger = true;
                return;
            }
        }

        revealCollider = GetComponentInChildren<Collider2D>();
        if (revealCollider != null)
        {
            revealCollider.isTrigger = true;
        }
    }

    private bool IsInsideGlowArea(Bounds platformBounds)
    {
        if (revealCollider.OverlapPoint(platformBounds.center))
        {
            return true;
        }

        Vector2 min = platformBounds.min;
        Vector2 max = platformBounds.max;
        if (revealCollider.OverlapPoint(min)
            || revealCollider.OverlapPoint(max)
            || revealCollider.OverlapPoint(new Vector2(min.x, max.y))
            || revealCollider.OverlapPoint(new Vector2(max.x, min.y)))
        {
            return true;
        }

        return platformBounds.Contains(revealCollider.bounds.center);
    }

    private bool IsPlayerInPastTimeline()
    {
        if (timeTravel == null)
        {
            timeTravel = FindFirstObjectByType<TimeTravel>();
        }

        return timeTravel != null && timeTravel.IsInPastTimeline;
    }

    private bool MatchesRevealMask(int layer)
    {
        return (revealMask.value & (1 << layer)) != 0;
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
