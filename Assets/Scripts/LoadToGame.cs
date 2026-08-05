using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// This script loads the race scene after a predefined delay.
public class LoadToGame : MonoBehaviour
{
    // Starts the delayed scene loading process when the scene is initialized.
    private void Start()
    {
        Time.timeScale = 1f;
        StartCoroutine(LoadRaceArea());
    }

    /// Waits for the specified duration before loading the race scene
    IEnumerator LoadRaceArea()
    {
        Time.timeScale = 1f;
        // Initial loading delay.
        yield return new WaitForSecondsRealtime(7.5f);

        // Load the race scene by its build index.
        SceneManager.LoadScene(4);
    }
}