using System.Collections;
using UnityEngine;

// this script controls the waypoint progression for AICar on the track and updates the active target marker and advances to the next marker whenever the car reaches the current checkpoint
public class AICarTracker : MonoBehaviour
{
    // Reference to the target marker that the AI car follows.
    [SerializeField] private GameObject TheMarker;

    [Header("Track Markers (in order)")]

    // Ordered list of track markers that define the AI racing path.
    [SerializeField] private GameObject[] markers = new GameObject[30];

    // Keeps track of the current marker index.
    public int MarkTracker { get; private set; } = 0;

    //Continuously updates the target marker position to match the current waypoint
    private void Update()
    {
        // Exit if there are no markers assigned or the index is invalid.
        if (markers.Length == 0 || MarkTracker >= markers.Length)
            return;

        // Move the target marker to the current waypoint.
        TheMarker.transform.position = markers[MarkTracker].transform.position;
    }

    // Advances to the next waypoint when AICar enters the checkpoint trigger
    private IEnumerator OnTriggerEnter(Collider collision)
    {
        // Check if the collider belongs to AICar
        if (collision.gameObject.CompareTag("AICarTracker"))
        {
            // Disable the collider temporarily to prevent multiple trigger events.
            GetComponent<BoxCollider>().enabled = false;

            // Advance to the next waypoint.
            MarkTracker++;

            // Loop back to the first waypoint after the last one.
            if (MarkTracker >= markers.Length)
                MarkTracker = 0;

            // Wait before re-enabling the trigger.
            yield return new WaitForSeconds(0.5f);

            // Re-enable the checkpoint collider.
            GetComponent<BoxCollider>().enabled = true;
        }
    }
}