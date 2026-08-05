using UnityEngine;
using UnityEngine.UI;

/// Loads the player's saved lap time from PlayerPrefs and displays it on the UI when the scene starts
public class LoadLapTime : MonoBehaviour
{
    // Stores the saved minutes value.
    [SerializeField] private int minCount;

    // Stores the saved seconds value.
    [SerializeField] private int secCount;

    // Stores the saved milliseconds value.
    [SerializeField] private float milliCount;

    // Reference to the UI object displaying the minutes.
    [SerializeField] private GameObject minDisplay;

    // Reference to the UI object displaying the seconds.
    [SerializeField] private GameObject secDisplay;

    // Reference to the UI object displaying the milliseconds.
    [SerializeField] private GameObject milliDisplay;

    // Retrieves the saved lap time values from PlayerPrefs and updates the corresponding UI text elements
    private void Update()
    {
        // Load the saved lap time values.
        minCount = PlayerPrefs.GetInt("MinSave");
        secCount = PlayerPrefs.GetInt("SecSave");
        milliCount = PlayerPrefs.GetFloat("MilliSave");

        // Display the loaded minutes.
        minDisplay.GetComponent<TMPro.TMP_Text>().text = "" + minCount + ":";

        // Display the loaded seconds.
        secDisplay.GetComponent<TMPro.TMP_Text>().text = "" + secCount + ".";

        // Display the loaded milliseconds without decimal places.
        milliDisplay.GetComponent<TMPro.TMP_Text>().text = milliCount.ToString("F0");
    }
}