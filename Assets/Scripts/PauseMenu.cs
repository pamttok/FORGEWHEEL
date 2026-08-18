using UnityEngine;
using UnityEngine.SceneManagement;

// this script handles the pause,restart,resume, mainmenu,track-select,garage,information BUTTONS logic
/// <summary>
/// Handles the pause menu: toggling it open via the Escape key (freezing
/// time and stopping level/engine audio), and button callbacks for resuming,
/// returning to the main menu, restarting, and going to track select.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    // ---------------------------------------------------------------
    // Inspector References
    // ---------------------------------------------------------------

    [Tooltip("Root UI container for the pause menu panel.")]
    [SerializeField] private GameObject container;

    [Tooltip("Background level music, stopped while paused and resumed on unpause.")]
    [SerializeField] private AudioSource levelMusic;

    [Tooltip("Car engine audio source, stopped while paused and resumed on unpause.")]
    [SerializeField] private AudioSource engine;

    // ---------------------------------------------------------------
    // Unity Lifecycle
    // ---------------------------------------------------------------

    /// <summary>
    /// Listens for the pause input each frame and opens the pause menu
    /// (freezing time, stopping music/engine audio) when pressed.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            container.SetActive(true);
            Time.timeScale = 0;
            levelMusic.Stop();
            engine.Stop();
        }
    }

    // ---------------------------------------------------------------
    // Button Callbacks
    // ---------------------------------------------------------------

    /// <summary>
    /// Closes the pause menu and resumes normal gameplay speed, music, and engine audio.
    /// </summary>
    public void ResumeButton()
    {
        container.SetActive(false);
        Time.timeScale = 1;
        levelMusic.Play();
        engine.Play();
    }

    /// <summary>
    /// Returns the player to the main menu scene and restores normal time scale.
    /// </summary>
    public void MainMenu()
    {
        SceneManager.LoadScene(0);
        Time.timeScale = 1f;
    }

    /// <summary>
    /// Restarts the race: reloads the level scene, resets the car's top speed
    /// (in case it was zeroed out by a race-finish state), and restores time scale.
    /// </summary>
    public void Restart()
    {
        SceneManager.LoadScene(4);
        BMWCarController.topSpeed = 80f;
        Time.timeScale = 1f;
    }

    /// <summary>
    /// Returns to track select and restores normal time scale.
    /// </summary>
    public void TrackSelect()
    {
        SceneManager.LoadScene(4);
        Time.timeScale = 1f;
    }
}
