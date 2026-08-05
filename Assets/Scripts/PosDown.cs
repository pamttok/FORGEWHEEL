using UnityEngine;
using UnityEngine.UI;

public class PosDown : MonoBehaviour
{
    [SerializeField] private GameObject positionDisplay;
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "CarPos")
        {
            positionDisplay.GetComponent<TMPro.TMP_Text>().text = "2ND";
        }
    }
}
