using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trap : MonoBehaviour
{
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private string playerTag = "Player";

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

        other.transform.position = respawnPoint.position;

        Rigidbody2D playerRb = other.attachedRigidbody;
        if (playerRb != null)
        {
            playerRb.velocity = Vector2.zero;
            playerRb.angularVelocity = 0f;
        }
    }
}
