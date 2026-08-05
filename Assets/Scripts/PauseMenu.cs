using UnityEngine;
using UnityEngine.SceneManagement;

// this script handles the pause,restart,resume, mainmenu,track-select,garage,information BUTTONS logic
public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject container;
    [SerializeField] private AudioSource levelMusic;
    [SerializeField] private AudioSource engine;
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
    public void ResumeButton()
    {
        container.SetActive(false);
        Time.timeScale = 1;
        levelMusic.Play();
        engine.Play();
    }
    public void MainMenu()
    {
        SceneManager.LoadScene(0);
        Time.timeScale = 1f;
    }
    public void Restart()
    {
        SceneManager.LoadScene(4);
        BMWCarController.topSpeed = 80f;
        Time.timeScale = 1f;
    }
    public void TrackSelect()
    {
        SceneManager.LoadScene(4);
        Time.timeScale = 1f;
    }
}
