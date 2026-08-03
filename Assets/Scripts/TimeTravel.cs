using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeTravel : MonoBehaviour
{
    [SerializeField] private float travelDistance = 50f;
    [SerializeField] private Pot carriedPot;

    private bool isInFutureTimeline;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
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
        float moveX = isInFutureTimeline ? -travelDistance : travelDistance;
        transform.position += new Vector3(moveX, 0f, 0f);
        isInFutureTimeline = !isInFutureTimeline;
    }
}
