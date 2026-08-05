using UnityEngine;
using TMPro;

// this script handles lap completion, updates the UI,saves the player's best lap,resets the timer,and enables the race finish trigger
public class LapComplete : MonoBehaviour
{
    [Header("Triggers")]
    [SerializeField] private GameObject lapCompleteTrigger;
    [SerializeField] private GameObject halfLapTrigger;
    [SerializeField] private GameObject raceFinish;

    [Header("UI")]
    [SerializeField] private TMP_Text minuteDisplay;
    [SerializeField] private TMP_Text secondDisplay;
    [SerializeField] private TMP_Text milliDisplay;
    [SerializeField] private TMP_Text lapCounter;

    [Header("Race Settings")]
    [SerializeField] private int totalLaps = 1;

    private int lapsDone = 0;

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.tag == "MyCar" || collision.gameObject.tag == "AICarTracker")
            return;

        lapsDone++;
        lapCounter.text = lapsDone.ToString();

        float bestRawTime = PlayerPrefs.GetFloat("rawTime", 0f);

        if (bestRawTime == 0f || LapTimeManager.rawTime < bestRawTime)
        {
            minuteDisplay.text = LapTimeManager.minuteCount.ToString("00") + ":";
            secondDisplay.text = LapTimeManager.secondCount.ToString("00") + ".";
            milliDisplay.text = LapTimeManager.milliCount.ToString("F0");

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