using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Diamond : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private GameObject collectEffectPrefab;
    [SerializeField, Tooltip("Opt in to the old prefab instead of the shared forest feedback.")] private bool useLegacyCollectEffect;
    private bool collected;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || !other.CompareTag(playerTag))
        {
            return;
        }

        collected = true;
        bool legacy = useLegacyCollectEffect && collectEffectPrefab != null;
        InteractionFeedback.Play(InteractionCue.Collect, transform.position, true, !legacy);

        if (legacy)
        {
            GameObject effect = Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);
            float lifetime = 1f;
            foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>())
            {
                var main = particles.main;
                main.loop = false;
                lifetime = Mathf.Max(lifetime, main.startDelay.constantMax + main.duration + main.startLifetime.constantMax);
            }
            Destroy(effect, lifetime + 0.1f);
        }

        Destroy(gameObject);
    }
}
