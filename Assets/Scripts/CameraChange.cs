using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

//this script handles switching between the available camera views based on player input
public class CameraChange : MonoBehaviour
{
    // Reference to the default third-person camera.
    [SerializeField] private GameObject normalCam;

    // Reference to the distant chase camera.
    [SerializeField] private GameObject farCam;

    // Reference to the first-person camera.
    [SerializeField] private GameObject fpCam;

    // Keeps track of the currently selected camera mode.
    // 0 = Normal Camera
    // 1 = Far Camera
    // 2 = First-Person Camera
    [SerializeField] private int CamMode;

    // Checks for camera switch input and cycles through the available camera modes
    private void Update()
    {
        // Switch camera when the assigned input button is pressed.
        if (Input.GetButtonDown("Viewmode"))
        {
            // Reset to the first camera after reaching the last one.
            if (CamMode == 2)
            {
                CamMode = 0;
            }
            else
            {
                // Move to the next camera mode.
                CamMode += 1;
            }

            // Apply the camera change after a short delay.
            StartCoroutine(ModeChange());
        }
    }
    // Activates the selected camera and deactivates the previously active camera
    IEnumerator ModeChange()
    {
        // Small delay to ensure the camera transition occurs smoothly.
        yield return new WaitForSeconds(0.03f);

        // Activate the normal third-person camera.
        if (CamMode == 0)
        {
            normalCam.SetActive(true);
            fpCam.SetActive(false);
        }

        // Activate the far chase camera.
        if (CamMode == 1)
        {
            farCam.SetActive(true);
            normalCam.SetActive(false);
        }

        // Activate the first-person camera.
        if (CamMode == 2)
        {
            fpCam.SetActive(true);
            farCam.SetActive(false);
        }
    }
}