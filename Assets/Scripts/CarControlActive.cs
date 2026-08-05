using UnityEngine;

/// <summary>
/// Enables the player and AI vehicle controllers
/// when the scene starts.
/// </summary>
public class CarControlActive : MonoBehaviour
{
    // Reference to the player-controlled vehicle.
    [SerializeField] private GameObject carControl;

    // Reference to the AI-controlled vehicle.
    [SerializeField] private GameObject carAI;

    /// <summary>
    /// Enables the required control scripts at the
    /// beginning of the scene.
    /// </summary>
    private void Start()
    {
        // Enable the player's car controller.
        carControl.GetComponent<BMWCarController>().enabled = true;

        // Enable the AI car controller.
        carAI.GetComponent<CarAIControl>().enabled = true;
    }
}