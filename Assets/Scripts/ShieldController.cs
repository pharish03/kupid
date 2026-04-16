using UnityEngine;

public class ShieldController : MonoBehaviour
{
    // In order for this shield code to work, the arrow must be tagged as "Arrow" and the shield must have a collider with "Is Trigger" enabled.
     private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Arrow"))
        {
            Debug.Log("Shield hit by an arrow!");
            Destroy(other.gameObject);
            gameObject.SetActive(false);
        }
    }
   
}
