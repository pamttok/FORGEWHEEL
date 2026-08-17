using UnityEngine;

/// <summary>
/// Keeps this object's rotation matching the target vehicle's yaw and pitch,
/// while forcibly leveling out roll (Z rotation) so the camera stays steady
/// even if the car banks or rolls.
/// </summary>
public class CameraSteady : MonoBehaviour
{
    // ---------------------------------------------------------------
    // Inspector References
    // ---------------------------------------------------------------

    [Tooltip("The vehicle whose rotation this object should follow.")]
    [SerializeField] private GameObject theVehicle;

    [Tooltip("Cached X (pitch) rotation of the vehicle, exposed for inspection/debugging.")]
    [SerializeField] private float CarX;

    [Tooltip("Cached Y (yaw) rotation of the vehicle, exposed for inspection/debugging.")]
    [SerializeField] private float CarY;

    [Tooltip("Cached Z (roll) rotation of the vehicle, exposed for inspection/debugging.")]
    [SerializeField] private float CarZ;

    // ---------------------------------------------------------------
    // Unity Lifecycle
    // ---------------------------------------------------------------

    /// <summary>
    /// Runs after all other Update logic (e.g. vehicle physics) to read the
    /// vehicle's current rotation and apply it to this object, with roll
    /// zeroed out to keep the view level.
    /// </summary>
    private void LateUpdate()
    {
        // Cache the vehicle's current rotation on each axis.
        CarX = theVehicle.transform.eulerAngles.x;
        CarY = theVehicle.transform.eulerAngles.y;
        CarZ = theVehicle.transform.eulerAngles.z;

        // Match pitch and yaw to the vehicle, but cancel out roll entirely
        // (CarZ - CarZ always evaluates to 0) so this object never tilts sideways.
        transform.eulerAngles = new Vector3(CarX, CarY, CarZ - CarZ);
    }
}
