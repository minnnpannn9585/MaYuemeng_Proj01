using System;
using System.Collections.Generic;
using UnityEngine;

public class HiddenPlatformReveal : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] targetRenderers = Array.Empty<SpriteRenderer>();
    [SerializeField] private float fadeDuration = 0.65f;

    private readonly HashSet<int> activeSourceIds = new HashSet<int>();
    private Collider2D[] targetColliders = Array.Empty<Collider2D>();
    private Color[] originalColors = Array.Empty<Color>();
    private Bounds localBounds = new Bounds(Vector3.zero, Vector3.one);
    private float visibleAmount;
    private float targetVisibleAmount;
    private float nextRevealCueTime;

    public Bounds WorldBounds
    {
        get
        {
            Vector3 worldCenter = transform.TransformPoint(localBounds.center);
            Vector3 lossyScale = transform.lossyScale;
            Vector3 worldSize = new Vector3(
                Mathf.Abs(localBounds.size.x * lossyScale.x),
                Mathf.Abs(localBounds.size.y * lossyScale.y),
                Mathf.Max(0.1f, Mathf.Abs(localBounds.size.z * lossyScale.z)));
            return new Bounds(worldCenter, worldSize);
        }
    }

    private void Awake()
    {
        EnsureCollider();

        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        targetColliders = GetComponentsInChildren<Collider2D>(true);
        CacheOriginalColors();
        CacheLocalBounds();
        visibleAmount = 0f;
        targetVisibleAmount = 0f;
        ApplyVisibility(0f);
        SetComponentsEnabled(false);
    }

    private void Update()
    {
        if (Mathf.Approximately(visibleAmount, targetVisibleAmount))
        {
            return;
        }

        float duration = Mathf.Max(0.01f, fadeDuration);
        visibleAmount = Mathf.MoveTowards(visibleAmount, targetVisibleAmount, Time.deltaTime / duration);
        ApplyVisibility(visibleAmount);

        if (visibleAmount <= 0.001f && targetVisibleAmount <= 0f)
        {
            visibleAmount = 0f;
            SetComponentsEnabled(false);
        }
    }

    public void RegisterRevealSource(UnityEngine.Object revealSource)
    {
        if (revealSource == null)
        {
            return;
        }

        if (activeSourceIds.Add(revealSource.GetInstanceID()))
        {
            ApplyRevealState(true);
        }
    }

    public void UnregisterRevealSource(UnityEngine.Object revealSource)
    {
        if (revealSource == null)
        {
            return;
        }

        if (!activeSourceIds.Remove(revealSource.GetInstanceID()))
        {
            return;
        }

        if (activeSourceIds.Count == 0)
        {
            ApplyRevealState(false);
        }
    }

    private void OnDisable()
    {
        activeSourceIds.Clear();
        visibleAmount = 0f;
        targetVisibleAmount = 0f;
        ApplyVisibility(0f);
        SetComponentsEnabled(false);
    }

    private void CacheOriginalColors()
    {
        originalColors = new Color[targetRenderers.Length];
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            originalColors[i] = targetRenderers[i] != null ? targetRenderers[i].color : Color.white;
        }
    }

    private void CacheLocalBounds()
    {
        bool hasBounds = false;
        localBounds = new Bounds(Vector3.zero, Vector3.one);

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = targetRenderers[i];
            if (spriteRenderer == null || spriteRenderer.sprite == null)
            {
                continue;
            }

            Bounds spriteBounds = spriteRenderer.sprite.bounds;
            if (!hasBounds)
            {
                localBounds = spriteBounds;
                hasBounds = true;
            }
            else
            {
                localBounds.Encapsulate(spriteBounds);
            }
        }

        if (!hasBounds)
        {
            localBounds = new Bounds(Vector3.zero, Vector3.one);
        }
    }

    private void EnsureCollider()
    {
        if (GetComponent<Collider2D>() != null)
        {
            return;
        }

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return;
        }

        boxCollider.size = spriteRenderer.sprite.bounds.size;
        boxCollider.offset = spriteRenderer.sprite.bounds.center;
    }

    private void ApplyRevealState(bool isRevealed)
    {
        targetVisibleAmount = isRevealed ? 1f : 0f;
        if (!isRevealed)
        {
            return;
        }

        SetComponentsEnabled(true);
        ApplyVisibility(visibleAmount);

        if (visibleAmount <= 0.05f && Time.time >= nextRevealCueTime)
        {
            nextRevealCueTime = Time.time + 0.45f;
            InteractionFeedback.Play(InteractionCue.Reveal, WorldBounds.center);
        }
    }

    private void SetComponentsEnabled(bool isEnabled)
    {
        if (targetRenderers != null)
        {
            for (int i = 0; i < targetRenderers.Length; i++)
            {
                if (targetRenderers[i] != null)
                {
                    targetRenderers[i].enabled = isEnabled;
                }
            }
        }

        if (targetColliders != null)
        {
            for (int i = 0; i < targetColliders.Length; i++)
            {
                if (targetColliders[i] != null)
                {
                    targetColliders[i].enabled = isEnabled;
                }
            }
        }
    }

    private void ApplyVisibility(float amount)
    {
        if (targetRenderers == null)
        {
            return;
        }

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = targetRenderers[i];
            if (spriteRenderer == null)
            {
                continue;
            }

            Color color = i < originalColors.Length ? originalColors[i] : spriteRenderer.color;
            color.a *= amount;
            spriteRenderer.color = color;
        }
    }
}
