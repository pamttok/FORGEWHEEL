using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Updates the vehicle speedometer by displaying the current speed
/// and rotating the speedometer needle based on the vehicle's velocity.
/// </summary>
public class Speedometer : MonoBehaviour
{
    // Reference to the vehicle's Rigidbody.
    [SerializeField] private Rigidbody target;

    // Maximum speed represented on the speedometer.
    public float maxSpeed = 40f;

    // Needle angle corresponding to zero speed.
    public float minSpeedArrowAngle;

    // Needle angle corresponding to maximum speed.
    public float maxSpeedArrowAngle;

    // UI text displaying the current speed.
    [SerializeField] private TMP_Text speedLabel;

    // Speedometer needle.
    [SerializeField] private RectTransform arrow;

    // Stores the current vehicle speed in kilometers per hour.
    private float speed = 0f;

    /// <summary>
    /// Updates the displayed speed and rotates the speedometer
    /// needle based on the vehicle's current velocity.
    /// </summary>
    private void FixedUpdate()
    {
        // Convert the Rigidbody's speed from meters per second to kilometers per hour.
        speed = target.linearVelocity.magnitude * 3.6f;

        // Update the speed display.
        if (speedLabel != null)
        {
            speedLabel.text = ((int)speed) + "-KPH";
        }

        // Rotate the speedometer needle according to the current speed.
        if (arrow != null)
        {
            arrow.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(minSpeedArrowAngle, maxSpeedArrowAngle, speed / maxSpeed));
        }
    }
}