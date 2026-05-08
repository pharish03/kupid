using System.Collections;
using UnityEngine;

public class PlayerPowerUps : MonoBehaviour
{

    private int ActiveAbility = 0;
    public PlayerMovement playerMovement;
    public GameObject shield;
    public Grappling grappling;

    private void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("PowerUp"))
        {
            Debug.Log("Collided with PowerUp!");
            if (other.gameObject.name == "Speed")
            {
                ActiveAbility = 1;
                Debug.Log("Speed PowerUp Activated!");
            }
            else if (other.gameObject.name == "Shield")
            {
                ActiveAbility = 2;
                Debug.Log("Shield PowerUp Activated!");
            }
            else if (other.gameObject.name == "grappleHook")
            {
                ActiveAbility = 3;
                Debug.Log("Grapple Hook PowerUp Activated!");
            }
            other.gameObject.SetActive(false); 
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E)) // WE, yes WE, are all using if else statements FOREVER!
        {
            if (ActiveAbility == 1) // Speed
            {
                Debug.Log("Using Speed PowerUp!");
                StartCoroutine(ActivateSpeed());
                ActiveAbility = 0;
            }
            else if (ActiveAbility == 2) // Shield
            {
                Debug.Log("Using Shield PowerUp!");
                StartCoroutine(ActivateShield());
                ActiveAbility = 0;
            }
            else if (ActiveAbility == 3) // AWESOME AF GRAPPLE HOOK
            {
                Debug.Log("Using Grapple Hook PowerUp!");
                grappling.enabled = true;
                StartCoroutine(UseGrapple());
                ActiveAbility = 0;
                // Implement grapple hook logic here
            }
        }
    }

    private IEnumerator ActivateSpeed()
    {
        playerMovement.speed *= 2f; 
        yield return new WaitForSeconds(5f); // Duration of the speed boost
        playerMovement.speed /= 2f; 
    }
    private IEnumerator ActivateShield()
    {
        shield.SetActive(true); // Activate the shield
        yield return new WaitForSeconds(5f); // Duration of the shield
        shield.SetActive(false); // Deactivate the shield
    }

    private IEnumerator UseGrapple() // Player uses grapple hook -> This function fires -> Player finishes using grapple hook -> Grapplehook disabled :D
    {
        yield return new WaitForSeconds(5f); 
        grappling.enabled = false;
   
    }
}
