using UnityEngine;

/// <summary>
/// Continuously spins this object around the world Y-axis.
/// Typically used for decorative effects, such as a finish-line
/// trophy, banner, or checkpoint marker slowly rotating in place.
/// </summary>
public class FinishRotate : MonoBehaviour
{
    // ---------------------------------------------------------------
    // Unity Lifecycle
    // ---------------------------------------------------------------

    /// <summary>
    /// Applies a constant world-space rotation every frame.
    /// </summary>
    private void Update()
    {
        // Rotate 1 degree per frame around the world Y-axis.
        // Note: not framerate-independent — actual rotation speed
        // will vary with frame rate since this isn't scaled by Time.deltaTime.
        transform.Rotate(0, 1, 0, Space.World);
    }
}
