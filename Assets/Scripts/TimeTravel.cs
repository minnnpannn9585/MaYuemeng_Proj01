using UnityEngine;

public class TimeTravel : MonoBehaviour
{
    [SerializeField] private float travelDistance = 50f;

    private bool isInPastTimeline;
    private PotManager[] potManagers = System.Array.Empty<PotManager>();

    public bool IsInPastTimeline => isInPastTimeline;

    private void Start()
    {
        CachePotManagers();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (IsAnyPastPotHeld())
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

    private void CachePotManagers()
    {
        potManagers = FindObjectsByType<PotManager>(FindObjectsSortMode.None);
    }

    private bool IsAnyPastPotHeld()
    {
        if (potManagers == null || potManagers.Length == 0)
        {
            CachePotManagers();
        }

        for (int i = 0; i < potManagers.Length; i++)
        {
            PotManager potManager = potManagers[i];
            if (potManager == null)
            {
                CachePotManagers();
                break;
            }

            Pot pastPot = potManager.PastTimelinePot;
            if (pastPot != null && pastPot.IsHeld)
            {
                return true;
            }
        }

        return false;
    }
}
