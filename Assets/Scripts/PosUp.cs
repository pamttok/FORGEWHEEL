using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Updates the player's race position display when the
/// player reaches the first-place position trigger.
/// </summary>
public class PosUp : MonoBehaviour
{
    // Reference to the race position UI text.
    [SerializeField] private GameObject positionDisplay;

    /// <summary>
    /// Called when another collider enters this trigger.
    /// Updates the position display if the detected object
    /// has the "CarPos" tag.
    /// </summary>
    /// <param name="other">The collider that entered the trigger.</param>
    private void OnTriggerEnter(Collider other)
    {
        // Check if the detected object is the position trigger.
        if (other.tag == "CarPos")
        {
            // Display the player's current race position.
            positionDisplay.GetComponent<TMPro.TMP_Text>().text = "1ST";
        }
    }
}