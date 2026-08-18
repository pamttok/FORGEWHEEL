using UnityEngine;
using TMPro;

// this script handles lap completion, updates the UI,saves the player's best lap,resets the timer,and enables the race finish trigger
/// <summary>
/// Handles lap-completion logic: increments the lap counter, checks/updates
/// the best lap time (persisted via PlayerPrefs), refreshes the lap time UI,
/// resets the live lap timer for the next lap, re-arms the half-lap trigger,
/// and activates the race-finish trigger once the required lap count is reached.
/// </summary>
public class LapComplete : MonoBehaviour
{
    // ---------------------------------------------------------------
    // Inspector References
    // ---------------------------------------------------------------

    [Header("Triggers")]
    [Tooltip("Trigger collider that fires this lap-complete logic; disabled after use until re-armed.")]
    [SerializeField] private GameObject lapCompleteTrigger;

    [Tooltip("Trigger collider marking the halfway point of the next lap; re-armed after this lap completes.")]
    [SerializeField] private GameObject halfLapTrigger;

    [Tooltip("Object activated once the required number of laps has been completed.")]
    [SerializeField] private GameObject raceFinish;

    [Header("UI")]
    [Tooltip("UI text element displaying the best lap's minutes.")]
    [SerializeField] private TMP_Text minuteDisplay;

    [Tooltip("UI text element displaying the best lap's seconds.")]
    [SerializeField] private TMP_Text secondDisplay;

    [Tooltip("UI text element displaying the best lap's milliseconds.")]
    [SerializeField] private TMP_Text milliDisplay;

    [Tooltip("UI text element showing the total number of completed laps.")]
    [SerializeField] private TMP_Text lapCounter;

    [Header("Race Settings")]
    [Tooltip("Number of laps required to finish the race.")]
    [SerializeField] private int totalLaps = 1;

    // ---------------------------------------------------------------
    // Internal State
    // ---------------------------------------------------------------

    // Total number of laps completed so far.
    private int lapsDone = 0;

    // ---------------------------------------------------------------
    // Trigger Events
    // ---------------------------------------------------------------

    /// <summary>
    /// Fired when the player crosses the lap-complete trigger (ignoring
    /// tracking-helper tags). Updates lap count, compares/updates the best
    /// lap time and persists it, resets the live timer, re-arms the triggers
    /// for the next lap, and triggers race-finish once the final lap is done.
    /// </summary>
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.tag == "MyCar" || collision.gameObject.tag == "AICarTracker")
            return;

        lapsDone++;
        lapCounter.text = lapsDone.ToString();

        // Load the previously saved best raw lap time for comparison.
        float bestRawTime = PlayerPrefs.GetFloat("rawTime", 0f);

        // Treat a saved value of 0 as "no best time yet" — update if this is
        // the first recorded lap, or if it beat the current best.
        if (bestRawTime == 0f || LapTimeManager.rawTime < bestRawTime)
        {
            minuteDisplay.text = LapTimeManager.minuteCount.ToString("00") + ":";
            secondDisplay.text = LapTimeManager.secondCount.ToString("00") + ".";
            milliDisplay.text = LapTimeManager.milliCount.ToString("F0");

            // Persist this lap's time as the new saved best-lap data.
            PlayerPrefs.SetInt("MinSave", LapTimeManager.minuteCount);
            PlayerPrefs.SetInt("SecSave", LapTimeManager.secondCount);
            PlayerPrefs.SetFloat("MilliSave", LapTimeManager.milliCount);
            PlayerPrefs.SetFloat("rawTime", LapTimeManager.rawTime);
            PlayerPrefs.Save();
        }

        // Reset timer.
        LapTimeManager.minuteCount = 0;
        LapTimeManager.secondCount = 0;
        LapTimeManager.milliCount = 0;
        LapTimeManager.rawTime = 0;

        // Prepare next lap.
        halfLapTrigger.SetActive(true);
        lapCompleteTrigger.SetActive(false);

        // Final lap reached.
        if (lapsDone >= totalLaps)
        {
            raceFinish.SetActive(true);
        }
    }
}
