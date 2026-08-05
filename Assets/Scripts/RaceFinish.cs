using UnityEngine;

//This script determines whether the player wins or loses depending on which car reaches the finish trigger first
public class RaceFinish : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;

    [Header("References")]
    [SerializeField] private GameObject myCar;
    [SerializeField] private AudioSource finishMusic;
    [SerializeField] private AudioSource _engine;

    [Header("Optional")]
    [SerializeField] private GameObject finishCam;
    [SerializeField] private GameObject levelMusic;
    [SerializeField] private GameObject viewModes;
    [SerializeField] private GameObject completeTrig;

    private bool raceFinished;

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