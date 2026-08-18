using UnityEngine;
using TMPro;

/// <summary>
/// Displays a countdown timer in MM:SS format on a TextMeshPro UI element.
/// Timer text turns red once time has expired.
/// </summary>
public class CountdownTimer : MonoBehaviour
{
    // ---------------------------------------------------------------
    // Inspector References
    // ---------------------------------------------------------------

    [Tooltip("UI text element used to display the formatted countdown.")]
    [SerializeField] private TextMeshProUGUI timer;

    [Tooltip("Time remaining on the countdown, in seconds.")]
    [SerializeField] float remainingTime;

    // ---------------------------------------------------------------
    // Unity Lifecycle
    // ---------------------------------------------------------------

    /// <summary>
    /// Ticks the countdown down every frame, clamps it at zero and flags
    /// expiry with a red color change, and refreshes the displayed MM:SS text.
    /// </summary>
    private void Update()
    {
        if (remainingTime > 0)
        {
            // Countdown still active: tick time down.
            remainingTime -= Time.deltaTime;
        }
        else if (remainingTime < 0)
        {
            // Timer has expired: clamp to zero and signal visually.
            remainingTime = 0;
            timer.color = Color.red;
        }

        // Convert remaining seconds into minutes/seconds for display.
        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);
        timer.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
