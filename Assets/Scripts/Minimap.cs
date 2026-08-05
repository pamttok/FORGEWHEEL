using UnityEngine;

/// <summary>
/// Keeps the minimap camera aligned with the player's position
/// while maintaining a constant height above the scene.
/// </summary>
public class Minimap : MonoBehaviour
{
    // Reference to the player's transform.
    [SerializeField] private Transform player;

    /// <summary>
    /// Updates the minimap camera position after all movement
    /// has been processed for the current frame.
    /// </summary>
    private void LateUpdate()
    {
        // Copy the player's current position.
        Vector3 newPosition = player.position;

        // Preserve the minimap camera's current height.
        newPosition.y = transform.position.y;

        // Move the minimap camera to follow the player.
        transform.position = newPosition;
    }
}