using UnityEngine;

public class CameraSteady : MonoBehaviour
{
    [SerializeField] private GameObject theVehicle;
    [SerializeField] private float CarX;
    [SerializeField] private float CarY;
    [SerializeField] private float CarZ;

    private void LateUpdate()
    {
        CarX = theVehicle.transform.eulerAngles.x;
        CarY = theVehicle.transform.eulerAngles.y;
        CarZ = theVehicle.transform.eulerAngles.z;

        transform.eulerAngles = new Vector3(CarX, CarY, CarZ - CarZ);
    }
}