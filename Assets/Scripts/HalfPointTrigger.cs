using UnityEngine;

//Activates the lap completion trigger after the player reaches the halfway point
public class HalfPointTrigger : MonoBehaviour
{
    [SerializeField] private GameObject lapCompleteTrigger;
    [SerializeField] private GameObject halfLapTrigger;

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.tag == "MyCar" || collision.gameObject.tag == "AICarTracker")
            return;

        lapCompleteTrigger.SetActive(true);
        halfLapTrigger.SetActive(false);
    }
}