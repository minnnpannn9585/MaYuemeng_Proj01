using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trap : MonoBehaviour
{
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private string playerTag = "Player";
    private Rigidbody2D lastRespawnedBody;
    private float nextRespawnTime;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        if (respawnPoint == null)
        {
            Debug.LogWarning("Trap: respawnPoint is not assigned.", this);
            return;
        }

        Rigidbody2D playerRb = other.attachedRigidbody;
        if (playerRb != null && playerRb == lastRespawnedBody && Time.time < nextRespawnTime) return;
        lastRespawnedBody = playerRb;
        nextRespawnTime = Time.time + 0.2f;
        Transform playerTransform = playerRb != null ? playerRb.transform : other.transform;
        Vector3 caughtPosition = playerTransform.position;
        Vector3 destination = respawnPoint.position;
        InteractionFeedback.Play(InteractionCue.Trap, caughtPosition);
        playerTransform.position = destination;
        NewBehaviourScript player = playerTransform.GetComponent<NewBehaviourScript>();
        if (player != null) player.ResetLandingFeedback();
        InteractionFeedback.Play(InteractionCue.Respawn, destination, false, true);

        if (playerRb != null)
        {
            playerRb.position = destination;
            playerRb.velocity = Vector2.zero;
            playerRb.angularVelocity = 0f;
        }
    }
}
