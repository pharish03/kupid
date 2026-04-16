using UnityEngine;

public class SpeedPowerup : MonoBehaviour
{
  
    public Vector3 rotationSpeed = new Vector3(0, 0, 70);

    void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}
