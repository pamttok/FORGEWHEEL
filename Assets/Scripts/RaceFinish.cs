using UnityEngine;

//This script determines whether the player wins or loses depending on which car reaches the finish trigger first
/// <summary>
/// Detects which car crosses the finish trigger first and resolves the
/// race outcome accordingly: shows the win panel if the player's car
/// arrives first, or the lose panel if an AI car does. Freezes gameplay,
/// disables car control, and swaps to the finish camera/UI/audio state
/// for whichever outcome occurs. Only resolves once per race.
/// </summary>
public class RaceFinish : MonoBehaviour
{
    // ---------------------------------------------------------------
    // Inspector References
    // ---------------------------------------------------------------

    [Header("UI")]
    [Tooltip("Panel shown when the player's car crosses the finish line first.")]
    [SerializeField] private GameObject winPanel;

    [Tooltip("Panel shown when an AI car crosses the finish line first.")]
    [SerializeField] private GameObject losePanel;

    [Header("References")]
    [Tooltip("The player's car GameObject, used to disable its controller on race end.")]
    [SerializeField] private GameObject myCar;

    [Tooltip("Music/stinger played once the race resolves (win or lose).")]
    [SerializeField] private AudioSource finishMusic;

    [Tooltip("Player car's engine audio source, stopped once the race resolves.")]
    [SerializeField] private AudioSource _engine;

    [Header("Optional")]
    [Tooltip("Camera activated for the finish sequence.")]
    [SerializeField] private GameObject finishCam;

    [Tooltip("In-level background music, disabled once the race resolves.")]
    [SerializeField] private GameObject levelMusic;

    [Tooltip("Camera/view-mode switching UI, disabled once the race resolves.")]
    [SerializeField] private GameObject viewModes;

    [Tooltip("Trigger object to disable on a win (e.g. to prevent further scoring/collision).")]
    [SerializeField] private GameObject completeTrig;

    // ---------------------------------------------------------------
    // Internal State
    // ---------------------------------------------------------------

    // Guards against the race being resolved more than once.
    private bool raceFinished;

    // ---------------------------------------------------------------
    // Trigger Events
    // ---------------------------------------------------------------

    /// <summary>
    /// Fired when a car crosses the finish trigger. Resolves the race as a
    /// win if it's the player's car ("MyCar"), or a loss if it's an AI car
    /// ("AICarTracker"). Ignored entirely once the race has already resolved.
    /// </summary>
    private void OnTriggerEnter(Collider collision)
    {
        if (raceFinished)
            return;
        if (collision.gameObject.tag == "MyCar")
        {
            raceFinished = true;
            PlayerWins();
        }
        else if (collision.gameObject.tag == "AICarTracker")
        {
            raceFinished = true;
            PlayerLoses();
        }
    }

    // ---------------------------------------------------------------
    // Race Resolution
    // ---------------------------------------------------------------

    /// <summary>
    /// Handles the win state: freezes gameplay, disables the player's car
    /// control and top speed, switches to the finish camera/UI, stops level
    /// music/engine audio, plays the finish stinger, and shows the win panel.
    /// </summary>
    private void PlayerWins()
    {
        GetComponent<Collider>().enabled = false;
        Time.timeScale = 0f;
        if (completeTrig != null)
            completeTrig.SetActive(false);
        BMWCarController.topSpeed = 0f;
        BMWCarController controller = myCar.GetComponent<BMWCarController>();
        if (controller != null)
            controller.enabled = false;
        if (finishCam != null)
            finishCam.SetActive(true);
        if (levelMusic != null)
            levelMusic.SetActive(false);
        if (viewModes != null)
            viewModes.SetActive(false);
        if (finishMusic != null)
            finishMusic.Play();
        if (_engine != null)
            _engine.Stop();
        if (winPanel != null)
            winPanel.SetActive(true);
    }

    /// <summary>
    /// Handles the lose state: freezes gameplay, disables the player's car
    /// control and top speed, switches to the finish camera/UI, stops level
    /// music/engine audio, plays the finish stinger, and shows the lose panel.
    /// </summary>
    private void PlayerLoses()
    {
        GetComponent<Collider>().enabled = false;
        Time.timeScale = 0f;
        BMWCarController.topSpeed = 0f;
        BMWCarController controller = myCar.GetComponent<BMWCarController>();
        if (controller != null)
            controller.enabled = false;
        if (finishCam != null)
            finishCam.SetActive(true);
        if (levelMusic != null)
            levelMusic.SetActive(false);
        if (viewModes != null)
            viewModes.SetActive(false);
        if (_engine != null)
            _engine.Stop();
        if (finishMusic != null)
            finishMusic.Play();
        if (losePanel != null)
            losePanel.SetActive(true);
    }
}
