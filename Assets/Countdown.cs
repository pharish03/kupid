using UnityEngine;
using TMPro;
using Unity.Netcode;

public class Countdown : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI timerText;

    // These are set directly by RoundManager (server only)
    public float remainingTime;
    public bool isTimerOn;

    void Update()
    {
        if (isTimerOn)
        {
            if (remainingTime > 0)
                remainingTime -= Time.deltaTime;
            else
            {
                remainingTime = 0;
                isTimerOn = false;
            }
        }

        if (timerText != null && isTimerOn)
        {
            int minutes = Mathf.FloorToInt(remainingTime / 60);
            int seconds = Mathf.FloorToInt(remainingTime % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    public void SetCountdown(float newTime)
    {
        remainingTime = newTime;
    }
}