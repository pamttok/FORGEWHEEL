using UnityEngine;

//Activates the lap completion trigger after the player reaches the halfway point
/// <summary>
/// Marks the halfway point of a lap. When a car (excluding the tracking
/// helper objects) crosses this trigger, arms the lap-complete trigger
/// so crossing the finish line now counts, and disarms itself so it
/// can't be re-triggered until reset.
/// </summary>
public class HalfPointTrigger : MonoBehaviour
{
    // ---------------------------------------------------------------
    // Inspector References
    // ---------------------------------------------------------------

    [Tooltip("Trigger collider that registers a completed lap once the halfway point has been passed.")]
    [SerializeField] private GameObject lapCompleteTrigger;

    [Tooltip("This half-lap trigger's own GameObject; disabled after firing to prevent re-triggering.")]
    [SerializeField] private GameObject halfLapTrigger;

    // ---------------------------------------------------------------
    // Trigger Events
    // ---------------------------------------------------------------

    /// <summary>
    /// Fired when something enters this trigger's collider. Ignores the
    /// "MyCar" and "AICarTracker" tags (presumably non-collider tracking
    /// objects that shouldn't count as crossing the marker), and otherwise
    /// arms the lap-complete trigger and disarms this one.
    /// </summary>
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.tag == "MyCar" || collision.gameObject.tag == "AICarTracker")
            return;
        lapCompleteTrigger.SetActive(true);
        halfLapTrigger.SetActive(false);
    }
}
