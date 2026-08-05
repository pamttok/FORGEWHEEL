using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// this script manages the in-game lap timer by tracking minutes, seconds,milliseconds, and updating the timer UI each frame
public class LapTimeManager : MonoBehaviour
{
    // Stores the elapsed minutes.
    public static int minuteCount;

    // Stores the elapsed seconds.
    public static int secondCount;

    // Stores the elapsed milliseconds (displayed as tenths).
    public static float milliCount;

    // String representation of the milliseconds for UI display.
    public static string milliDisplay;

    // Reference to the minutes UI text.
    [SerializeField] private GameObject minuteBox;

    // Reference to the seconds UI text.
    [SerializeField] private GameObject secondBox;

    // Reference to the milliseconds UI text.
    [SerializeField] private GameObject milliBox;

    // Stores the total elapsed lap time in seconds.
    public static float rawTime;

    private void Update()
    {
        StartCoroutine(StartLapTimer());
    }

    /// Updates the lap timer values and refreshes the timer UI.
    IEnumerator StartLapTimer()
    {
        yield return new WaitForSeconds(1.3f);
        // Increment the displayed milliseconds.
        milliCount += Time.deltaTime * 10;

        // Track the total elapsed lap time.
        rawTime += Time.deltaTime;

        // Convert the milliseconds value to a display string.
        milliDisplay = milliCount.ToString("F0");

        // Update the milliseconds UI.
        milliBox.GetComponent<TMPro.TMP_Text>().text = "" + milliDisplay;

        // Advance the timer once the milliseconds reach the limit.
        if (milliCount >= 10)
        {
            // Reset milliseconds and increment seconds.
            milliCount = 0;
            secondCount += 1;
        }
        // Display seconds with a leading zero when necessary.
        if (secondCount <= 9)
        {
            secondBox.GetComponent<TMPro.TMP_Text>().text = "0" + secondCount + ".";
        }
        else
        {
            secondBox.GetComponent<TMPro.TMP_Text>().text = "" + secondCount + ".";
        }

        // Advance to the next minute after 60 seconds.
        if (secondCount >= 60)
        {
            secondCount = 0;
            minuteCount += 1;
        }

        // Display minutes with a leading zero when necessary.
        if (minuteCount <= 9)
        {
            minuteBox.GetComponent<TMPro.TMP_Text>().text = "0" + minuteCount + ":";
        }
        else
        {
            minuteBox.GetComponent<TMPro.TMP_Text>().text = "" + minuteCount + ":";
        }
    }
}