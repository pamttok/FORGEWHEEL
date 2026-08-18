using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Updates the position-display UI to show "2ND" when the tagged
/// position-tracking object enters this trigger — used to reflect a
/// race-position change (e.g. the player's car being overtaken).
/// </summary>
public class PosDown : MonoBehaviour
{
    // ---------------------------------------------------------------
    // Inspector References
    // ---------------------------------------------------------------

    [Tooltip("UI text element showing the player's current race position.")]
    [SerializeField] private GameObject positionDisplay;

    // ---------------------------------------------------------------
    // Trigger Events
    // ---------------------------------------------------------------

    /// <summary>
    /// Fired when something enters this trigger. If it's tagged "CarPos"
    /// (the position-tracking marker), updates the position display to "2ND".
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "CarPos")
        {
            positionDisplay.GetComponent<TMPro.TMP_Text>().text = "2ND";
        }
    }
}
