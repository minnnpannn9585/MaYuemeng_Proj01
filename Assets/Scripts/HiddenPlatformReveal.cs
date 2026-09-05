using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HiddenPlatformReveal : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] targetRenderers = Array.Empty<SpriteRenderer>();

    private readonly HashSet<int> activeSourceIds = new HashSet<int>();

    private void Awake()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        ApplyRevealState(false);
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
        ApplyRevealState(false);
    }

    private void ApplyRevealState(bool isRevealed)
    {
        if (targetRenderers == null)
        {
            return;
        }

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] != null)
            {
                targetRenderers[i].enabled = isRevealed;
            }
        }
    }
}
