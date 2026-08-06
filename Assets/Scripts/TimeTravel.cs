using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeTravel : MonoBehaviour
{
    [SerializeField] private float travelDistance = 50f;
    [SerializeField] private Pot carriedPot;

    private bool isInPastTimeline;

    public bool IsInPastTimeline => isInPastTimeline;

    private void Start()
    {
        TryResolveCarriedPot();
    }

    private void Update()
    {
        TryResolveCarriedPot();

        if (Input.GetKeyDown(KeyCode.R))
        {
            if (carriedPot != null && carriedPot.IsHeld)
            {
                return;
            }

            ToggleTimeTravel();
        }
    }

    private void ToggleTimeTravel()
    {
        float moveX = isInPastTimeline ? -travelDistance : travelDistance;
        transform.position += new Vector3(moveX, 0f, 0f);
        isInPastTimeline = !isInPastTimeline;
    }

    private void TryResolveCarriedPot()
    {
        if (carriedPot != null)
        {
            return;
        }

        PotManager potManager = FindFirstObjectByType<PotManager>();
        if (potManager != null)
        {
            carriedPot = potManager.PastTimelinePot;
        }
    }
}
